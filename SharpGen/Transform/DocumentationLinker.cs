using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using SharpGen.Model;

namespace SharpGen.Transform
{
    public class DocumentationLinker : IDocumentationLinker
    {
        private readonly TypeRegistry typeRegistry;

        private readonly Dictionary<string, string> _docToCSharp = new Dictionary<string, string>();

        public DocumentationLinker(TypeRegistry typeRegistry)
        {
            this.typeRegistry = typeRegistry;
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

        public IEnumerable<(string cppName, string cSharpName)> GetAllDocLinks()
        {
            foreach (var link in _docToCSharp)
            {
                yield return (link.Key, link.Value);
            }

            foreach (var binding in typeRegistry.GetTypeBindings())
            {
                yield return (binding.CppType, binding.CSharpType.QualifiedName);
            }
        }
    }
}
