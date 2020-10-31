using System;
using System.IO;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Parser;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests.Parsing
{
    public class MacroManagerTests : ParsingTestBase
    {
        public MacroManagerTests(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [Fact]
        public void MacroManagerCollectsIncludedHeaders()
        {
            CreateCppFile("included", "");

            var includeRule = GetTestFileIncludeRule();

            var config = new ConfigFile
            {
                Id = nameof(MacroManagerCollectsIncludedHeaders),
                Namespace = nameof(MacroManagerCollectsIncludedHeaders),
                IncludeDirs = { includeRule },
                Includes =
                {
                    CreateCppFile("includer", "#include \"included.h\"")
                }
            };

            var castXml = GetCastXml(ConfigFile.Load(config, Array.Empty<string>(), Logger));

            var macroManager = new MacroManager(castXml);

            macroManager.Parse(Path.Combine(includeRule.Path, "includer.h"), new CppModule());

            Assert.Contains(includeRule.Path + "/included.h",
                macroManager.IncludedFiles);
        }
    }
}
