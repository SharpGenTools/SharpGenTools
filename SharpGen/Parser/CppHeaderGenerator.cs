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
    class CppHeaderGenerator
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

        public (bool anyConfigUpdated, ConfigFile consumerConfig)
            GenerateCppHeaders(ConfigFile configRoot, HashSet<ConfigFile> filesWithIncludes, HashSet<string> filesWithExtensionHeaders)
        {
            var anyConfigsUpdated = ForceParsing;
            
            var includeDirsForConsumers = new HashSet<IncludeDirRule>();
            var includesForConsumers = new HashSet<IncludeRule>();
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
                            includesForConsumers.Add(includeRule);
                            foreach (var includeDir in configFile.IncludeDirs)
                            {
                                includeDirsForConsumers.Add(includeDir);
                            }
                        }
                        outputConfig.WriteLine("#include \"{0}\"", includeRule.File);
                        if (!string.IsNullOrEmpty(includeRule.Post))
                        {
                            outputConfig.WriteLine(includeRule.Post);
                            includesForConsumers.Add(includeRule);
                            foreach (var includeDir in configFile.IncludeDirs)
                            {
                                includeDirsForConsumers.Add(includeDir);
                            }
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
                            if (RuleGeneratesExtensionHeader(typeBaseRule))
                                outputConfig.WriteLine("// {0}", typeBaseRule);
                        }
                        outputConfig.WriteLine("#include \"{0}\"", configFile.ExtensionFileName);

                        // Create Extension file name if it doesn't exist);
                        if (!File.Exists(Path.Combine(OutputPath, configFile.ExtensionFileName)))
                            File.WriteAllText(Path.Combine(OutputPath, configFile.ExtensionFileName), "");
                    }

                    var outputConfigStr = outputConfig.ToString();

                    var fileName = Path.Combine(OutputPath, configFile.Id + ".h");

                    // Test if Last config file was generated. If not, then we need to generate it
                    // If it exists, then we need to test if it is the same than previous run
                    configFile.IsConfigUpdated = ForceParsing;

                    if (File.Exists(fileName) && !ForceParsing)
                        configFile.IsConfigUpdated = outputConfigStr != File.ReadAllText(fileName);
                    else
                        configFile.IsConfigUpdated = true;

                    // Small optim: just write the header file when the file is updated or new
                    if (configFile.IsConfigUpdated)
                    {
                        if (!ForceParsing)
                            Logger.Message("Config file changed for C++ headers [{0}]/[{1}]", configFile.Id, configFile.FilePath);

                        anyConfigsUpdated = true;

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

            var consumerConfig = new ConfigFile
            {
                IncludeProlog =
                {
                    prolog.ToString() + Environment.NewLine
                },
                IncludeDirs = includeDirsForConsumers.ToList(),
                Includes = includesForConsumers.Select(include =>
                    new IncludeRule
                    {
                        File = include.File,
                        Pre = include.Pre,
                        Post = include.Post,
                        FilterErrors = include.FilterErrors
                    }).ToList()
            };

            return (anyConfigsUpdated, consumerConfig);
        }


        /// <summary>
        /// Checks if this rule is creating headers extension.
        /// </summary>
        /// <param name="rule">The rule to check.</param>
        /// <returns>true if the rule is creating an header extension.</returns>
        private static bool RuleGeneratesExtensionHeader(ConfigBaseRule rule)
        {
            return (rule is CreateCppExtensionRule createCpp && !string.IsNullOrEmpty(createCpp.Macro))
                || (rule is ConstantRule constant && !string.IsNullOrEmpty(constant.Macro));
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

                if (configFile.Extension.Any(rule => RuleGeneratesExtensionHeader(rule)))
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
