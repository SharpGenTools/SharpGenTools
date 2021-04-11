using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen;
using SharpGen.Config;
using SharpGen.Generator;
using SharpGen.Logging;
using SharpGen.Model;
using SharpGen.Parser;
using SharpGen.Platform;
using SharpGen.Transform;
using ILogger = SharpGen.Logging.ILogger;
using Logger = SharpGen.Logging.Logger;
using SdkResolver = SharpGen.Parser.SdkResolver;

namespace SharpGenTools.Sdk.Test
{
    class Program
    {
        protected Logger SharpGenLogger { get; private set; }

        private ConfigFile LoadConfig(ConfigFile config)
        {
            config = ConfigFile.Load(config, Array.Empty<string>(), SharpGenLogger);

            var sdkResolver = new SdkResolver(SharpGenLogger);
            SharpGenLogger.Message("Resolving SDKs...");
            foreach (var cfg in config.ConfigFilesLoaded)
            {
                SharpGenLogger.Message("Resolving SDK for Config {0}", cfg);
                foreach (var sdk in cfg.Sdks)
                {
                    SharpGenLogger.Message("Resolving {0}: Version {1}", sdk.Name, sdk.Version);
                    foreach (var directory in sdkResolver.ResolveIncludeDirsForSdk(sdk))
                    {
                        SharpGenLogger.Message("Resolved include directory {0}", directory);
                        cfg.IncludeDirs.Add(directory);
                    }
                }
            }

            return config;
        }

        protected void PrepareExecute()
        {
            SharpGenLogger = new Logger(new ConsoleSharpGenLogger());

            // WaitForDebuggerAttach();
        }

        private void WaitForDebuggerAttach()
        {
            if (!Debugger.IsAttached)
            {
                SharpGenLogger.Warning(
                    null, $"{GetType().Name} is waiting for attach: {Process.GetCurrentProcess().Id}");
                Thread.Yield();
            }

            while (!Debugger.IsAttached)
                Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        static void Main(string[] args)
        {
            var program = new Program();
            program.PrepareExecute();
            program.Run();
        }

        private void Run()
        {
            var ConfigFiles = new ITaskItem[]
            {
                new TaskItem(@"D:\dev\SharpGenTools\SdkTests\Struct\Mapping.xml"),
                new TaskItem(@"D:\dev\SharpGenTools\SharpGen.Runtime\Mapping.xml")
            };
            var OutputPath = @"D:\dev\SharpGenTools\NextGen\Struct\x64\Debug\net5.0\";
            var GeneratedCodeFolder = OutputPath;

            if (!Directory.Exists(OutputPath))
                Directory.CreateDirectory(OutputPath);

            var config = new ConfigFile
            {
                Files = ConfigFiles.Select(file => file.ItemSpec).ToList(),
                Id = "SharpGen-MSBuild"
            };

            config = LoadConfig(config);

            config.GetFilesWithIncludesAndExtensionHeaders(
                out var configsWithHeaders,
                out var configsWithExtensionHeaders
            );

            var cppHeaderGenerator = new CppHeaderGenerator(SharpGenLogger, OutputPath);

            var cppHeaderGenerationResult =
                cppHeaderGenerator.GenerateCppHeaders(config, configsWithHeaders, configsWithExtensionHeaders);

            if (SharpGenLogger.HasErrors)
                return;

            var resolver = new IncludeDirectoryResolver(SharpGenLogger);
            resolver.Configure(config);

            var cppExtensionGenerator = new CppExtensionHeaderGenerator();

            var module = config.CreateSkeletonModule();

            // todo MACROPARSER PARSE MODULE

            cppExtensionGenerator.GenerateExtensionHeaders(
                config, OutputPath, module, configsWithExtensionHeaders, cppHeaderGenerationResult.UpdatedConfigs
            );

            if (SharpGenLogger.HasErrors)
                return;

            // Run the parser
            var parser = new ClangSharpParser(SharpGenLogger, config, resolver)
            {
                OutputPath = OutputPath
            };

            if (SharpGenLogger.HasErrors)
                return;

            // Run the C++ parser
            var group = parser.Run(module);

            if (SharpGenLogger.HasErrors)
                return;

            config.ExpandDynamicVariables(SharpGenLogger, group);

            var docLinker = new DocumentationLinker();
            var typeRegistry = new TypeRegistry(SharpGenLogger, docLinker);
            var namingRules = new NamingRulesManager();

            var globalNamespace = new GlobalNamespaceProvider();

            // Run the main mapping process
            var transformer = new TransformManager(
                globalNamespace,
                namingRules,
                SharpGenLogger,
                typeRegistry,
                docLinker,
                new ConstantManager(namingRules, docLinker)
            );

            var (solution, defines) = transformer.Transform(group, config);

            var consumerConfig = new ConfigFile
            {
                Id = "SharpGen-Consumer-BindMapping",
                IncludeProlog = {cppHeaderGenerationResult.Prologue},
                Extension = new List<ExtensionBaseRule>(defines)
            };

            var (bindings, generatedDefines) = transformer.GenerateTypeBindingsForConsumers();

            consumerConfig.Bindings.AddRange(bindings);
            consumerConfig.Extension.AddRange(generatedDefines);

            consumerConfig.Mappings.AddRange(
                docLinker.GetAllDocLinks().Select(
                    link => new MappingRule
                    {
                        DocItem = link.cppName,
                        MappingNameFinal = link.cSharpName
                    }
                )
            );

            if (SharpGenLogger.HasErrors)
                return;

            var generator = new RoslynGenerator(
                SharpGenLogger,
                globalNamespace,
                docLinker,
                new ExternalDocCommentsReader(new Dictionary<string, XmlDocument>()),
                new GeneratorConfig
                {
                    Platforms = PlatformAbi.Any
                }
            );

            generator.Run(solution, GeneratedCodeFolder);
        }
    }

    internal sealed class ConsoleSharpGenLogger : ILogger
    {
        public void Exit(string reason, int exitCode)
        {
            Console.WriteLine(reason);
            Environment.Exit(exitCode);
        }

        public void Log(LogLevel logLevel, LogLocation logLocation, string context, string code, string message,
                        Exception exception,
                        params object[] parameters)
        {
            Console.WriteLine(message, parameters);
        }
    }
}