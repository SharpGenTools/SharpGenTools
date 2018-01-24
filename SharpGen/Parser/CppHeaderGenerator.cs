using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpGen.Parser
{
    public class CppHeaderGenerator
    {
        private const string Version = "1.1";

        public Logger Logger { get; }
        public bool ForceParsing { get; }
        public string OutputPath { get; }

        public CppHeaderGenerator(Logger logger, bool forceParsing, string outputPath)
        {
            Logger = logger;
            ForceParsing = forceParsing;
            OutputPath = outputPath;
        }

        public (HashSet<ConfigFile> updatedConfigs, string prolog)
            GenerateCppHeaders(ConfigFile configRoot, IReadOnlyCollection<ConfigFile> filesWithIncludes, IReadOnlyCollection<string> filesWithExtensionHeaders)
        {
            var updatedConfigs = new HashSet<ConfigFile>();
            
            var prolog = new StringBuilder();

            foreach (var prologStr in configRoot.ConfigFilesLoaded.SelectMany(file => file.IncludeProlog))
            {
                prolog.Append(prologStr);
            }

            // Dump includes
            foreach (var configFile in filesWithIncludes)
            {
                using (var outputConfig = new StringWriter())
                {
                    outputConfig.WriteLine("// SharpGen include config [{0}] - Version {1}", configFile.Id, Version);

                    if (configRoot.Id == configFile.Id)
                        outputConfig.WriteLine(prolog);

                    // Write includes
                    foreach (var includeRule in configFile.Includes)
                    {
                        if (!string.IsNullOrEmpty(includeRule.Pre))
                        {
                            outputConfig.WriteLine(includeRule.Pre);
                        }
                        outputConfig.WriteLine("#include \"{0}\"", includeRule.File);
                        if (!string.IsNullOrEmpty(includeRule.Post))
                        {
                            outputConfig.WriteLine(includeRule.Post);
                        }
                    }

                    // Write includes to references
                    foreach (var reference in configFile.References)
                    {
                        if (filesWithIncludes.Contains(reference))
                            outputConfig.WriteLine("#include \"{0}\"", reference.Id + ".h");
                    }

                    // Dump Create from macros
                    if (filesWithExtensionHeaders.Contains(configFile.Id))
                    {
                        foreach (var typeBaseRule in configFile.Extension)
                        {
                            if (typeBaseRule.GeneratesExtensionHeader())
                                outputConfig.WriteLine("// {0}", typeBaseRule);
                        }
                        outputConfig.WriteLine("#include \"{0}\"", configFile.ExtensionFileName);

                        // Create Extension file name if it doesn't exist
                        if (!File.Exists(Path.Combine(OutputPath, configFile.ExtensionFileName)))
                            File.WriteAllText(Path.Combine(OutputPath, configFile.ExtensionFileName), "");
                    }

                    var outputConfigStr = outputConfig.ToString();

                    var fileName = Path.Combine(OutputPath, configFile.Id + ".h");

                    // Test if Last config file was generated. If not, then we need to generate it
                    // If it exists, then we need to test if it is the same than previous run
                    var isConfigUpdated = ForceParsing;

                    if (File.Exists(fileName) && !ForceParsing)
                        isConfigUpdated = outputConfigStr != File.ReadAllText(fileName);
                    else
                        isConfigUpdated = true;

                    // Small optim: just write the header file when the file is updated or new
                    if (isConfigUpdated)
                    {
                        if (!ForceParsing)
                            Logger.Message("Config file changed for C++ headers [{0}]/[{1}]", configFile.Id, configFile.FilePath);

                        updatedConfigs.Add(configFile);

                        if (File.Exists(fileName))
                        {
                            File.Delete(fileName);
                        }

                        using (var file = File.OpenWrite(fileName))
                        using (var fileWriter = new StreamWriter(file, Encoding.ASCII))
                        {
                            fileWriter.Write(outputConfigStr);
                        }
                    }
                }
            }

            return (updatedConfigs, prolog.ToString() + Environment.NewLine);
        }

        public static (HashSet<string> filesWithIncludes, HashSet<string> filesWithExtensionHeaders)
            GetFilesWithIncludesAndExtensionHeaders(ConfigFile configRoot)
        {
            var filesWithExtensionHeaders = new HashSet<string>();

            var filesWithIncludes = new HashSet<string>();

            // Check if the file has any includes related config
            foreach (var configFile in configRoot.ConfigFilesLoaded)
            {
                var includesAnyFiles = false;

                // Build prolog
                if (configFile.IncludeProlog.Count > 0)
                    includesAnyFiles = true;

                if (configFile.Includes.Count > 0)
                    includesAnyFiles = true;

                if (configFile.References.Count > 0)
                    includesAnyFiles = true;

                if (configFile.Extension.Any(rule => rule.GeneratesExtensionHeader()))
                {
                    filesWithExtensionHeaders.Add(configFile.Id);
                    includesAnyFiles = true;
                }

                // If this config file has any include rules
                if (includesAnyFiles)
                    filesWithIncludes.Add(configFile.Id);
            }

            return (filesWithIncludes, filesWithExtensionHeaders);
        }
    }
}
