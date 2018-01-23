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
        private readonly Dictionary<string, string> _docToCSharp = new Dictionary<string, string>();

        public void AddOrUpdateDocLink(string cppName, string cSharpName)
        {
            _docToCSharp[cppName] = cSharpName;
        }

        /// <summary>
        ///   Finds the C# full name from a C++ name.
        /// </summary>
        /// <param name = "cppName">Name of a c++ type</param>
        /// <returns>Name of the C# type</returns>
        public string FindDocName(string cppName)
        {
            return _docToCSharp.TryGetValue(cppName, out string cSharpName) ? cSharpName : null;
        }

        public IEnumerable<(string cppName, string cSharpName)> GetAllDocLinks()
        {
            foreach (var link in _docToCSharp)
            {
                yield return (link.Key, link.Value);
            }
        }
    }
}
