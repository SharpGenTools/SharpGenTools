using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using SharpGen.E2ETests.VisualStudio;
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
    public abstract class TestBase : IDisposable
    {
        private readonly ITestOutputHelper outputHelper;
        private readonly DirectoryInfo testDirectory;

        protected TestBase(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
            testDirectory = GenerateTestDirectory();
        }

        public (bool success, string output) RunWithConfig(Config.ConfigFile config, string appType = "true", [CallerMemberName] string configName = "", bool failTestOnError = true)
        {
            config.Id = configName;

            var xUnitLogger = new XUnitLogger(outputHelper, failTestOnError);
            var logger = new Logger(xUnitLogger, null);

            var vcInstallDir = Path.Combine(GetVSInstallPath(), "VC");

            var msvcToolsetVer = File.ReadAllText(vcInstallDir + Path.Combine(@"\Auxiliary\Build", "Microsoft.VCToolsVersion.default.txt")).Trim();

            var codeGenApp = new CodeGenApp(logger)
            {
                GlobalNamespace = new GlobalNamespaceProvider("SharpGen.Runtime"),
                CastXmlExecutablePath = "../../../../CastXML/bin/castxml.exe",
                VcToolsPath = Path.Combine(vcInstallDir, $@"Tools\MSVC\{msvcToolsetVer}\"),
                AppType = appType,
                Config = config,
                OutputDirectory = testDirectory.FullName,
                IntermediateOutputPath = testDirectory.FullName,
                UseRoslynCodeGen = true
            };
            codeGenApp.Init();
            codeGenApp.Run();
            return (xUnitLogger.Success, xUnitLogger.ExitReason);
        }

        private static string GetVSInstallPath()
        {
            var query = new SetupConfiguration();
            var enumInstances = query.EnumInstances();

            int fetched;
            var instances = new ISetupInstance[1];
            do
            {
                enumInstances.Next(1, instances, out fetched);
                var instance2 = (ISetupInstance2)instances[0];
                var state = instance2.GetState();
                if (fetched > 0)
                {
                    if ((state & InstanceState.Registered) == InstanceState.Registered)
                    {
                        if (instance2.GetPackages().Any(pkg => pkg.GetId() == "Microsoft.VisualStudio.Component.VC.Tools.x86.x64"))
                        {
                            return instance2.GetInstallationPath();
                        }
                    }
                }
            }
            while (fetched > 0);

            throw new DirectoryNotFoundException("Unable to find compatible Visual Studio installation path.");
        }

        public static void AssertRanSuccessfully(bool success, string output)
        {
            Assert.True(success, output);
        }

        public void SaveConfigFile(Config.ConfigFile config, string configName)
        {
            config.Id = configName;

            var serializer = new XmlSerializer(typeof(Config.ConfigFile));

            using (var configFile = File.Create(Path.Combine(testDirectory.FullName, configName + "-Mapping.xml")))
            {
                serializer.Serialize(configFile, config);
            }
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

        public static DirectoryInfo GenerateTestDirectory()
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
