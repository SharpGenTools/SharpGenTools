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
        /// Gets or sets the path to the Visual C++ toolset
        /// </summary>
        /// <value>The Visual C++ toolset path</value>
        public string VcToolsPath { get; set; }

        /// <summary>
        /// Gets or sets the app configuration.
        /// </summary>
        /// <value>The application configuration</value>
        public string AppType { get; set; }

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
            _assemblyCheckFile = Path.Combine(IntermediateOutputPath, $"SharpGen.{AppType}.check");
            _assemblyDatetime = File.GetLastWriteTime(_thisAssemblyPath);
            _isAssemblyNew = (_assemblyDatetime != File.GetLastWriteTime(_assemblyCheckFile));
            _generatedPath = OutputDirectory ?? Path.GetDirectoryName(Path.GetFullPath(ConfigRootPath));

            Logger.Message("Loading config files...");
            
            Macros.Add(AppType);

            if (Config == null)
            {
                Config = ConfigFile.Load(ConfigRootPath, Macros.ToArray(), Logger, new KeyValue("VC_TOOLS_PATH", VcToolsPath));
            }
            else
            {
                Config = ConfigFile.Load(Config, Macros.ToArray(), Logger, new KeyValue("VC_TOOLS_PATH", VcToolsPath));
            }

            var latestConfigTime = ConfigFile.GetLatestTimestamp(Config.ConfigFilesLoaded);
            
            _allConfigCheck = Path.Combine(IntermediateOutputPath, Config.Id + "-" + AppType + "-CodeGen.check");

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
                // Run the parser
                var parser = new Parser.CppParser(GlobalNamespace, Logger)
                                 {
                                     IsGeneratingDoc = IsGeneratingDoc,
                                     DocProviderAssembly = DocProviderAssemblyPath,
                                     ForceParsing = _isAssemblyNew,
                                     CastXmlExecutablePath = CastXmlExecutablePath,
                                     OutputPath = IntermediateOutputPath
                                 };

                // Init the parser
                parser.Init(Config);

                if (Logger.HasErrors)
                    Logger.Fatal("Initializing parser failed");

                // Run the parser
                var group = parser.Run();

                if (Logger.HasErrors)
                    Logger.Fatal("C++ compiler failed to parse header files");

                // Run the main mapping process
                var transformer = new TransformManager(GlobalNamespace, Logger)
                {
                    GeneratedPath = _generatedPath,
                    IncludeAssemblyNameFolder = IncludeAssemblyNameFolder,
                    GeneratedCodeFolder = GeneratedCodeFolder,
                    ForceGenerator = _isAssemblyNew,
                    AppType = AppType
                };

                transformer.Init(group, Config, IntermediateOutputPath);

                if (Logger.HasErrors)
                    Logger.Fatal("Mapping rules initialization failed");

                transformer.Generate(IntermediateOutputPath);

                if (Logger.HasErrors)
                    Logger.Fatal("Code generation failed");


                // Print statistics
                parser.PrintStatistics();
                transformer.PrintStatistics();

                // Output all elements
                using (var renameLog = File.Open(Path.Combine(IntermediateOutputPath, "SharpGen_rename.log"), FileMode.OpenOrCreate, FileAccess.Write))
                using (var fileWriter = new StreamWriter(renameLog))
                {
                    transformer.NamingRules.DumpRenames(fileWriter);
                }

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
    }
}