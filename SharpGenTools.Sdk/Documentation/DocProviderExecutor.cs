#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Polly.Contrib.WaitAndRetry;
using SharpGen.Doc;
using SharpGen.Model;
using SharpGen.Platform.Documentation;

namespace SharpGenTools.Sdk.Documentation;

internal static class DocProviderExecutor
{
    private static readonly TimeSpan TenthOfSecond = TimeSpan.FromMilliseconds(100);

    public static async Task ApplyDocumentation(IDocProvider? docProvider, DocItemCache cache, CsAssembly module,
                                                DocumentationContext context)
    {
        var documentationTasks = new List<Task>();

        Task DocumentSelector(CsBase cppElement) =>
            DocumentElement(docProvider, cache, cppElement, context, true, null);

        foreach (var cppInclude in module.Namespaces)
        {
            documentationTasks.AddRange(cppInclude.Enums.Select(DocumentSelector));
            documentationTasks.AddRange(cppInclude.Structs.Select(DocumentSelector));
            documentationTasks.AddRange(
                cppInclude.Interfaces
                          .Select(cppInterface => DocumentInterface(docProvider, cache, cppInterface, context))
            );
            documentationTasks.AddRange(
                cppInclude.Classes
                          .Select(cppFunction => DocumentGroup(docProvider, cache, cppFunction, context))
            );
        }

        await Task.WhenAll(documentationTasks);
    }

    private static async Task<IDocItem?> DocumentElement(IDocProvider? docProvider,
                                                         DocItemCache cache,
                                                         CsBase element,
                                                         DocumentationContext context,
                                                         bool documentInnerElements,
                                                         string? name)
    {
        var docName = name ?? element.CppElementName;

        if (string.IsNullOrEmpty(docName))
            return null;

        docName = docName.Trim();

        if (string.IsNullOrEmpty(docName))
            return null;

        var cacheEntry = cache.Find(docName);
        var docItem = cacheEntry ?? await QueryDocumentationProvider();

        if (docItem == null)
            return null;

        element.DocId = docItem.ShortId;
        element.Description = docItem.Summary;
        element.Remarks = docItem.Remarks;
        docItem.Names.Add(docName);

        if (cacheEntry == null)
            cache.Add(docItem);

        if (element.Items.Count == 0)
            return docItem;

        if (documentInnerElements)
            DocumentInnerElements(element.Items, docItem);

        return docItem;

        async Task<IDocItem?> QueryDocumentationProvider()
        {
            if (docProvider == null)
                return null;

            Lazy<string> docProviderName = new(
                () =>
                {
                    try
                    {
                        var friendlyName = docProvider.UserFriendlyName;
                        return string.IsNullOrWhiteSpace(friendlyName) ? FullName() : friendlyName;
                    }
                    catch
                    {
                        return FullName();
                    }

                    string FullName()
                    {
                        var type = docProvider.GetType();
                        var name = type.FullName;
                        return string.IsNullOrEmpty(name) ? type.Name : name!;
                    }
                },
                LazyThreadSafetyMode.None
            );

            List<Exception> exceptions = new(3);
            var (backoff, backoffIndex, nextDelay) = GenerateBackoff(TimeSpan.Zero);

            try
            {
                for (uint retry = 0; retry <= 5; retry++)
                {
                    if (retry != 0)
                    {
                        TimeSpan delay;
                        if (nextDelay.HasValue)
                            delay = nextDelay.Value;
                        else
                        {
                            // TODO: fix the bug and remove this hack
                            if (backoffIndex >= 5)
                            {
                                context.Logger.Message(
                                    $"SharpGen internal invalid state on delay: backoffIndex == {backoffIndex}"
                                );
                                if (Debugger.IsAttached) Debugger.Break();
                                backoffIndex = 0;
                            }

                            delay = backoff[backoffIndex++];
                        }
                        if (delay > TimeSpan.Zero)
                            await Task.Delay(delay);
                        nextDelay = null;
                    }

                    try
                    {
                        var result = await docProvider.FindDocumentationAsync(docName, context);
                        switch (result)
                        {
                            case null:
                                throw new ArgumentNullException(
                                    nameof(result),
                                    $"Unexpected null {nameof(IFindDocumentationResult)}"
                                );
                            case FindDocumentationResultFailure resultFailure:
                            {
                                var retryDelay = resultFailure.RetryDelay;
                                if (retryDelay == TimeSpan.MaxValue)
                                    return null;

                                if (retryDelay <= TimeSpan.Zero)
                                    nextDelay = TimeSpan.Zero;

                                // TODO: fix the bug and remove this hack
                                if (backoffIndex >= 5)
                                {
                                    context.Logger.Message(
                                        $"SharpGen internal invalid state on reschedule: backoffIndex = {backoffIndex}"
                                    );
                                    if (Debugger.IsAttached) Debugger.Break();
                                    (backoff, backoffIndex, nextDelay) = GenerateBackoff(retryDelay);
                                }

                                nextDelay = backoff[backoffIndex++];

                                if (nextDelay < retryDelay)
                                    (backoff, backoffIndex, nextDelay) = GenerateBackoff(retryDelay);

                                break;
                            }
                            case FindDocumentationResultSuccess resultSuccess:
                                return resultSuccess.Item; // TODO: check if the item is empty (therefore, useless)
                            default:
                                throw new ArgumentOutOfRangeException(
                                    nameof(result),
                                    $"Unexpected {nameof(IFindDocumentationResult)}: {result.GetType().FullName}"
                                );
                        }
                    }
                    catch (Exception e)
                    {
                        e.Data["SDK:" + nameof(docProvider)] = docProvider;
                        e.Data["SDK:" + nameof(docName)] = docName;
                        e.Data["SDK:" + nameof(context)] = context;
                        e.Data["SDK:" + nameof(retry)] = retry;
                        e.Data["SDK:" + nameof(backoffIndex)] = backoffIndex;
                        e.Data["SDK:" + nameof(exceptions) + ".Count"] = exceptions.Count;

                        exceptions.Add(e);

                        // We should retry less when it's due to unhandled exception.
                        // So in exception case we step twice in retry count on each iteration.
                        retry++;
                    }
                }

                context.Logger.Message($"{docProviderName.Value} extension failed to find documentation for \"{docName}\"");

                return null;
            }
            finally
            {
                if (exceptions.Count > 0)
                {
                    var failure = new DocumentationQueryFailure(docName)
                    {
                        Exceptions = exceptions,
                        FailedProviderName = docProviderName.Value,
                        TreatProviderFailuresAsErrors = docProvider.TreatFailuresAsErrors
                    };

                    context.Failures.Add(failure);
                }
            }
        }
    }

    private static (TimeSpan[] backoff, int index, TimeSpan? nextDelay) GenerateBackoff(TimeSpan offset) =>
        (Backoff.DecorrelatedJitterBackoffV2(offset + TenthOfSecond, 5).ToArray(), 0, null);

    private static async Task DocumentCallable(IDocProvider? docProvider, DocItemCache cache,
                                               CsCallable callable, DocumentationContext context,
                                               string? name = null)
    {
        var docItem = await DocumentElement(docProvider, cache, callable, context, true, name);

        if (docItem == null)
            return;

        callable.ReturnValue.Description = docItem.Return;
    }

    private static async Task DocumentInterface(IDocProvider? docProvider, DocItemCache cache,
                                                CsInterface cppInterface, DocumentationContext context)
    {
        Task CallableSelector(CsCallable func) =>
            DocumentCallable(docProvider, cache, func, context, cppInterface.CppElementName + "::" + func.Name);

        Task VariableSelector(CsBase cppElement) =>
            DocumentElement(docProvider, cache, cppElement, context, true, null);

        // TODO: fix properties docs (they extract doc data from methods on attach to interface)
        await Task.WhenAll(
            cppInterface.Methods.Select(CallableSelector)
                        .Concat(cppInterface.Items.OfType<CsConstantBase>().Select(VariableSelector))
                        .Append(DocumentElement(docProvider, cache, cppInterface, context, false, null))
        );
    }

    private static async Task DocumentGroup(IDocProvider? docProvider, DocItemCache cache,
                                            CsGroup cppInterface, DocumentationContext context)
    {
        Task CallableSelector(CsCallable func) =>
            DocumentCallable(docProvider, cache, func, context);

        Task VariableSelector(CsBase cppElement) =>
            DocumentElement(docProvider, cache, cppElement, context, true, null);

        await Task.WhenAll(
            cppInterface.Functions.Select(CallableSelector)
                        .Concat(cppInterface.Items.OfType<CsConstantBase>().Select(VariableSelector))
                        .Append(DocumentElement(docProvider, cache, cppInterface, context, false, null))
        );
    }

    private static void DocumentInnerElements(IReadOnlyCollection<CsBase> elements, IDocItem docItem)
    {
        var count = Math.Min(elements.Count, docItem.Items.Count);
        var i = 0;
        foreach (var element in elements)
        {
            element.DocId = docItem.ShortId;

            // Try to find the matching item
            var foundMatch = false;
            foreach (var subItem in docItem.Items)
            {
                if (ContainsCppIdentifier(subItem.Term, element.Name))
                {
                    element.Description = subItem.Description;
                    foundMatch = true;
                    break;
                }
            }

            if (!foundMatch && i < count)
                element.Description = docItem.Items[i].Description;
            i++;
        }
    }

    /// <summary>
    /// Determines whether a string contains a given C++ identifier.
    /// </summary>
    /// <param name="str">The string to search.</param>
    /// <param name="identifier">The C++ identifier to search for.</param>
    /// <returns></returns>
    private static bool ContainsCppIdentifier(string str, string identifier)
    {
        if (string.IsNullOrEmpty(str))
            return string.IsNullOrEmpty(identifier);

        return Regex.IsMatch(str, $@"\b{Regex.Escape(identifier)}\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }
}