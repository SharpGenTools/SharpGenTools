using System.Collections.Generic;

namespace SharpGen.Transform
{
    public interface IDocumentationLinker
    {
        void AddOrUpdateDocLink(string cppName, string cSharpName);

        IEnumerable<(string cppName, string cSharpName)> GetAllDocLinks();

        string FindDocName(string cppName);
    }
}
