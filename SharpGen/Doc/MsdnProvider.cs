using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SharpGen.Logging;
using System.IO.Compression;
using Newtonsoft.Json.Linq;
using MTPS;

namespace SharpGen.Doc
{
    public class MsdnProvider : IDocProvider
    {
        private static readonly Regex StripSpace = new Regex(@"[\r\n]+\s+", RegexOptions.Multiline);
        private static readonly Regex BeginWithWhitespace = new Regex(@"^\s+");
        private readonly Dictionary<Regex, string> CommonReplaceRuleMap;
        private ZipArchive _zipFile;
        private bool isZipUpdated;
        private string archiveFullPath;
        private string shadowCopyFullPath;

        public MsdnProvider(Logger logger)
        {
            CommonReplaceRuleMap = new Dictionary<Regex, string>();
            ReplaceName("W::", @"::");
            ReplaceName("([a-z0-9])A::", @"$1::");
            ReplaceName("W$", @"");
            ReplaceName("^_+", @"");
            Logger = logger;
        }

        private void ReplaceName(string fromNameRegex, string toName)
        {
            CommonReplaceRuleMap.Add(new Regex(fromNameRegex), toName);
        }

        /// <summary>
        /// Archive to use to save the documentation
        /// </summary>
        private string ArchiveName => "MSDNDoc.zip";

        /// <summary>
        /// Output path for the archive / Directory
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Shadow copy the archive when running. Defaults to true.
        /// </summary>
        public bool ShadowCopy { get; set; } = true;

        public Logger Logger { get; private set; }

        /// <summary>
        /// Begin to request MSDN
        /// </summary>
        public void Begin()
        {
            var fullPath = Path.Combine((OutputPath ?? "."), ArchiveName);

            var outputDirectory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);

            archiveFullPath = Path.Combine(outputDirectory, ArchiveName);
            shadowCopyFullPath = ShadowCopy ? Path.GetTempFileName() : archiveFullPath;
            OpenArchive();
        }

        /// <summary>
        /// End request to MSDN. Archive is saved if any updated occurred between Begin/End.
        /// </summary>
        public void End()
        {
            CloseArchive();
        }

        private void OpenArchive()
        {
            if (_zipFile == null)
            {
                isZipUpdated = false;
                if (ShadowCopy)
                {
                    if (File.Exists(archiveFullPath))
                    {
                        File.Copy(archiveFullPath, shadowCopyFullPath, true);
                    }
                    _zipFile = ZipFile.Open(shadowCopyFullPath, ZipArchiveMode.Update);
                }
                else
                {
                    _zipFile = ZipFile.Open(archiveFullPath, ZipArchiveMode.Update);
                }
            }
        }

        private void CloseArchive()
        {
            if (_zipFile != null)
            {
                _zipFile.Dispose();
                if (isZipUpdated && ShadowCopy)
                {
                    File.Copy(shadowCopyFullPath, archiveFullPath, true);
                }
                if (ShadowCopy)
                {
                    File.Delete(shadowCopyFullPath); 
                }
                _zipFile = null;
            }
        }

        private int counter;


        /// <summary>
        /// Get the documentation for a particular prefix (include name) and a full name item
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DocItem FindDocumentation(string name)
        {
            var oldName = name;
            // Regex replacer
            foreach (var keyValue in CommonReplaceRuleMap)
            {
                if (keyValue.Key.Match(name).Success)
                {
                    name = keyValue.Key.Replace(name, keyValue.Value);
                    break;
                }
            }

            // Handle name with ends A or W
            if (name.EndsWith("A") || name.EndsWith("W"))
            {
                var previouewChar = new string(name[name.Length - 2], 1);

                if (previouewChar.ToUpper() != previouewChar)
                {
                    name = name.Substring(0, name.Length - 1);
                }
            }

            var doc = GetDocumentationFromCacheOrMsdn(name);
            if (doc == null)
            {
                return new DocItem { Description = "No documentation" };
            }
            return ParseDocumentation(doc);
        }

        /// <summary>
        /// Handles documentation from zip/directory
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetDocumentationFromCacheOrMsdn(string name)
        {
            var fileName = name.Replace("::", "-") + ".html";
            counter++;

            string doc;

            OpenArchive();

            var zipEntry = _zipFile.GetEntry(fileName);
            if (zipEntry != null)
            {
                using (var streamInput = zipEntry.Open())
                using (var reader = new StreamReader(streamInput))
                {
                    doc = reader.ReadToEnd();
                }
            }
            else
            {
                // Begin update if zip is not updated
                if (!isZipUpdated)
                {
                    isZipUpdated = true;
                }

                Logger.Progress(20 + (counter / 50) % 10, "Fetching C++ documentation ([{0}]) from MSDN", name);

                doc = GetDocumentationFromMsdn(name);

                var newEntry = _zipFile.CreateEntry(fileName);
                using (var stream = newEntry.Open())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(doc);
                }
            }
            return doc;
        }


        private static readonly HashSet<string> HtmlPreserveTags = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase) { "dl", "dt", "dd", "p", "strong", "pre", "em", "code", "ul", "ol", "li", "table", "tr", "th", "td" };

        private static string ParseSubNodes(HtmlNode htmlNode, bool isRoot)
        {
            StringBuilder documentation = new StringBuilder();

            bool isDocClear = false;

            string htmlNodeName = htmlNode.Name.ToLower();
            if (HtmlPreserveTags.Contains(htmlNodeName))
                documentation.Append("<").Append(htmlNodeName).Append(">");
            
            if (htmlNode.Name == "a")
            {
                StringBuilder inside = new StringBuilder();
                foreach (var node in htmlNode.ChildNodes)
                    inside.Append(ParseSubNodes(node, false).Trim());
                string insideStr = inside.ToString();

                if (!string.IsNullOrEmpty(insideStr) && insideStr != "Copy")
                {
                    documentation.Append("{{");
                    insideStr = insideStr.Trim().Split(' ', '\t')[0];
                    documentation.Append(insideStr);
                    documentation.Append("}}");
                }
                return documentation.ToString();
            }
            else if (htmlNode.NodeType == HtmlNodeType.Text)
            {
                string text = htmlNode.InnerText;
                if (BeginWithWhitespace.Match(text).Success)
                    text = BeginWithWhitespace.Replace(text, " ");
                if (StripSpace.Match(text).Success)
                    text = StripSpace.Replace(text, " ");
                return text;
            }

            foreach (var node in htmlNode.ChildNodes)
            {
                string text = ParseSubNodes(node, false);

                if (text.StartsWith("Type:"))
                {
                    isDocClear = true;
                }
                else
                {
                    documentation.Append(text);
                }
            }
            
            if (!isDocClear)
            {
                if (HtmlPreserveTags.Contains(htmlNodeName))
                    documentation.Append("</").Append(htmlNodeName).Append(">");
            }

            if (isDocClear)
                documentation.Clear();

            return documentation.ToString();
        }

        private static readonly Regex regexCapitals = new Regex(@"([^0-9A-Za-z_:\{])([A-Z][A-Z0-9_][0-9A-Za-z_:]*)");


        /// <summary>
        /// Parse HtmlNode to extract a string from it. Replace anchors href with {{ }} 
        /// and code with [[ ]]
        /// </summary>
        /// <param name="htmlNode"></param>
        /// <returns></returns>
        private static string ParseNode(HtmlNode htmlNode)
        {
            var result = ParseSubNodes(htmlNode, true);
            result = regexCapitals.Replace(result, "$1{{$2}}");
            result = result.Replace("pointer", "reference");
            result = result.Trim();
            return result;
        }

        private static string GetTextUntilNextHeader(HtmlNode htmlNode, bool skipFirstNode = true, params string[] untilNodes)
        {
            if (skipFirstNode)
                htmlNode = htmlNode.NextSibling;

            while (htmlNode != null && htmlNode.Name.ToLower() == "div")
            {
                htmlNode = htmlNode.FirstChild;
            }
            if (htmlNode == null)
                return string.Empty;

            var builder = new StringBuilder();
            var nodes = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase) { "h3", "h2", "mtps:collapsiblearea" };
            foreach (var untilNode in untilNodes)
            {
                nodes.Add(untilNode);
            }

            while (htmlNode != null && !nodes.Contains(htmlNode.Name.ToLower()))
            {
                builder.Append(ParseNode(htmlNode));
                htmlNode = htmlNode.NextSibling;
            }

            return builder.ToString();
        }

        private static string ParseNextDiv(HtmlNode htmlNode)
        {
            while (htmlNode != null)
            {
                if (htmlNode.Name == "div" || htmlNode.Name.ToLower() == "mtps:collapsiblearea")
                    return ParseNode(htmlNode);
                htmlNode = htmlNode.NextSibling;
            }
            return "";
        }

        /// <summary>
        /// Parse a MSDN documentation file
        /// </summary>
        /// <param name="documentationToParse"></param>
        /// <returns></returns>
        public static DocItem ParseDocumentation(string documentationToParse)
        {
            if (string.IsNullOrEmpty(documentationToParse))
                return new DocItem();

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(documentationToParse);

            var item = new DocItem { Id = htmlDocument.DocumentNode.ChildNodes.FindFirst("id").InnerText };

            var element = htmlDocument.GetElementbyId("mainSection");

            // Page not found?
            if (element == null)
                return item;

            // Get description before h3/collasiblearea and table
            item.Description = GetTextUntilNextHeader(element.FirstChild, false, "table");

            HtmlNode firstElement = element.ChildNodes.FindFirst("dl");
            if (firstElement != null)
            {
                string termName = null;
                List<string> currentDoc = new List<string>();
                var nodes = firstElement.ChildNodes;
                foreach (HtmlNode htmlNode in nodes)
                {
                    if (htmlNode.Name == "dt")
                    {
                        if (currentDoc.Count > 0)
                        {
                            item.Items.Add(new DocSubItem
                            {
                                Term = termName,
                                Description = currentDoc[currentDoc.Count - 1]
                            });
                            currentDoc.Clear();
                            termName = htmlNode.InnerText;
                        }
                    }
                    else if (htmlNode.Name == "dd")
                    {
                        currentDoc.Add(ParseNode(htmlNode));
                    }
                }
                if (currentDoc.Count > 0)
                    item.Items.Add(new DocSubItem
                    {
                        Term = termName,
                        Description = currentDoc[currentDoc.Count - 1]
                    });
            }
            var headerCollection = element.SelectNodes("//h3 | //h2");
            if (headerCollection != null)
            {
                foreach (HtmlNode htmlNode in headerCollection)
                {
                    string text = ParseNode(htmlNode);
                    if (text.StartsWith("Remarks"))
                        item.Remarks = GetTextUntilNextHeader(htmlNode);
                    else if (text.StartsWith("Return"))
                        item.Return = GetTextUntilNextHeader(htmlNode);
                }
            }
            else
            {
                var returnCollection = element.SelectNodes("//h4[contains(.,'Return')]");
                if (returnCollection != null)
                    item.Return = ParseNextDiv(returnCollection[0].NextSibling);

                var remarksCollection = element.SelectNodes("//a[@id='remarks']");
                if (remarksCollection != null)
                {
                    item.Remarks = ParseNextDiv(remarksCollection[0].NextSibling);
                }
            }
            return item;
        }
        
        /// <summary>
        /// Get MSDN documentation using an http query
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static string GetDocumentationFromMsdn(string name)
        {

            var shortId = GetShortId(name);
            if (string.IsNullOrEmpty(shortId))
                return string.Empty;

            var result = GetDocFromMTPS(shortId);
            if (string.IsNullOrEmpty(result))
                return string.Empty;
            return "<id>" + shortId + "</id>\r\n" + result;
        }

        private static ContentServicePortTypeClient proxy;

        public static string GetDocFromMTPS(string shortId)
        {
            try
            {
                if (proxy == null)
                    proxy = new ContentServicePortTypeClient(ContentServicePortTypeClient.EndpointConfiguration.ContentService);
                

                var request = new getContentRequest
                {
                    contentIdentifier = shortId,
                    locale = "en-us",
                    version = "VS.85",
                    requestedDocuments = new[] { new requestedDocument { type = documentTypes.primary, selector = "Mtps.Xhtml" } }
                };
                var response = proxy.GetContent(new appId { value = "Sandcastle" }, request);
                if (response.primaryDocuments[0].Any != null)
                    return response.primaryDocuments[0].Any.OuterXml;
            }
            catch (Exception)
            {
                return string.Empty;
            }
            return string.Empty;
        }



        private static Regex matchId = new Regex(@"/([a-zA-Z0-9\._\-]+)(\(.+\).*|\.[a-zA-Z]+)?$");

        public static string GetShortId(string name)
        {
            try
            {
                var searchUrl = "http://social.msdn.microsoft.com/Search/en-US?query=" + WebUtility.UrlEncode(name) + "&addenglish=1";

                var result = GetFromUrl(searchUrl);

                if (string.IsNullOrEmpty(result))
                    return string.Empty;

                var resultsStart = "var results = ";
                var indexOfResults = result.IndexOf(resultsStart, StringComparison.Ordinal) + resultsStart.Length;
                if (indexOfResults > 0)
                {
                    var endOfLine = result.IndexOf('\n', indexOfResults) - 1;
                    var resultsText = result.Substring(indexOfResults, endOfLine - indexOfResults);
                    var endJsonSemicolon = resultsText.LastIndexOf(';');
                    var resultsJson = resultsText.Substring(0, endJsonSemicolon);
                    var urlResult = JObject.Parse(resultsJson);
                    var contentUrl = ((JArray)(urlResult["data"])["results"])[0]["url"].ToString();
                    var match = matchId.Match(contentUrl);
                    if (match.Success)
                        return match.Groups[1].Value;
                }
            }
            catch (Exception)
            {
            }

            return string.Empty;
        }

        internal static string GetFromUrl(string url)
        {
            try
            {
                // Create web request
                var request = (HttpWebRequest)WebRequest.Create(url);

                // Set value for request headers

                request.Method = "GET";
                request.Accept = "*/*";
                
                HttpWebResponse webResponse = null;
                // Get response for http web request
                webResponse = (HttpWebResponse)request.GetResponseAsync().Result;
                using (var responseStream = new StreamReader(webResponse.GetResponseStream()))
                {
                    return responseStream.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return string.Empty;
        }
    }
}
