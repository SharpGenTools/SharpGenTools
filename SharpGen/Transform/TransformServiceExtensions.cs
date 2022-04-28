using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SharpGen.Model;

namespace SharpGen.Transform;

public static class TransformServiceExtensions
{
    private static readonly Regex RegexLinkStart = new(@"^\s*\{\{.*?}}\s*(.*)", RegexOptions.Compiled);
    private static readonly Regex RegexLink = new(@"\{\{(.*?)}}", RegexOptions.Compiled);
    private static readonly Regex RegexSpaceBegin = new(@"^\s*(.*)", RegexOptions.Compiled);
    private static readonly Regex RegexWithMethodW = new("([^W])::", RegexOptions.Compiled);
    private static readonly Regex RegexWithTypeW = new("([^W])$", RegexOptions.Compiled);

    public static IEnumerable<string> GetDocItems(this IDocumentationLinker aggregator, ExternalDocCommentsReader reader, CsBase element)
    {
        var docItems = new List<string>();

        var externalCommentsPath = reader.GetDocumentWithExternalComments(element);

        if (externalCommentsPath == null)
        {
            var description = element.Description;
            if (description != CsBase.DefaultNoDescription)
            {
                description = RegexSpaceBegin.Replace(description, "$1");

                description = RegexLink.Replace(description, aggregator.ReplaceCRefReferences);
                // evaluator => "<see cref=\"$1\"/>"

                docItems.Add("<summary>");
                docItems.AddRange(description.Split('\n'));
                docItems.Add("</summary>");
            }

            element.FillDocItems(docItems, aggregator);

            var remarks = element.Remarks;
            if (!string.IsNullOrEmpty(remarks))
            {
                remarks = RegexSpaceBegin.Replace(remarks, "$1");
                remarks = RegexLink.Replace(remarks, aggregator.ReplaceCRefReferences);

                docItems.Add("<remarks>");
                docItems.AddRange(remarks.Split('\n'));
                docItems.Add("</remarks>");
            } 
        }
        else
        {
            docItems.Add($"<include file=\"{externalCommentsPath}\" path=\"{ExternalDocCommentsReader.GetCodeCommentsXPath(element)}/*\" />");
        }

        if (element.CppElementName != null)
        {
            if (element.DocId != null)
            {
                docItems.Add("<doc-id>" + EscapeXml(element.DocId) + "</doc-id>");
            }
            docItems.Add("<unmanaged>" + EscapeXml(element.DocUnmanagedName) + "</unmanaged>");
            docItems.Add("<unmanaged-short>" + EscapeXml(element.DocUnmanagedShortName) + "</unmanaged-short>");
        }

        return docItems;
    }

    public static string GetSingleDoc(this IDocumentationLinker aggregator, CsBase element)
    {
        var description = element.Description;

        if (string.IsNullOrEmpty(description) || description == CsBase.DefaultNoDescription)
            return description;

        if (RegexLinkStart.Match(description).Success)
            description = RegexLinkStart.Replace(description, "$1");

        description = RegexSpaceBegin.Replace(description, "$1");

        description = RegexLink.Replace(description, aggregator.ReplaceCRefReferences);

        var docItems = new StringBuilder();

        foreach (var line in description.Split('\n'))
        {
            docItems.Append(line);
        }

        return docItems.ToString();
    }

    public static string ReplaceCRefReferences(this IDocumentationLinker linker, Match match)
    {
        var matchName = match.Groups[1].Value;
        var csName = linker.FindDocName(matchName);

        // Tries to match with W::
        if (csName == null && RegexWithMethodW.Match(matchName).Success)
            csName = linker.FindDocName(RegexWithMethodW.Replace(matchName, "$1W::"));

        // Or with W
        if (csName == null && RegexWithTypeW.Match(matchName).Success)
            csName = linker.FindDocName(RegexWithTypeW.Replace(matchName, "$1W"));

        if (csName == null)
            return matchName;

        if (csName.StartsWith("<"))
            return csName;
        return string.Format(CultureInfo.InvariantCulture, "<see cref=\"{0}\"/>", csName);
    }

    /// <summary>
    /// Escapes the xml/html text in order to use it inside xml.
    /// </summary>
    /// <param name="stringToEscape">The string to escape.</param>
    /// <returns></returns>
    private static string EscapeXml(string stringToEscape)
    {
        return stringToEscape.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
    }
}