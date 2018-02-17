using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SharpGen.Model;
using SharpGen.Transform;

namespace SharpGenTools.Sdk
{
    class CachedDocumentationLinker : IDocumentationLinker
    {
        internal static readonly char Delimiter = '%';

        private Dictionary<string, string> Links { get; } = new Dictionary<string, string>();

        public CachedDocumentationLinker(string fileName)
        {
            var lines = File.ReadAllLines(fileName);

            foreach (var line in lines.Where(ln => ln.Contains(Delimiter)))
            {
                var components = line.Split(Delimiter);
                AddOrUpdateDocLink(components[0], components[1]);
            }
        }

        public void AddOrUpdateDocLink(string cppName, string cSharpName)
        {
            Links[cppName] = cSharpName;
        }

        public string FindDocName(string cppName)
        {
            return Links.TryGetValue(cppName, out var cSharpName) ? cSharpName : null;
        }

        public IEnumerable<(string cppName, string cSharpName)> GetAllDocLinks()
        {
            foreach (var link in Links)
            {
                yield return (link.Key, link.Value);
            }
        }
    }
}
