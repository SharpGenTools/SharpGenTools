using System;
using System.IO;
using SharpGen.Config;
using Xunit.Abstractions;

namespace SharpGen.UnitTests
{
    public abstract class FileSystemTestBase : TestBase, IDisposable
    {
        protected DirectoryInfo TestDirectory { get; }

        protected FileSystemTestBase(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
            TestDirectory = GenerateTestDirectory();
        }

        public IncludeDirRule GetTestFileIncludeRule()
        {
            return new IncludeDirRule
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
