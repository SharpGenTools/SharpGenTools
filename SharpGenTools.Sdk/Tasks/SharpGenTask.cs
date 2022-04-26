#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
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
using Logger = SharpGen.Logging.Logger;
using SdkResolver = SharpGen.Parser.SdkResolver;

namespace SharpGenTools.Sdk.Tasks;

public sealed partial class SharpGenTask : Task, ICancelableTask
{
    // Default encoding used by MSBuild ReadLinesFromFile task
    private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);
    private readonly IocServiceContainer serviceContainer = new();
    private readonly Ioc ioc = new();
    private string? profilePath;
    private volatile bool isCancellationRequested;
    private Mutex? workerLock;
    private bool workerLockAcquired;

    // ReSharper disable MemberCanBePrivate.Global, UnusedAutoPropertyAccessor.Global
    [Required] public string[]? CastXmlArguments { get; set; }
    [Required] public string? CastXmlExecutable { get; set; }
    [Required] public string[]? ConfigFiles { get; set; }
    [Required] public string? ConsumerBindMappingConfigId { get; set; }
    [Required] public bool DebugWaitForDebuggerAttach { get; set; }
    [Required] public bool DocumentationFailuresAsErrors { get; set; }
    [Required] public string[]? ExtensionAssemblies { get; set; }
    [Required] public string[]? ExternalDocumentation { get; set; }
    [Required] public ITaskItem[]? GlobalNamespaceOverrides { get; set; }
    [Required] public string[]? Macros { get; set; }
    [Required] public string? IntermediateOutputDirectory { get; set; }
    public string? PlatformName { get; set; }
    [Required] public string[]? Platforms { get; set; }

    [Output]
    public string ProfilePath
    {
        get => profilePath ?? throw new InvalidOperationException("Profile not set");
        set
        {
            if (profilePath is not null)
                throw new InvalidOperationException("Profile cannot be set twice");
            profilePath = Utilities.EnsureTrailingSlash(value ?? throw new InvalidOperationException("Profile cannot be null"));
        }
    }

    public string? RuntimeIdentifier { get; set; }
    [Required] public string[]? SilenceMissingDocumentationErrorIdentifierPatterns { get; set; }
    // ReSharper restore UnusedAutoPropertyAccessor.Global, MemberCanBePrivate.Global

    private string GeneratedCodeFile => GetProfileChild("SharpGen.Bindings.g.cs");
    private string InputsCache => GetProfileChild("InputsCache.txt");
    private string PropertyCache => GetProfileChild("PropertyCache.txt");
    private string DocumentationCache => GetProfileChild("DocumentationCache.json");
    private string DirtyMarkerFile => GetProfileChild("dirty");
    private string PackagePropsFile => GetProfileChild("Package.props");

    private string ConsumerBindMappingConfig => GetProfileChild(
        (ConsumerBindMappingConfigId ?? throw new InvalidOperationException(nameof(ConsumerBindMappingConfigId))) +
        ".BindMapping.xml"
    );

    private string GetProfileChild(string childName) => Path.Combine(ProfilePath, childName);

    private Logger SharpGenLogger { get; set; }

    [Conditional("DEBUG")]
    private void WaitForDebuggerAttach()
    {
        if (!Debugger.IsAttached)
        {
            SharpGenLogger.Warning(null, $"{GetType().Name} is waiting for attach: {Process.GetCurrentProcess().Id}");
            Thread.Yield();
        }

        while (!Debugger.IsAttached && !isCancellationRequested)
            Thread.Sleep(TimeSpan.FromSeconds(1));
    }

    public void Cancel() => isCancellationRequested = true;

    public override bool Execute()
    {
        BindingRedirectResolution.Enable();

        SharpGenLogger = new Logger(new MSBuildSharpGenLogger(Log));

#if DEBUG
        if (DebugWaitForDebuggerAttach)
            WaitForDebuggerAttach();
#endif

        var success = false;

        try
        {
            if (!GeneratePropertyCache())
                return success = true;

            if (AbortExecution)
                return success = false;

            serviceContainer.AddService(SharpGenLogger);
            serviceContainer.AddService<IDocumentationLinker, DocumentationLinker>();
            serviceContainer.AddService<GlobalNamespaceProvider>();
            serviceContainer.AddService(new TypeRegistry(ioc));
            ioc.ConfigureServices(serviceContainer);

            ExtensibilityDriver.Instance.LoadExtensions(SharpGenLogger, ExtensionAssemblies.ToArray());

            ConfigFile config = new()
            {
                Files = ConfigFiles.ToList(),
                Id = "SharpGen-MSBuild"
            };

            LoadConfig(config);

            return success = !AbortExecution && Execute(config);
        }
        catch (CodeGenFailedException ex)
        {
            SharpGenLogger.Fatal("Internal SharpGen exception", ex);
            return success = false;
        }
        finally
        {
            Debug.Assert(workerLock != null, nameof(workerLock) + " != null");

            try
            {
                GeneratePackagesProps();
            }
            catch (Exception e)
            {
                SharpGenLogger.Fatal("Internal SharpGen exception while generating NuGet package properties file", e);
            }

            if (success && !AbortExecution && File.Exists(DirtyMarkerFile))
                File.Delete(DirtyMarkerFile);

            if (workerLockAcquired)
                workerLock.ReleaseMutex();

            workerLock.Dispose();
            workerLock = null;
        }
    }

    private bool AbortExecution => SharpGenLogger.HasErrors || isCancellationRequested;

    private bool Execute(ConfigFile config)
    {
        config.GetFilesWithIncludesAndExtensionHeaders(
            out var configsWithHeaders,
            out var configsWithExtensionHeaders
        );

        CppHeaderGenerator cppHeaderGenerator = new(ProfilePath, ioc);

        var cppHeaderGenerationResult = cppHeaderGenerator.GenerateCppHeaders(config, configsWithHeaders, configsWithExtensionHeaders);

        if (AbortExecution)
            return false;

        IncludeDirectoryResolver resolver = new(ioc);
        resolver.Configure(config);

        CastXmlRunner castXml = new(resolver, CastXmlExecutable, CastXmlArguments, ioc)
        {
            OutputPath = ProfilePath
        };

        var macroManager = new MacroManager(castXml);

        var cppExtensionGenerator = new CppExtensionHeaderGenerator();

        var module = config.CreateSkeletonModule();

        macroManager.Parse(Path.Combine(ProfilePath, config.HeaderFileName), module);

        cppExtensionGenerator.GenerateExtensionHeaders(
            config, ProfilePath, module, configsWithExtensionHeaders, cppHeaderGenerationResult.UpdatedConfigs
        );

        GenerateInputsCache(
            macroManager.IncludedFiles
                        .Concat(config.ConfigFilesLoaded.Select(x => x.AbsoluteFilePath))
                        .Concat(configsWithExtensionHeaders.Select(x => Path.Combine(ProfilePath, x.ExtensionFileName)))
                        .Select(s => Utilities.FixFilePath(s, Utilities.EmptyFilePathBehavior.Ignore))
                        .Where(x => x != null)
                        .Distinct()
        );

        if (AbortExecution)
            return false;

        // Run the parser
        var parser = new CppParser(config, ioc)
        {
            OutputPath = ProfilePath
        };

        if (AbortExecution)
            return false;

        CppModule group;

        using (var xmlReader = castXml.Process(parser.RootConfigHeaderFileName))
        {
            // Run the C++ parser
            group = parser.Run(module, xmlReader);
        }

        if (AbortExecution)
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

        serviceContainer.AddService(
            new GeneratorConfig
            {
                Platforms = ConfigPlatforms
            }
        );

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

        if (AbortExecution)
            return false;

        var documentationCacheItemSpec = DocumentationCache;

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
                            SilenceMissingDocumentationErrorIdentifierPatterns[i],
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

                switch (queryFailure.Exceptions)
                {
                    case { Count: > 1 } exceptions:
                    {
                        var exceptionsCount = exceptions.Count;
                        for (var index = 0; index < exceptionsCount; index++)
                        {
                            var exception = exceptions[index];

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

                        break;
                    }
                    case { Count: 1 } exceptions:
                        SharpGenLogger.LogRawMessage(
                            docLogLevel,
                            LoggingCodes.DocumentationProviderInternalError,
                            "Documentation provider [{0}] query for \"{1}\" failed.",
                            exceptions[0],
                            providerName,
                            queryFailure.Query
                        );
                        break;
                    default:
                        SharpGenLogger.LogRawMessage(
                            docLogLevel,
                            LoggingCodes.DocumentationProviderInternalError,
                            "Documentation provider [{0}] query for \"{1}\" failed.",
                            null,
                            providerName,
                            queryFailure.Query
                        );
                        break;
                }
            }
        }

        cache.WriteIfDirty(documentationCacheItemSpec);

        if (AbortExecution)
            return false;

        var documentationFiles = new Dictionary<string, XmlDocument>();

        foreach (var file in ExternalDocumentation)
        {
            using var stream = File.OpenRead(file);

            var xml = new XmlDocument();
            xml.Load(stream);
            documentationFiles.Add(file, xml);
        }

        if (AbortExecution)
            return false;

        serviceContainer.AddService(new ExternalDocCommentsReader(documentationFiles));
        serviceContainer.AddService<IGeneratorRegistry>(new DefaultGenerators(ioc));

        RoslynGenerator generator = new();

        var generatedCode = generator.Run(solution, ioc);
        File.WriteAllText(GeneratedCodeFile, generatedCode);

        return !SharpGenLogger.HasErrors;
    }

    private PlatformDetectionType ConfigPlatforms
    {
        get
        {
            PlatformDetectionType platformMask = 0;

            foreach (var platform in Platforms)
            {
                if (!Enum.TryParse<PlatformDetectionType>(platform, out var parsedPlatform))
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

            return platformMask == 0 ? PlatformDetectionType.Any : platformMask;
        }
    }

    private void GenerateConfigForConsumers(ConfigFile consumerConfig)
    {
        using var consumerBindMapping = File.Create(ConsumerBindMappingConfig);

        consumerConfig.Write(consumerBindMapping);
    }

    private void GenerateInputsCache(IEnumerable<string> paths)
    {
        using var inputCacheStream = new StreamWriter(InputsCache, false, DefaultEncoding);

        foreach (var path in paths) inputCacheStream.WriteLine(path);
    }

    private void LoadConfig(ConfigFile config)
    {
        config.Load(null, Macros, SharpGenLogger);

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
    }

    private void GeneratePackagesProps()
    {
        using MemoryStream stream = new();
        {
            using var writer = new StreamWriter(stream, DefaultEncoding, -1, true);


            writer.WriteLine(@"<Project>");
            writer.WriteLine(@"  <ItemGroup>");
            writer.WriteLine(
                $@"    <SharpGenConsumerMapping Include=""$([MSBuild]::NormalizePath('$(MSBuildThisFileDirectory)', '..', 'build', '{ConsumerBindMappingConfigId}.BindMapping.xml'))""/>"
            );
            writer.WriteLine(@"  </ItemGroup>");
            writer.WriteLine(@"</Project>");
        }

        bool writeNeeded;

        if (File.Exists(PackagePropsFile))
        {
            ReadOnlySpan<byte> cachedHash = File.ReadAllBytes(PackagePropsFile);

            stream.Position = 0;
            var success = stream.TryGetBuffer(out var buffer);
            Debug.Assert(success);

            if (buffer.AsSpan().SequenceEqual(cachedHash))
            {
                SharpGenLogger.Message("NuGet package properties file is already up-to-date.");
                writeNeeded = false;
            }
            else
            {
                SharpGenLogger.Message("NuGet package properties file is out-of-date.");
                writeNeeded = true;
            }
        }
        else
        {
            SharpGenLogger.Message("NuGet package properties file doesn't exist.");
            writeNeeded = true;
        }

        if (writeNeeded)
        {
            stream.Position = 0;
            using var file = File.Open(PackagePropsFile, FileMode.Create, FileAccess.Write);
            stream.CopyTo(file);
        }
    }
}