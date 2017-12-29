using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Transform
{
    public interface IDocumentationAggregator
    {
        IEnumerable<string> GetDocItems(CsBase element);

        string GetSingleDoc(CsBase element);

        void AddDocLink(string cppName, string cSharpName);
    }
}
