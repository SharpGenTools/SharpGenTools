using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SharpGen.E2ETests
{
    public class TestBase
    {
        private const string DefaultSharpGenArguments = @"--castxml castxml/bin/castxml.exe --vctools ""C:\Program Files(x86)\Microsoft Visual Studio\2017\Community\VC\Tools\MSVC\14.10.25017\";

        public static (int exitCode, string output) RunWithConfig(DirectoryInfo testDirectory, Config.ConfigFile config, string appType = "true", [CallerMemberName] string configName = "")
        {
            SaveConfigFile(testDirectory, config, configName);

            ExtractCastXmlToTestDirectory(testDirectory);

            var sharpGenProcessInfo = new ProcessStartInfo
            {
                WorkingDirectory = testDirectory.FullName,
                FileName = "SharpGen.exe",
                Arguments = $"{configName}-Mapping.xml --apptype {appType} " + DefaultSharpGenArguments,
                EnvironmentVariables =
                {
                    { "SharpDXBuildNoWindow", "" },
                },
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            var sharpGenProcess = Process.Start(sharpGenProcessInfo);
            sharpGenProcess.WaitForExit(30000);
            var output = sharpGenProcess.StandardOutput.ReadToEnd();
            return (sharpGenProcess.ExitCode, output);
        }

        public static void AssertRanSuccessfully(int exitCode, string output)
        {
            if(exitCode != 0)
            {
                throw new Xunit.Sdk.AssertActualExpectedException(0, exitCode, output);
            }
        }

        private static void SaveConfigFile(DirectoryInfo testDirectory, Config.ConfigFile config, string configName)
        {
            config.Id = configName;

            var serializer = new XmlSerializer(typeof(Config.ConfigFile));

            using (var configFile = File.Create(Path.Combine(testDirectory.FullName, configName + "-Mapping.xml")))
            {
                serializer.Serialize(configFile, config);
            }
        }

        public static Config.IncludeRule CreateCppFile(DirectoryInfo testDirectory, string cppFileName, string cppFile, [CallerMemberName] string testName = "")
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

        public static Config.IncludeDirRule GetTestFileIncludeRule([CallerMemberName] string testName = "")
        {
            return new Config.IncludeDirRule
            {
                Path = "includes"
            };
        }

        public static Compilation GetCompilationForGeneratedCode(DirectoryInfo testDirectory, [CallerMemberName] string assemblyName = "")
        {
            return CSharpCompilation.Create(assemblyName)
                .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
                .AddSyntaxTrees(GetSyntaxTrees(testDirectory, assemblyName));
        }

        private static IEnumerable<SyntaxTree> GetSyntaxTrees(DirectoryInfo testDirectory, string assemblyName)
        {
            foreach (var child in testDirectory.CreateSubdirectory(assemblyName).EnumerateFiles("*.cs", SearchOption.AllDirectories))
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

        private static void ExtractCastXmlToTestDirectory(DirectoryInfo testDirectory)
        {
            using (var castXml = typeof(TestBase).Assembly.GetManifestResourceStream("SharpGen.E2ETests.CastXML.zip"))
            using (var archive = new ZipArchive(castXml))
            {
                archive.ExtractToDirectory(testDirectory.FullName);
            }
        }
    }
}
