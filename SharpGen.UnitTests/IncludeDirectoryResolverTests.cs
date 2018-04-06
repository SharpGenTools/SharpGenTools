using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SharpGen.Config;
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
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            var resolver = new IncludeDirectoryResolver(Logger);
            resolver.AddDirectories(new[] { new IncludeDirRule(@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10") });

            if (IntPtr.Size == 8)
            {
                Assert.Equal($@"-isystem""C:\Program Files (x86)\Windows Kits\10""", resolver.IncludePaths.First());
            }
            else
            {
                Assert.Equal(@"-isystem""C:\Program Files\Windows Kits\10""", resolver.IncludePaths.First());
            }
        }
    }
}
