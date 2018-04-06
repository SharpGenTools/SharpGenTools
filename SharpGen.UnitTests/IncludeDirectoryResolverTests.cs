using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SharpGen.Config;
using SharpGen.Logging;
using SharpGen.Parser;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests
{
    public class IncludeDirectoryResolverTests : TestBase
    {
        public IncludeDirectoryResolverTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [SkippableFact]
        public void CanResolveRegistryPath()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Registry paths can only be resolved on Windows");

            var resolver = new IncludeDirectoryResolver(Logger);
            resolver.AddDirectories(new[] { new IncludeDirRule(@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10") });

            Assert.Equal($@"-isystem""C:\Program Files (x86)\Windows Kits\10""", resolver.IncludePaths.First());
        }

        [SkippableFact]
        public void CanResolveRegistryPathWithSubPath()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Registry paths can only be resolved on Windows");

            var resolver = new IncludeDirectoryResolver(Logger);
            resolver.AddDirectories(new[] { new IncludeDirRule(@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;SubFolder") });

            Assert.Equal($@"-isystem""C:\Program Files (x86)\Windows Kits\10\SubFolder""", resolver.IncludePaths.First());
        }

        [SkippableFact]
        public void RegistryPathsFailToResolveOffWindows()
        {
            Skip.If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            var resolver = new IncludeDirectoryResolver(Logger);
            resolver.AddDirectories(new[] { new IncludeDirRule(@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;SubFolder") });

            var resolvedPaths = resolver.IncludePaths;

            AssertLoggingCodeLogged(LoggingCodes.RegistryKeyNotFound);
        }
    }
}
