using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Parser;
using SharpGen.Platform;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Parsing;

public abstract class ParsingTestBase : FileSystemTestBase
{
    private static readonly string CastXmlDirectoryPath = Path.Combine("CastXML", "bin", "castxml.exe");

    protected ParsingTestBase(ITestOutputHelper outputHelper) : base(outputHelper)
    {
    }

    protected IncludeRule CreateCppFile(string cppFileName, string cppFile, [CallerMemberName] string testName = "")
    {
        var includesDir = TestDirectory.CreateSubdirectory("includes");
        File.WriteAllText(Path.Combine(includesDir.FullName, cppFileName + ".h"), cppFile);
        return new IncludeRule
        {
            Attach = true,
            File = cppFileName + ".h",
            Namespace = testName,
        };
    }

    protected IncludeRule CreateCppFile(string cppFileName, string cppFile, List<string> attaches, [CallerMemberName] string testName = "")
    {
        var includesDir = TestDirectory.CreateSubdirectory("includes");
        File.WriteAllText(Path.Combine(includesDir.FullName, cppFileName + ".h"), cppFile);
        return new IncludeRule
        {
            AttachTypes = attaches,
            File = cppFileName + ".h",
            Namespace = testName,
        };
    }

    protected CastXmlRunner GetCastXml(ConfigFile config, string[] additionalArguments = null)
    {
        IncludeDirectoryResolver resolver = new(Ioc);
        resolver.Configure(config);

        var rootPath = new DirectoryInfo(Environment.CurrentDirectory);
        while (rootPath != null && !File.Exists(Path.Combine(rootPath.FullName, CastXmlDirectoryPath)))
            rootPath = rootPath.Parent;

        if (rootPath == null)
            throw new InvalidOperationException("CastXML not found");

        return new CastXmlRunner(resolver, Path.Combine(rootPath.FullName, CastXmlDirectoryPath),
                                 additionalArguments ?? Array.Empty<string>(), Ioc)
        {
            OutputPath = TestDirectory.FullName
        };
    }

    protected CppModule ParseCpp(ConfigFile config)
    {
        config.Load(null, Array.Empty<string>(), Logger);

        config.GetFilesWithIncludesAndExtensionHeaders(out var configsWithIncludes,
                                                       out var configsWithExtensionHeaders);

        CppHeaderGenerator cppHeaderGenerator = new(TestDirectory.FullName, Ioc);

        var updated = cppHeaderGenerator
                     .GenerateCppHeaders(config, configsWithIncludes, configsWithExtensionHeaders)
                     .UpdatedConfigs;

        var castXml = GetCastXml(config);

        var macro = new MacroManager(castXml);
        var extensionGenerator = new CppExtensionHeaderGenerator();

        var skeleton = config.CreateSkeletonModule();

        macro.Parse(Path.Combine(TestDirectory.FullName, config.HeaderFileName), skeleton);

        extensionGenerator.GenerateExtensionHeaders(
            config, TestDirectory.FullName, skeleton, configsWithExtensionHeaders, updated
        );

        CppParser parser = new(config, Ioc)
        {
            OutputPath = TestDirectory.FullName
        };

        using var xmlReader = castXml.Process(parser.RootConfigHeaderFileName);

        return parser.Run(skeleton, xmlReader);
    }
}