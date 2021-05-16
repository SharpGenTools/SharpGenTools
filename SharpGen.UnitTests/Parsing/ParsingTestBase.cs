using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Parser;
using SharpGen.Platform;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Parsing
{
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
            var loaded = ConfigFile.Load(config, new string[0], Logger);

            loaded.GetFilesWithIncludesAndExtensionHeaders(out var configsWithIncludes,
                                                           out var configsWithExtensionHeaders);

            CppHeaderGenerator cppHeaderGenerator = new(TestDirectory.FullName, Ioc);

            var updated = cppHeaderGenerator
                         .GenerateCppHeaders(loaded, configsWithIncludes, configsWithExtensionHeaders)
                         .UpdatedConfigs;

            var castXml = GetCastXml(loaded);

            var macro = new MacroManager(castXml);
            var extensionGenerator = new CppExtensionHeaderGenerator();

            var skeleton = loaded.CreateSkeletonModule();

            macro.Parse(Path.Combine(TestDirectory.FullName, loaded.HeaderFileName), skeleton);

            extensionGenerator.GenerateExtensionHeaders(
                loaded, TestDirectory.FullName, skeleton, configsWithExtensionHeaders, updated
            );

            CppParser parser = new(loaded, Ioc)
            {
                OutputPath = TestDirectory.FullName
            };

            using var xmlReader = castXml.Process(parser.RootConfigHeaderFileName);

            return parser.Run(skeleton, xmlReader);
        }
    }
}
