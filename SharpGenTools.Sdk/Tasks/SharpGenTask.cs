using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using SharpGen;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Generator;
using SharpGen.Logging;
using SharpGen.Model;
using SharpGen.Parser;
using SharpGen.Platform;
using SharpGen.Platform.Documentation;
using SharpGen.Transform;
using SharpGenTools.Sdk.Documentation;
using SharpGenTools.Sdk.Extensibility;
using SharpGenTools.Sdk.Internal;
using SdkResolver = SharpGen.Parser.SdkResolver;

namespace SharpGenTools.Sdk.Tasks
{
    public sealed class SharpGenTask : SharpGenTaskBase
    {
        // Default encoding used by MSBuild ReadLinesFromFile task
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);
        private readonly IocServiceContainer serviceContainer = new();
        private readonly Ioc ioc = new();

        public override bool Execute()
        {
            PrepareExecute();

            serviceContainer.AddService(SharpGenLogger);
            serviceContainer.AddService<IDocumentationLinker, DocumentationLinker>();
            serviceContainer.AddService<GlobalNamespaceProvider>();
            serviceContainer.AddService(new TypeRegistry(ioc));
            ioc.ConfigureServices(serviceContainer);

            ExtensibilityDriver.Instance.LoadExtensions(SharpGenLogger,
                                                        ExtensionAssemblies.Select(x => x.ItemSpec).ToArray());

            var config = new ConfigFile
            {
                Files = ConfigFiles.Select(file => file.ItemSpec).ToList(),
                Id = "SharpGen-MSBuild"
            };

            try
            {
                config = LoadConfig(config);

                return !SharpGenLogger.HasErrors && Execute(config);
            }
            catch (CodeGenFailedException ex)
            {
                SharpGenLogger.Fatal("Internal SharpGen exception", ex);
                return false;
            }
        }

        private bool Execute(ConfigFile config)
        {
            config.GetFilesWithIncludesAndExtensionHeaders(
                out var configsWithHeaders,
                out var configsWithExtensionHeaders
            );

            CppHeaderGenerator cppHeaderGenerator = new(OutputPath, ioc);

            var cppHeaderGenerationResult = cppHeaderGenerator.GenerateCppHeaders(config, configsWithHeaders, configsWithExtensionHeaders);

            if (SharpGenLogger.HasErrors)
                return false;

            IncludeDirectoryResolver resolver = new(ioc);
            resolver.Configure(config);

            CastXmlRunner castXml = new(resolver, CastXmlExecutable.ItemSpec, CastXmlArguments, ioc)
            {
                OutputPath = OutputPath
            };

            var macroManager = new MacroManager(castXml);

            var cppExtensionGenerator = new CppExtensionHeaderGenerator();

            var module = config.CreateSkeletonModule();

            macroManager.Parse(Path.Combine(OutputPath, config.HeaderFileName), module);

            cppExtensionGenerator.GenerateExtensionHeaders(
                config, OutputPath, module, configsWithExtensionHeaders, cppHeaderGenerationResult.UpdatedConfigs
            );

            GenerateInputsCache(
                macroManager.IncludedFiles
                            .Concat(config.ConfigFilesLoaded.Select(x => x.AbsoluteFilePath))
                            .Concat(configsWithExtensionHeaders.Select(x => Path.Combine(OutputPath, x.ExtensionFileName)))
                            .Select(s => Utilities.FixFilePath(s, Utilities.EmptyFilePathBehavior.Ignore))
                            .Where(x => x != null)
                            .Distinct()
            );

            if (SharpGenLogger.HasErrors)
                return false;

            // Run the parser
            var parser = new CppParser(config, ioc)
            {
                OutputPath = OutputPath
            };

            if (SharpGenLogger.HasErrors)
                return false;

            CppModule group;

            using (var xmlReader = castXml.Process(parser.RootConfigHeaderFileName))
            {
                // Run the C++ parser
                group = parser.Run(module, xmlReader);
            }

            if (SharpGenLogger.HasErrors)
                return false;

            config.ExpandDynamicVariables(SharpGenLogger, group);

            var docLinker = ioc.DocumentationLinker;
            var globalNamespace = ioc.GlobalNamespace;
            NamingRulesManager namingRules = new();

            foreach (var nameOverride in GlobalNamespaceOverrides)
            {
                var wellKnownName = nameOverride.ItemSpec;
                var overridenName = nameOverride.GetMetadata("Override");

                if (string.IsNullOrEmpty(overridenName))
                    continue;

                if (Enum.TryParse(wellKnownName, out WellKnownName name))
                {
                    globalNamespace.OverrideName(name, overridenName);
                }
                else
                {
                    SharpGenLogger.Warning(
                        LoggingCodes.InvalidGlobalNamespaceOverride,
                        "Invalid override of \"{0}\": unknown class name, ignoring the override.",
                        wellKnownName
                    );
                }
            }

            // Run the main mapping process
            TransformManager transformer = new(
                namingRules,
                new ConstantManager(namingRules, ioc),
                ioc
            );

            var (solution, defines) = transformer.Transform(group, config);

            var consumerConfig = new ConfigFile
            {
                Id = ConsumerBindMappingConfigId,
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

            GenerateConfigForConsumers(consumerConfig);

            if (SharpGenLogger.HasErrors)
                return false;

            var documentationCacheItemSpec = DocumentationCache.ItemSpec;

            Utilities.RequireAbsolutePath(documentationCacheItemSpec, nameof(DocumentationCache));

            var cache = File.Exists(documentationCacheItemSpec)
                            ? DocItemCache.Read(documentationCacheItemSpec)
                            : new DocItemCache();

            DocumentationLogger docLogger = new(SharpGenLogger) {MaxLevel = LogLevel.Warning};
            var docContext = new Lazy<DocumentationContext>(() => new DocumentationContext(docLogger));
            ExtensibilityDriver.Instance.DocumentModule(SharpGenLogger, cache, solution, docContext).Wait();

            if (docContext.IsValueCreated)
            {
                Regex[] silencePatterns = null;
                var docLogLevelDefault = DocumentationFailuresAsErrors ? LogLevel.Error : LogLevel.Warning;

                foreach (var queryFailure in docContext.Value.Failures)
                {
                    if (silencePatterns == null)
                    {
                        silencePatterns = new Regex[SilenceMissingDocumentationErrorIdentifierPatterns.Length];
                        for (var i = 0; i < silencePatterns.Length; i++)
                            silencePatterns[i] = new Regex(
                                SilenceMissingDocumentationErrorIdentifierPatterns[i].ItemSpec,
                                RegexOptions.CultureInvariant
                            );
                    }

                    if (silencePatterns.Length != 0)
                    {
                        bool SilencePredicate(Regex x) => x.Match(queryFailure.Query).Success;

                        if (silencePatterns.Any(SilencePredicate))
                            continue;
                    }

                    var providerName = queryFailure.FailedProviderName ?? "<null>";

                    var docLogLevel = queryFailure.TreatProviderFailuresAsErrors
                                          ? docLogLevelDefault
                                          : docLogLevelDefault > LogLevel.Warning
                                              ? LogLevel.Warning
                                              : docLogLevelDefault;

                    if (queryFailure.Exceptions == null || queryFailure.Exceptions.Count <= 1)
                    {
                        SharpGenLogger.LogRawMessage(
                            docLogLevel,
                            LoggingCodes.DocumentationProviderInternalError,
                            "Documentation provider [{0}] query for \"{1}\" failed.",
                            queryFailure.Exceptions?.FirstOrDefault(),
                            providerName,
                            queryFailure.Query
                        );
                    }
                    else
                    {
                        var exceptionsCount = queryFailure.Exceptions.Count;
                        for (var index = 0; index < exceptionsCount; index++)
                        {
                            var exception = queryFailure.Exceptions[index];

                            SharpGenLogger.LogRawMessage(
                                docLogLevel,
                                LoggingCodes.DocumentationProviderInternalError,
                                "Documentation provider [{0}] query for \"{1}\" failed ({2}/{3}).",
                                exception,
                                providerName,
                                queryFailure.Query,
                                index + 1,
                                exceptionsCount
                            );
                        }
                    }
                }
            }

            cache.WriteIfDirty(documentationCacheItemSpec);

            if (SharpGenLogger.HasErrors)
                return false;

            var documentationFiles = new Dictionary<string, XmlDocument>();

            foreach (var file in ExternalDocumentation)
            {
                using var stream = File.OpenRead(file.ItemSpec);

                var xml = new XmlDocument();
                xml.Load(stream);
                documentationFiles.Add(file.ItemSpec, xml);
            }

            PlatformDetectionType platformMask = 0;

            foreach (var platform in Platforms)
            {
                if (!Enum.TryParse<PlatformDetectionType>(platform.ItemSpec, out var parsedPlatform))
                {
                    SharpGenLogger.Warning(
                        LoggingCodes.InvalidPlatformDetectionType,
                        "The platform type {0} is an unknown platform to SharpGenTools. Falling back to Any platform detection.",
                        platform
                    );
                    platformMask = PlatformDetectionType.Any;
                }
                else
                {
                    platformMask |= parsedPlatform;
                }
            }

            if (platformMask == 0)
                platformMask = PlatformDetectionType.Any;

            if (SharpGenLogger.HasErrors)
                return false;

            serviceContainer.AddService(new ExternalDocCommentsReader(documentationFiles));

            serviceContainer.AddService<IGeneratorRegistry>(
                new DefaultGenerators(
                    new GeneratorConfig
                    {
                        Platforms = platformMask
                    },
                    ioc
                )
            );

            RoslynGenerator generator = new();
            generator.Run(solution, GeneratedCodeFolder, ioc);

            return !SharpGenLogger.HasErrors;
        }

        private void GenerateConfigForConsumers(ConfigFile consumerConfig)
        {
            if (ConsumerBindMappingConfig == null) return;

            using var consumerBindMapping = File.Create(ConsumerBindMappingConfig.ItemSpec);

            consumerConfig.Write(consumerBindMapping);
        }

        private void GenerateInputsCache(IEnumerable<string> paths)
        {
            using var inputCacheStream = new StreamWriter(InputsCache.ItemSpec, false, DefaultEncoding);

            foreach (var path in paths) inputCacheStream.WriteLine(path);
        }

        private ConfigFile LoadConfig(ConfigFile config)
        {
            config = ConfigFile.Load(config, Macros, SharpGenLogger);

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
    }
}