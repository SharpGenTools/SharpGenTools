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
    public abstract class E2ETestBase : FileSystemTestBase
    {
        private readonly ITestOutputHelper outputHelper;

        protected E2ETestBase(ITestOutputHelper outputHelper)
            :base(outputHelper)
        {
            this.outputHelper = outputHelper;
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
                OutputDirectory = TestDirectory.FullName,
                IntermediateOutputPath = TestDirectory.FullName,
            };
            codeGenApp.Init();
            codeGenApp.Run();
            return (xUnitLogger.Success, xUnitLogger.ExitReason);
        }
        
        public static void AssertRanSuccessfully(bool success, string output)
        {
            Assert.True(success, output);
        }

        public Compilation GetCompilationForGeneratedCode([CallerMemberName] string assemblyName = "")
        {
            return CSharpCompilation.Create(assemblyName, options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location))
                .AddSyntaxTrees(GetSyntaxTrees(assemblyName));
        }

        private IEnumerable<SyntaxTree> GetSyntaxTrees(string assemblyName)
        {
            foreach (var child in TestDirectory.CreateSubdirectory("Generated").EnumerateFiles("*.cs", SearchOption.AllDirectories))
            {
                using (var file = child.OpenRead())
                {
                    yield return CSharpSyntaxTree.ParseText(SourceText.From(file));
                }
            }
        }
    }
}
