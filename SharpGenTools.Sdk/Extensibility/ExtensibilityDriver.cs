#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpGen.CppModel;
using SharpGen.Doc;
using SharpGen.Logging;
using SharpGenTools.Sdk.Documentation;
using SharpGenTools.Sdk.Internal;
using SharpGenTools.Sdk.Internal.Roslyn;

namespace SharpGenTools.Sdk.Extensibility
{
    internal class ExtensibilityDriver
    {
        private readonly HashSet<ExtensionReference> extensionReferences = new();
        private ImmutableHashSet<ExtensionReference>? extensionsImmutable;

        public static ExtensibilityDriver Instance { get; } = new();

        private ExtensibilityDriver()
        {
        }

        public ExtensibilityAssemblyLoader AssemblyLoader { get; } = new DefaultExtensionAssemblyLoader();

        public ImmutableHashSet<ExtensionReference> ExtensionReferences => extensionsImmutable ??= extensionReferences.ToImmutableHashSet();

        public bool LoadExtensions(IReadOnlyCollection<string> references)
        {
            var loaded = false;

            foreach (var reference in ResolveExtensionReferences(references))
            {
                if (!extensionReferences.Add(reference)) continue;

                if (loaded) continue;

                loaded = true;
                Interlocked.Exchange(ref extensionsImmutable, null);
            }

            return loaded;
        }

        private void ResolveExtensibilityPoints(Logger logger, out ImmutableArray<IDocProvider> docProviders)
        {
            var docProviderBuilder = ImmutableArray.CreateBuilder<IDocProvider>();

            void ErrorHandler(object o, ExtensionLoadFailureEventArgs e)
            {
                var analyzerReference = o as ExtensionReference;
                Debug.Assert(analyzerReference != null);
                switch (e.ErrorCode)
                {
                    case ExtensionLoadFailureEventArgs.FailureErrorCode.UnableToLoadExtension:
                        logger.Error(LoggingCodes.ExtensionLoadFailure, "Unable to load extension assembly {0} : {1}", e.Exception, analyzerReference.FullPath, e.Message);
                        break;
                    case ExtensionLoadFailureEventArgs.FailureErrorCode.UnableToCreateExtension:
                        logger.Error(LoggingCodes.ExtensionLoadFailure, "An instance of extension {0} cannot be created from {1} : {2}", e.Exception, e.TypeName ?? "", analyzerReference.FullPath, e.Message);
                        break;
                    case ExtensionLoadFailureEventArgs.FailureErrorCode.InternalExtensionEntryPointCastError:
                        logger.Error(LoggingCodes.ExtensibilityInternalError, "The extension assembly {0} has caused internal assertion errors during load.", e.Exception, analyzerReference.FullPath);
                        break;
                    case ExtensionLoadFailureEventArgs.FailureErrorCode.None:
                    default:
                        return;
                }
            }

            // All analyzer references are registered now, we can start loading them:
            foreach (var resolvedReference in extensionReferences.OfType<ExtensionFileReference>())
            {
                resolvedReference.AnalyzerLoadFailed += ErrorHandler;
                resolvedReference.AddDocumentationProviders(docProviderBuilder);
                resolvedReference.AnalyzerLoadFailed -= ErrorHandler;
            }

            docProviders = docProviderBuilder.ToImmutable();
        }

        public async Task DocumentModule(Logger logger, DocItemCache cache, CppModule module, Lazy<DocumentationContext> context)
        {
            ResolveExtensibilityPoints(logger, out var documentationProviders);

            if (documentationProviders.Length == 0)
                return;

            var docContext = context.Value;

            foreach (var documentationProvider in documentationProviders)
            {
                // Wait on every provider (sequential execution to prevent data races)
                await DocProviderExecutor.ApplyDocumentation(documentationProvider, cache, module, docContext);
            }
        }

        private IEnumerable<ExtensionReference> ResolveExtensionReferences(IReadOnlyCollection<string> references)
        {
            foreach (var path in references)
            {
                if (PathUtilities.IsAbsolute(path))
                {
                    var fullPath = Utilities.TryNormalizeAbsolutePath(path);

                    if (fullPath != null && File.Exists(fullPath))
                    {
                        AssemblyLoader.AddDependencyLocation(fullPath);
                    }
                }
            }

            foreach (var cmdLineReference in references)
            {
                yield return ResolveExtensionReference(cmdLineReference, AssemblyLoader)
                          ?? new UnresolvedExtensionReference(cmdLineReference);
            }
        }

        private static ExtensionReference? ResolveExtensionReference(string reference, ExtensibilityAssemblyLoader analyzerLoader)
        {
            if (reference != null)
            {
                reference = Utilities.TryNormalizeAbsolutePath(reference);
            }

            return reference != null ? new ExtensionFileReference(reference, analyzerLoader) : null;
        }
    }
}