using SharpGen.Config;
using SharpGen.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace SharpGen.UnitTests.Parsing
{
    public class MacroManagerTests : ParsingTestBase
    {
        public MacroManagerTests(Xunit.Abstractions.ITestOutputHelper outputHelper)
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

            macroManager.Parse(Path.Combine(includeRule.Path, "includer.h"), new CppModel.CppModule());

            Assert.Contains(includeRule.Path + "/included.h",
                macroManager.IncludedFiles);
        }
    }
}
