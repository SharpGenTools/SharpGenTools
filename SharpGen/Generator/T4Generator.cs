using SharpGen.Logging;
using SharpGen.Model;
using SharpGen.TextTemplating;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGen.Generator
{
    public class T4Generator
    {
        private readonly IDocumentationAggregator docAggregator;

        public Logger Logger { get; }
        public GlobalNamespaceProvider GlobalNamespace { get; }
        public string AppType { get; }

        public T4Generator(Logger logger, IDocumentationAggregator aggregator, GlobalNamespaceProvider globalNamespace, string appType)
        {
            Logger = logger;
            docAggregator = aggregator;
            GlobalNamespace = globalNamespace;
            AppType = appType;
        }

        public IEnumerable<string> GetDocItems(CsBase item) => docAggregator.GetDocItems(item);

        public void Run(string generatedCodeFolder, IEnumerable<CsAssembly> assemblies)
        {
            if (Logger.HasErrors)
                Logger.Fatal("Transform failed");

            // Configure TextTemplateEngine
            var engine = new TemplateEngine(Logger);
            engine.OnInclude += OnInclude;
            engine.SetParameter("Generator", this);

            int indexToGenerate = 0;
            var templateNames = new[] { "Enumerations", "Structures", "Interfaces", "Functions", "LocalInterop" };

            var directoryToCreate = new HashSet<string>(StringComparer.CurrentCulture);

            // Iterates on templates
            foreach (string templateName in templateNames)
            {
                Logger.Progress(85 + (indexToGenerate * 15 / templateNames.Length), "Generating code for {0}...", templateName);
                indexToGenerate++;

                Logger.Message("\nGenerate {0}", templateName);
                string templateFileName = templateName + ".tt";

                string input = Utilities.GetResourceAsString("Templates." + templateFileName);

                // Iterates on assemblies
                foreach (var csAssembly in assemblies)
                {
                    if (!csAssembly.IsToUpdate)
                        continue;

                    engine.SetParameter("Assembly", csAssembly);

                    string generatedDirectoryForAssembly = Path.Combine(csAssembly.RootDirectory, generatedCodeFolder ?? "Generated", AppType);

                    // Remove the generated directory before creating it
                    if (!directoryToCreate.Contains(generatedDirectoryForAssembly))
                    {
                        directoryToCreate.Add(generatedDirectoryForAssembly);
                        if (Directory.Exists(generatedDirectoryForAssembly))
                        {
                            foreach (var oldGeneratedFile in Directory.EnumerateFiles(generatedDirectoryForAssembly, "*.cs", SearchOption.AllDirectories))
                            {
                                try
                                {
                                    File.Delete(oldGeneratedFile);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }

                    if (!Directory.Exists(generatedDirectoryForAssembly))
                        Directory.CreateDirectory(generatedDirectoryForAssembly);

                    Logger.Message("Process Assembly {0} => {1}", csAssembly.Name, generatedDirectoryForAssembly);

                    // LocalInterop is once generated per assembly
                    if (templateName == "LocalInterop")
                    {
                        Logger.Message("\tProcess Interop {0} => {1}", csAssembly.Name, generatedDirectoryForAssembly);

                        //Transform the text template.
                        string output = engine.ProcessTemplate(input, templateName);
                        string outputFileName = Path.GetFileNameWithoutExtension(templateFileName);

                        outputFileName = Path.Combine(generatedDirectoryForAssembly, outputFileName);
                        outputFileName = outputFileName + ".cs";
                        File.WriteAllText(outputFileName, output, Encoding.ASCII);
                    }
                    else
                    {
                        // Else, iterates on each namespace
                        foreach (var csNamespace in csAssembly.Namespaces)
                        {
                            engine.SetParameter("Namespace", csNamespace);

                            string subDirectory = csNamespace.OutputDirectory ?? ".";

                            string nameSpaceDirectory = generatedDirectoryForAssembly + "\\" + subDirectory;
                            if (!Directory.Exists(nameSpaceDirectory))
                                Directory.CreateDirectory(nameSpaceDirectory);

                            Logger.Message("\tProcess Namespace {0} => {1}", csNamespace.Name, nameSpaceDirectory);

                            //Transform the text template.
                            string output = engine.ProcessTemplate(input, templateName);
                            string outputFileName = Path.GetFileNameWithoutExtension(templateFileName);

                            outputFileName = Path.Combine(nameSpaceDirectory, outputFileName);
                            outputFileName = outputFileName + ".cs";
                            File.WriteAllText(outputFileName, output, Encoding.ASCII);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Callback used by the text templating engine.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private static void OnInclude(object sender, TemplateIncludeArgs e)
        {
            e.Text = Utilities.GetResourceAsString("Templates." + e.IncludeName);
        }
    }
}
