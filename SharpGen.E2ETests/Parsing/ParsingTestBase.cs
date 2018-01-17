using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Parser;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace SharpGen.E2ETests.Parsing
{
    public abstract class ParsingTestBase : FileSystemTestBase
    {
        private const string CastXmlExecutablePath = "../../../../CastXML/bin/castxml.exe";

        protected ParsingTestBase(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        protected CppModule ParseCpp(ConfigFile config)
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

            var castXml = new CastXml(Logger, CastXmlExecutablePath)
            {
                OutputPath = TestDirectory.FullName
            };
            castXml.Configure(loaded);

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
