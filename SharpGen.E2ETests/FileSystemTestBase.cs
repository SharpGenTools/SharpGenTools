using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Xunit.Abstractions;

namespace SharpGen.E2ETests
{
    public abstract class FileSystemTestBase : TestBase, IDisposable
    {
        protected DirectoryInfo TestDirectory { get; }

        protected FileSystemTestBase(ITestOutputHelper outputHelper)
            :base(outputHelper)
        {
            TestDirectory = GenerateTestDirectory();
        }

        public Config.IncludeRule CreateCppFile(string cppFileName, string cppFile, [CallerMemberName] string testName = "")
        {
            var includesDir = TestDirectory.CreateSubdirectory("includes");
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
                Path = $@"{TestDirectory.FullName}\includes"
            };
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
            TestDirectory.Delete(true);
        }
    }
}
