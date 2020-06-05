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
        public string OutputPath { get; }

        public CppHeaderGenerator(Logger logger, string outputPath)
        {
            Logger = logger;
            OutputPath = outputPath;
        }

        public readonly struct Result
        {
            public HashSet<ConfigFile> UpdatedConfigs { get; }
            public string Prologue { get; }

            public Result(HashSet<ConfigFile> updatedConfigs, string prolog)
            {
                UpdatedConfigs = updatedConfigs ?? throw new ArgumentNullException(nameof(updatedConfigs));
                Prologue = prolog ?? throw new ArgumentNullException(nameof(prolog));
            }
        }

        public Result GenerateCppHeaders(ConfigFile configRoot, IReadOnlyCollection<ConfigFile> filesWithIncludes,
                                         ISet<ConfigFile> filesWithExtensionHeaders)
        {
            var updatedConfigs = new HashSet<ConfigFile>(ConfigFile.IdComparer);

            var prologue = GeneratePrologue(configRoot);

            // Dump includes
            foreach (var configFile in filesWithIncludes)
            {
                var outputConfigStr = GenerateIncludeConfigContents(
                    configRoot, configFile, filesWithIncludes, filesWithExtensionHeaders, prologue
                );

                var fileName = Path.Combine(OutputPath, configFile.HeaderFileName);

                // Test if Last config file was generated. If not, then we need to generate it
                // If it exists, then we need to test if it is the same than previous run
                bool isConfigUpdated;

                if (File.Exists(fileName))
                    isConfigUpdated = outputConfigStr != File.ReadAllText(fileName);
                else
                    isConfigUpdated = true;

                // Small optim: just write the header file when the file is updated or new
                if (!isConfigUpdated)
                    continue;

                Logger.Message("Config file changed for C++ headers [{0}]/[{1}]", configFile.Id, configFile.FilePath);

                updatedConfigs.Add(configFile);

                using var file = File.OpenWrite(fileName);
                using var fileWriter = new StreamWriter(file, Encoding.ASCII);
                fileWriter.Write(outputConfigStr);
            }

            return new Result(updatedConfigs, prologue);
        }

        private static string GeneratePrologue(ConfigFile configRoot)
        {
            var prolog = new StringBuilder();

            foreach (var prologItem in configRoot.ConfigFilesLoaded.SelectMany(file => file.IncludeProlog))
            {
                prolog.Append(prologItem);
            }

            prolog.AppendLine();

            return prolog.ToString();
        }

        private static string GenerateIncludeConfigContents(ConfigFile configRoot, ConfigFile configFile,
                                                            IReadOnlyCollection<ConfigFile> filesWithIncludes,
                                                            ISet<ConfigFile> filesWithExtensionHeaders,
                                                            string prolog)
        {
            using var outputConfig = new StringWriter();

            outputConfig.WriteLine("// SharpGen include config [{0}] - Version {1}", configFile.Id, Version);

            if (configRoot.Id == configFile.Id)
                outputConfig.Write(prolog);

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
                    outputConfig.WriteLine("#include \"{0}\"", reference.HeaderFileName);
            }

            // Dump Create from macros
            if (filesWithExtensionHeaders.Contains(configFile))
            {
                foreach (var typeBaseRule in configFile.Extension)
                {
                    if (typeBaseRule.GeneratesExtensionHeader())
                        outputConfig.WriteLine("// {0}", typeBaseRule);
                }

                // Include extension header if it exists
                // so we can generate extension headers without needing them to already exist.
                outputConfig.WriteLine("#if __has_include(\"{0}\")", configFile.ExtensionFileName);
                outputConfig.WriteLine("#include \"{0}\"", configFile.ExtensionFileName);
                outputConfig.WriteLine("#endif");
            }

            var outputConfigStr = outputConfig.ToString();
            return outputConfigStr;
        }
    }
}
