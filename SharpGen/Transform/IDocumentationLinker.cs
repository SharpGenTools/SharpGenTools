using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpGen.Transform
{
    public interface IDocumentationLinker
    {
        void AddDocLink(string cppName, string cSharpName);

        IEnumerable<(string cppName, string cSharpName)> GetAllDocLinks();
        
        string FindDocName(string cppName);
    }
}
