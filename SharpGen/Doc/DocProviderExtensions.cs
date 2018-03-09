using SharpGen.CppModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpGen.Doc
{
    public static class DocProviderExtensions
    {
        public static async Task<CppModule> ApplyDocumentation(this IDocProvider docProvider,  DocItemCache cache, CppModule module)
        {
            var documentationTasks = new List<Task>();
            foreach (CppInclude cppInclude in module.Includes)
            {
                documentationTasks.AddRange(cppInclude.Enums.Select(cppEnum => docProvider.DocumentElement(cache, cppEnum)));
                documentationTasks.AddRange(cppInclude.Structs.Select(cppStruct => docProvider.DocumentElement(cache, cppStruct)));
                documentationTasks.AddRange(cppInclude.Interfaces.Select(cppInterface => docProvider.DocumentInterface(cache, cppInterface)));
                documentationTasks.AddRange(cppInclude.Functions.Select(cppFunction => docProvider.DocumentCallable(cache, cppFunction)));
            }

            await Task.WhenAll(documentationTasks);

            return module;
        }

        private static async Task<DocItem> DocumentElement(
            this IDocProvider docProvider,
            DocItemCache cache,
            CppElement element,
            bool documentInnerElements = true,
            string name = null)
        {
            var docName = name ?? element.Name;

            DocItem cacheEntry = cache.Find(docName);
            var docItem = cacheEntry ?? await docProvider.FindDocumentationAsync(docName);

            element.Id = docItem.ShortId;
            element.Description = docItem.Summary;
            element.Remarks = docItem.Remarks;

            if (cacheEntry == null)
            {
                docItem.Name = docName;
                cache.Add(docItem);
            }

            if (element.IsEmpty)
                return docItem;
            
            if (documentInnerElements)
            {
                DocumentInnerElements(element.Items, docItem);
            }

            return docItem;
        }

        private static async Task DocumentCallable(this IDocProvider docProvider, DocItemCache cache, CppCallable callable, string name = null)
        {
            var docItem = await docProvider.DocumentElement(cache, callable, name: name);

            callable.ReturnValue.Description = docItem.Return;
        }

        private static async Task DocumentInterface(this IDocProvider docProvider, DocItemCache cache, CppInterface cppInterface)
        {
            await Task.WhenAll(
                cppInterface.Methods
                    .Select(func => docProvider.DocumentCallable(cache, func, name: cppInterface.Name + "::" + func.Name))
                    .Concat(new[] { docProvider.DocumentElement(cache, cppInterface, documentInnerElements: false) }));
        }

        private static void DocumentInnerElements(IEnumerable<CppElement> elements, DocItem docItem)
        {
            var count = Math.Min(elements.Count(), docItem.Items.Count);
            var i = 0;
            foreach (CppElement element in elements)
            {
                element.Id = docItem.ShortId;

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

            return Regex.IsMatch(str, string.Format(@"\b{0}\b", Regex.Escape(identifier)), RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        }
    }
}
