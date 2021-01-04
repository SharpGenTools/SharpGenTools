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

        public IncludeRule CreateCppFile(string cppFileName, string cppFile, [CallerMemberName] string testName = "")
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

        public IncludeRule CreateCppFile(string cppFileName, string cppFile, List<string> attaches, [CallerMemberName] string testName = "")
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
            var resolver = new IncludeDirectoryResolver(Logger);
            resolver.Configure(config);

            var rootPath = new DirectoryInfo(Environment.CurrentDirectory);
            while (rootPath != null && !File.Exists(Path.Combine(rootPath.FullName, CastXmlDirectoryPath)))
                rootPath = rootPath.Parent;
            
            if (rootPath == null)
                throw new InvalidOperationException("CastXML not found");

            return new CastXmlRunner(Logger, resolver,
                                     Path.Combine(rootPath.FullName, CastXmlDirectoryPath),
                                     additionalArguments ?? Array.Empty<string>())
            {
                OutputPath = TestDirectory.FullName
            };
        }

        protected CppModule ParseCpp(ConfigFile config, string[] additionalArguments = null)
        {
            var loaded = ConfigFile.Load(config, new string[0], Logger);

            loaded.GetFilesWithIncludesAndExtensionHeaders(out var configsWithIncludes,
                                                           out var configsWithExtensionHeaders);

            var cppHeaderGenerator = new CppHeaderGenerator(Logger, TestDirectory.FullName);

            var updated = cppHeaderGenerator
                         .GenerateCppHeaders(loaded, configsWithIncludes, configsWithExtensionHeaders)
                         .UpdatedConfigs;

            var castXml = GetCastXml(loaded);

            var extensionGenerator = new CppExtensionHeaderGenerator(new MacroManager(castXml));

            var skeleton = extensionGenerator.GenerateExtensionHeaders(
                loaded, TestDirectory.FullName, configsWithExtensionHeaders, updated
            );

            var parser = new CppParser(Logger, loaded)
            {
                OutputPath = TestDirectory.FullName
            };

            using var xmlReader = castXml.Process(parser.RootConfigHeaderFileName);

            return parser.Run(skeleton, xmlReader);
        }
    }
}
