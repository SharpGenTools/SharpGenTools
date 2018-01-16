using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SharpGen.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests
{
    public abstract class E2ETestBase : IDisposable
    {
        private readonly ITestOutputHelper outputHelper;
        private readonly DirectoryInfo testDirectory;

        protected E2ETestBase(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
            testDirectory = GenerateTestDirectory();
        }

        public (bool success, string output) RunWithConfig(Config.ConfigFile config, [CallerMemberName] string configName = "")
        {
            config.Id = configName;

            var xUnitLogger = new XUnitLogger(outputHelper);
            var logger = new Logger(xUnitLogger, null);

            var codeGenApp = new CodeGenApp(logger)
            {
                GlobalNamespace = new GlobalNamespaceProvider("SharpGen.Runtime"),
                CastXmlExecutablePath = "../../../../CastXML/bin/castxml.exe",
                Config = config,
                OutputDirectory = testDirectory.FullName,
                IntermediateOutputPath = testDirectory.FullName,
            };
            codeGenApp.Init();
            codeGenApp.Run();
            return (xUnitLogger.Success, xUnitLogger.ExitReason);
        }
        
        public static void AssertRanSuccessfully(bool success, string output)
        {
            Assert.True(success, output);
        }

        public Config.IncludeRule CreateCppFile(string cppFileName, string cppFile, [CallerMemberName] string testName = "")
        {
            var includesDir = testDirectory.CreateSubdirectory("includes");
            File.WriteAllText(Path.Combine(includesDir.FullName, cppFileName + ".h"), cppFile);
            return new Config.IncludeRule
            {
                Attach = true,
                File = cppFileName + ".h",
                Namespace = testName,
            };
        }

        public Config.IncludeDirRule GetTestFileIncludeRule()
        {
            return new Config.IncludeDirRule
            {
                Path = $@"{testDirectory.FullName}\includes"
            };
        }

        public Compilation GetCompilationForGeneratedCode([CallerMemberName] string assemblyName = "")
        {
            return CSharpCompilation.Create(assemblyName, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location))
                .AddSyntaxTrees(GetSyntaxTrees(assemblyName));
        }

        private IEnumerable<SyntaxTree> GetSyntaxTrees(string assemblyName)
        {
            foreach (var child in testDirectory.CreateSubdirectory("Generated").EnumerateFiles("*.cs", SearchOption.AllDirectories))
            {
                using (var file = child.OpenRead())
                {
                    yield return CSharpSyntaxTree.ParseText(SourceText.From(file));
                }
            }
        }

        private static DirectoryInfo GenerateTestDirectory()
        {
            var tempFolder = Path.GetTempPath();
            var testFolderName = Path.GetRandomFileName();
            var testDirectoryInfo = Directory.CreateDirectory(Path.Combine(tempFolder, testFolderName));
            return testDirectoryInfo;
        }

        public void Dispose()
        {
            testDirectory.Delete(true);
        }
    }
}
