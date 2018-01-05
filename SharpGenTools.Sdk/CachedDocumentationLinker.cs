using System;
using System.Collections.Generic;
using System.IO;
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
            using (var file = File.OpenRead(fileName))
            using (var reader = new StreamReader(file))
            {
                var pair = reader.ReadLine();
                var components = pair.Split(Delimiter);
                AddDocLink(components[0], components[1]);
            }
        }

        public void AddDocLink(string cppName, string cSharpName)
        {
            Links.Add(cppName, cSharpName);
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
