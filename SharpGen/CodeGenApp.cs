// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SharpGen.Logging;
using SharpGen.Config;
using SharpGen.Generator;
using SharpGen.Parser;
using System.Xml.Serialization;
using SharpGen.Model;
using SharpGen.Transform;
using SharpGen.CppModel;
using SharpGen.Doc;
#if NETSTANDARD1_5
using System.Runtime.Loader;
#endif

namespace SharpGen
{
    /// <summary>
    /// CodeGen Application.
    /// </summary>
    public class CodeGenApp
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenApp"/> class.
        /// </summary>
        public CodeGenApp(Logger logger)
        {
            Macros = new HashSet<string>();
            Logger = logger;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is generating doc.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is generating doc; otherwise, <c>false</c>.
        /// </value>
        public bool IsGeneratingDoc { get; set; }

        /// <summary>
        /// Gets or sets the path to a C++ document provider assembly.
        /// </summary>
        /// <value>The path to a C++ document provider assembly.</value>
        public string DocProviderAssemblyPath { get; set; }

        /// <summary>
        /// Gets or sets the CastXML executable path.
        /// </summary>
        /// <value>The CastXML executable path.</value>
        public string CastXmlExecutablePath { get; set; }

        /// <summary>
        /// Gets or sets output directory.
        /// </summary>
        /// <remarks>Null is allowed, in which case sharpgen will use default</remarks>
        public string OutputDirectory { get; set; }

        public bool IncludeAssemblyNameFolder { get; set; }

        public string GeneratedCodeFolder { get; set; }

        public Logger Logger { get; }

        /// <summary>
        /// Gets or sets the macros.
        /// </summary>
        /// <value>
        /// The macros.
        /// </value>
        public HashSet<string> Macros { get; set; }

        public string ConfigRootPath { get; set; }

        public string IntermediateOutputPath { get; set; } = "";

        public ConfigFile Config { get; set; }

        public string ConsumerBindMappingConfigId { get; set; }

        public GlobalNamespaceProvider GlobalNamespace { get; set; }

        private string _thisAssemblyPath;
        private bool _isAssemblyNew;
        private DateTime _assemblyDatetime;
        private string _assemblyCheckFile;
        private string _generatedPath;
        private string _allConfigCheck;



        /// <summary>
        /// Initializes the specified instance with a config root file.
        /// </summary>
        /// <returns>true if the config or assembly changed from the last run; otherwise returns false</returns>
        public bool Init()
        {
            _thisAssemblyPath = GetType().GetTypeInfo().Assembly.Location;
            _assemblyCheckFile = Path.Combine(IntermediateOutputPath, $"SharpGen.check");
            _assemblyDatetime = File.GetLastWriteTime(_thisAssemblyPath);
            _isAssemblyNew = (_assemblyDatetime != File.GetLastWriteTime(_assemblyCheckFile));
            _generatedPath = OutputDirectory ?? Path.GetDirectoryName(Path.GetFullPath(ConfigRootPath));

            Logger.Message("Loading config files...");

            if (Config == null)
            {
                Config = ConfigFile.Load(ConfigRootPath, Macros.ToArray(), Logger);
            }
            else
            {
                Config = ConfigFile.Load(Config, Macros.ToArray(), Logger);
            }

            var latestConfigTime = ConfigFile.GetLatestTimestamp(Config.ConfigFilesLoaded);
            
            _allConfigCheck = Path.Combine(IntermediateOutputPath, Config.Id + "-CodeGen.check");

            var isConfigFileChanged = !File.Exists(_allConfigCheck) || latestConfigTime > File.GetLastWriteTime(_allConfigCheck);

            if (_isAssemblyNew)
            {
                Logger.Message("Assembly [{0}] changed. All files will be generated", _thisAssemblyPath);
            }
            else if (isConfigFileChanged)
            {
                Logger.Message("Config files [{0}] changed", string.Join(",", Config.ConfigFilesLoaded.Select(file => Path.GetFileName(file.AbsoluteFilePath))));
            }


            // Return true if a config file changed or the assembly changed
            return isConfigFileChanged || _isAssemblyNew;
        }

        /// <summary>
        /// Run CodeGenerator
        /// </summary>
        public void Run()
        {
            Logger.Progress(0, "Starting code generation...");

            try
            {
                var consumerConfig = new ConfigFile
                {
                    Id = ConsumerBindMappingConfigId
                };

                var (filesWithIncludes, filesWithExtensions) = Config.GetFilesWithIncludesAndExtensionHeaders();

                var configsWithIncludes = new HashSet<ConfigFile>();

                foreach (var config in Config.ConfigFilesLoaded)
                {
                    if (filesWithIncludes.Contains(config.Id))
                    {
                        configsWithIncludes.Add(config);
                    }
                }

                var sdkResolver = new SdkResolver(Logger);

                foreach (var config in Config.ConfigFilesLoaded)
                {
                    foreach (var sdk in config.Sdks)
                    {
                        config.IncludeDirs.AddRange(sdkResolver.ResolveIncludeDirsForSdk(sdk));
                    }
                }

                var cppHeadersUpdated = GenerateHeaders(filesWithExtensions, configsWithIncludes, consumerConfig);

                if (Logger.HasErrors)
                {
                    Logger.Fatal("Failed to generate C++ headers.");
                }

                CppModule group;
                var groupFileName = $"{Config.Id}-out.xml";

                if (cppHeadersUpdated.Count != 0)
                {
                    var castXml = new CastXml(Logger, CastXmlExecutablePath)
                    {
                        OutputPath = IntermediateOutputPath,
                    };

                    castXml.Configure(Config);

                    group = GenerateExtensionHeaders(filesWithExtensions, cppHeadersUpdated, castXml);
                    group = ParseCpp(castXml, group);

                    if (IsGeneratingDoc)
                    {
                        ApplyDocumentation(group);
                    }
                }
                else
                {
                    Logger.Progress(10, "Config files unchanged. Read previous C++ parsing...");
                    if (File.Exists(Path.Combine(IntermediateOutputPath, groupFileName)))
                    {
                        group = CppModule.Read(Path.Combine(IntermediateOutputPath, groupFileName)); 
                    }
                    else
                    {
                        group = new CppModule();
                    }
                }

                // Save back the C++ parsed includes
                group.Write(Path.Combine(IntermediateOutputPath, groupFileName));

                Config.ExpandDynamicVariables(Logger, group);
                
                var (docAggregator, solution) = ExecuteMappings(group, consumerConfig);

                solution.Write(Path.Combine(IntermediateOutputPath, "Solution.xml"));

                solution = CsSolution.Read(Path.Combine(IntermediateOutputPath, "Solution.xml"));

                GenerateConfigForConsumers(consumerConfig);

                GenerateCode(docAggregator, solution);

                if (Logger.HasErrors)
                    Logger.Fatal("Code generation failed");

                // Update Checkfile for assembly
                File.WriteAllText(_assemblyCheckFile, "");
                File.SetLastWriteTime(_assemblyCheckFile, _assemblyDatetime);

                // Update Checkfile for all config files
                File.WriteAllText(_allConfigCheck, "");
                File.SetLastWriteTime(_allConfigCheck, DateTime.Now);
            }
            finally
            {
                Logger.Progress(100, "Finished");
            }
        }

        private (IDocumentationLinker doc, CsSolution solution) ExecuteMappings(CppModule group, ConfigFile consumerConfig)
        {
            var docLinker = new DocumentationLinker();
            var typeRegistry = new TypeRegistry(Logger, docLinker);
            var namingRules = new NamingRulesManager();

            // Run the main mapping process
            var transformer = new TransformManager(
                GlobalNamespace,
                namingRules,
                Logger,
                typeRegistry,
                docLinker,
                new ConstantManager(namingRules, docLinker),
                new AssemblyManager())
            {
                ForceGenerator = _isAssemblyNew
            };

            var (solution, defines) = transformer.Transform(group, Config, IntermediateOutputPath);

            consumerConfig.Extension = new List<ConfigBaseRule>(defines);

            var (bindings, generatedDefines) = transformer.GenerateTypeBindingsForConsumers();

            consumerConfig.Bindings.AddRange(bindings);
            consumerConfig.Extension.AddRange(generatedDefines);


            if (Logger.HasErrors)
                Logger.Fatal("Executing mapping rules failed");

            transformer.PrintStatistics();

            DumpRenames(transformer);

            return (docLinker, solution);
        }

        private void DumpRenames(TransformManager transformer)
        {
            using (var renameLog = File.Open(Path.Combine(IntermediateOutputPath, "SharpGen_rename.log"), FileMode.OpenOrCreate, FileAccess.Write))
            using (var fileWriter = new StreamWriter(renameLog))
            {
                transformer.NamingRules.DumpRenames(fileWriter);
            }
        }

        private CppModule ParseCpp(CastXml castXml, CppModule group)
        {

            // Run the parser
            var parser = new CppParser(Logger, castXml)
            {
                OutputPath = IntermediateOutputPath
            };
            parser.Initialize(Config);

            if (Logger.HasErrors)
                Logger.Fatal("Initializing parser failed");

            // Run the parser
            group = parser.Run(group);

            if (Logger.HasErrors)
            {
                Logger.Fatal("Parsing C++ failed.");
            }
            else
            {
                Logger.Message("Parsing C++ finished");
            }

            // Print statistics
            parser.PrintStatistics();

            return group;
        }

        private CppModule GenerateExtensionHeaders(IReadOnlyCollection<string> filesWithExtensions, IReadOnlyCollection<ConfigFile> updatedConfigs, CastXml castXml)
        {
            Logger.Progress(10, "Generating C++ extensions from macros");

            var cppExtensionHeaderGenerator = new CppExtensionHeaderGenerator(new MacroManager(castXml));

            var group = cppExtensionHeaderGenerator.GenerateExtensionHeaders(Config, IntermediateOutputPath, filesWithExtensions, updatedConfigs);
            return group;
        }

        private HashSet<ConfigFile> GenerateHeaders(IReadOnlyCollection<string> filesWithExtensions, IReadOnlyCollection<ConfigFile> configsWithIncludes, ConfigFile consumerConfig)
        {
            var headerGenerator = new CppHeaderGenerator(Logger, _isAssemblyNew, IntermediateOutputPath);

            var (cppHeadersUpdated, cppConsumerConfig) = headerGenerator.GenerateCppHeaders(Config, configsWithIncludes, filesWithExtensions);

            consumerConfig.IncludeProlog.AddRange(cppConsumerConfig.IncludeProlog);
            consumerConfig.IncludeDirs.AddRange(cppConsumerConfig.IncludeDirs);
            consumerConfig.Includes.AddRange(cppConsumerConfig.Includes);
            return cppHeadersUpdated;
        }

        private void GenerateCode(IDocumentationLinker docAggregator, CsSolution solution)
        {
            var generator = new RoslynGenerator(Logger, GlobalNamespace, docAggregator);
            generator.Run(solution, _generatedPath, GeneratedCodeFolder, IncludeAssemblyNameFolder);

            // Update check files for all assemblies
            var processTime = DateTime.Now;
            foreach (CsAssembly assembly in solution.Assemblies)
            {
                File.WriteAllText(Path.Combine(IntermediateOutputPath, assembly.CheckFileName), "");
                File.SetLastWriteTime(Path.Combine(IntermediateOutputPath, assembly.CheckFileName), processTime);
            }
        }

        private void GenerateConfigForConsumers(ConfigFile consumerConfig)
        {
            var consumerBindMappingFileName = Path.Combine(IntermediateOutputPath, $"{ConsumerBindMappingConfigId ?? Config.Id}.BindMapping.xml");

            if (File.Exists(consumerBindMappingFileName))
            {
                File.Delete(consumerBindMappingFileName);
            }

            using (var consumerBindMapping = File.Open(consumerBindMappingFileName, FileMode.OpenOrCreate, FileAccess.Write))
            using (var fileWriter = new StreamWriter(consumerBindMapping))
            {
                var serializer = new XmlSerializer(typeof(ConfigFile));
                serializer.Serialize(fileWriter, consumerConfig);
            }
        }

        /// <summary>
        /// Apply documentation from an external provider. This is optional.
        /// </summary>
        private CppModule ApplyDocumentation(CppModule group)
        {
            // Use default MSDN doc provider
            IDocProvider docProvider = new MsdnProvider(Logger);

            // Try to load doc provider from an external assembly
            if (DocProviderAssemblyPath != null)
            {
                try
                {
#if NETSTANDARD1_5
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(DocProviderAssemblyPath);
#else
                    var assembly = Assembly.LoadFrom(DocProviderAssemblyPath);
#endif

                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(IDocProvider).GetTypeInfo().IsAssignableFrom(type))
                        {
                            docProvider = (IDocProvider)Activator.CreateInstance(type);
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    Logger.Warning("Warning, Unable to locate/load DocProvider Assembly.");
                    Logger.Warning("Warning, DocProvider was not found from assembly [{0}]", DocProviderAssemblyPath);
                }
            }

            Logger.Progress(20, "Applying C++ documentation");
            return docProvider.ApplyDocumentation(group);
        }
    }
}