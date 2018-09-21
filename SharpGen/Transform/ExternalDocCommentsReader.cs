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
            return $"/comments/comment[@id='{GetExternalDocCommentId(element)}']";
        }

        public ExternalDocCommentsReader(Dictionary<string, XmlDocument> externalCommentsDocuments)
        {
            ExternalCommentsDocuments = externalCommentsDocuments;
        }

        public Dictionary<string, XmlDocument> ExternalCommentsDocuments { get; }

        public string GetDocumentWithExternalComments(CsBase element)
        {
            string externalDocCommentId = GetExternalDocCommentId(element);
            foreach (var document in ExternalCommentsDocuments)
            {
                foreach (XmlNode topLevelNode in document.Value.ChildNodes)
                {
                    if (topLevelNode.Name == "comments")
                    {
                        foreach (XmlNode node in topLevelNode.ChildNodes)
                        {
                            if (node.Name == "comment" && node.Attributes["id"].Value == externalDocCommentId)
                            {
                                return document.Key;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static string GetExternalDocCommentId(CsBase element)
        {
            return element.CppElementName ?? element.QualifiedName;
        }
    }
}
