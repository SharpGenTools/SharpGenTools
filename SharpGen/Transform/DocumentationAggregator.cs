using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SharpGen.Model;

namespace SharpGen.Transform
{
    class DocumentationAggregator : IDocumentationAggregator
    {
        private readonly TypeRegistry typeRegistry;
        private static readonly Regex RegexLinkStart = new Regex(@"^\s*\{\{.*?}}\s*(.*)", RegexOptions.Compiled);
        private static readonly Regex RegexLink = new Regex(@"\{\{(.*?)}}", RegexOptions.Compiled);
        private static readonly Regex RegexSpaceBegin = new Regex(@"^\s*(.*)", RegexOptions.Compiled);

        private readonly Dictionary<string, string> _docToCSharp = new Dictionary<string, string>();

        public DocumentationAggregator(TypeRegistry typeRegistry)
        {
            this.typeRegistry = typeRegistry;
        }

        public IEnumerable<string> GetDocItems(CsBase element)
        {
            var docItems = new List<string>();

            // If doc comments are already stored in an external file, than don't emit them
            if (!element.IsCodeCommentsExternal)
            {
                var description = element.Description;
                var remarks = element.Remarks;

                description = RegexSpaceBegin.Replace(description, "$1");

                description = RegexLink.Replace(description, RegexReplaceCReference);
                // evaluator => "<see cref=\"$1\"/>");
                
                docItems.Add("<summary>");
                docItems.AddRange(description.Split('\n'));
                docItems.Add("</summary>");

                element.FillDocItems(docItems, this);

                if (!string.IsNullOrEmpty(remarks))
                {
                    remarks = RegexSpaceBegin.Replace(remarks, "$1");
                    remarks = RegexLink.Replace(remarks, RegexReplaceCReference);

                    docItems.Add("<remarks>");
                    docItems.AddRange(remarks.Split('\n'));
                    docItems.Add("</remarks>");
                }
            }

            docItems.Add(element.DocIncludeDirective);
            if (element.CppElement != null)
            {
                if (element.DocId != null)
                {
                    docItems.Add("<msdn-id>" + Utilities.EscapeXml(element.DocId) + "</msdn-id>");
                }
                docItems.Add("<unmanaged>" + Utilities.EscapeXml(element.DocUnmanagedName) + "</unmanaged>");
                docItems.Add("<unmanaged-short>" + Utilities.EscapeXml(element.DocUnmanagedShortName) + "</unmanaged-short>");
            }

            return docItems;
        }

        /// <summary>
        /// Gets the description as a single line of documentation.
        /// </summary>
        /// <value>The single doc.</value>
        public string GetSingleDoc(CsBase element)
        {
            var description = element.Description;

            if (RegexLinkStart.Match(description).Success)
                description = RegexLinkStart.Replace(description, "$1");

            description = RegexSpaceBegin.Replace(description, "$1");

            description = RegexLink.Replace(description, RegexReplaceCReference);

            var docItems = new StringBuilder();

            foreach (var line in description.Split('\n'))
            {
                docItems.Append(line); 
            }

            return docItems.ToString();
        }


        private static readonly Regex regexWithMethodW = new Regex("([^W])::");
        private static readonly Regex regexWithTypeW = new Regex("([^W])$");

        private string RegexReplaceCReference(Match match)
        {
            var matchName = match.Groups[1].Value;
            var csName = FindDocName(matchName);

            // Tries to match with W::
            if (csName == null && regexWithMethodW.Match(matchName).Success)
                csName = FindDocName(regexWithMethodW.Replace(matchName, "$1W::"));

            // Or with W
            if (csName == null && regexWithTypeW.Match(matchName).Success)
                csName = FindDocName(regexWithTypeW.Replace(matchName, "$1W"));

            if (csName == null)
                return matchName;

            if (csName.StartsWith("<"))
                return csName;
            return string.Format(CultureInfo.InvariantCulture, "<see cref=\"{0}\"/>", csName);
        }

        public void AddDocLink(string cppName, string cSharpName)
        {
            if (!_docToCSharp.ContainsKey(cppName))
                _docToCSharp.Add(cppName, cSharpName);
        }

        /// <summary>
        ///   Finds the C# full name from a C++ name.
        /// </summary>
        /// <param name = "cppName">Name of a c++ type</param>
        /// <returns>Name of the C# type</returns>
        public string FindDocName(string cppName)
        {
            if (_docToCSharp.TryGetValue(cppName, out string cSharpName))
                return cSharpName;

            return typeRegistry.FindBoundType(cppName)?.QualifiedName;
        }
    }
}
