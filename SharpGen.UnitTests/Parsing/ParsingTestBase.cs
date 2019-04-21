using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Parsing
{
    public abstract class ParsingTestBase : FileSystemTestBase
    {
        private const string CastXmlExecutablePath = "../../../../CastXML/bin/castxml.exe";

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

        protected CastXml GetCastXml(ConfigFile config, string[] additionalArguments = null)
        {
            var resolver = new IncludeDirectoryResolver(Logger);
            resolver.Configure(config);
            return new CastXml(
                Logger,
                resolver,
                CastXmlExecutablePath,
                additionalArguments ?? Array.Empty<string>())
            {
                OutputPath = TestDirectory.FullName
            };
        }

        protected CppModule ParseCpp(ConfigFile config, string[] additionalArguments = null)
        {
            var loaded = ConfigFile.Load(config, new string[0], Logger);

            var (filesWithIncludes, filesWithExtensionHeaders) = loaded.GetFilesWithIncludesAndExtensionHeaders();

            var configsWithIncludes = new HashSet<ConfigFile>();

            foreach (var cfg in loaded.ConfigFilesLoaded)
            {
                if (filesWithIncludes.Contains(cfg.Id))
                {
                    configsWithIncludes.Add(cfg);
                }
            }

            var cppHeaderGenerator = new CppHeaderGenerator(Logger, true, TestDirectory.FullName);

            var (updated, _) = cppHeaderGenerator.GenerateCppHeaders(loaded, configsWithIncludes, filesWithExtensionHeaders);

            var castXml = GetCastXml(loaded);

            var extensionGenerator = new CppExtensionHeaderGenerator(new MacroManager(castXml));

            var skeleton = extensionGenerator.GenerateExtensionHeaders(loaded, TestDirectory.FullName, filesWithExtensionHeaders, updated);

            var parser = new CppParser(Logger, castXml)
            {
                OutputPath = TestDirectory.FullName
            };

            parser.Initialize(loaded);

            return parser.Run(skeleton);
        }
    }
}
