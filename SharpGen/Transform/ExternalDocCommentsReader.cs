using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SharpGen.Transform
{
    public class ExternalDocCommentsReader
    {
        public string GetCodeCommentsXPath(CsBase element)
        {
            return $"/comments/comment[@id='{element.CppElementName ?? element.QualifiedName}']";
        }

        public ExternalDocCommentsReader(Dictionary<string, XmlDocument> externalCommentsDocuments)
        {
            ExternalCommentsDocuments = externalCommentsDocuments;
        }

        public Dictionary<string, XmlDocument> ExternalCommentsDocuments { get; }

        public string GetDocumentWithExternalComments(CsBase element)
        {
            foreach (var document in ExternalCommentsDocuments)
            {
                if (document.Value.SelectSingleNode(GetCodeCommentsXPath(element)) != null)
                {
                    return document.Key;
                }
            }
            return null;
        }
    }
}
