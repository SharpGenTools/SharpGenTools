using SharpGen.Config;
using SharpGen.Logging;
using SharpGen.VisualStudioSetup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGen.Parser
{
    public class SdkResolver
    {
        public SdkResolver(Logger logger)
        {
            Logger = logger;
        }

        public Logger Logger { get; }

        public IEnumerable<IncludeDirRule> ResolveIncludeDirsForSdk(SdkRule sdkRule)
        {
            switch (sdkRule.Name)
            {
                case SdkLib.StdLib:
                    return ResolveStdLib(sdkRule.Version);
                case SdkLib.WindowsSdk:
                    return ResolveWindowsSdk(sdkRule.Version);
                default:
                    Logger.Error("Unknown SDK specified in an SDK rule.");
                    return Enumerable.Empty<IncludeDirRule>();
            }
        }

        private IEnumerable<IncludeDirRule> ResolveStdLib(Version version)
        {
            bool onWindows;
#if NETSTANDARD1_5
            onWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            onWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
#endif
            if (onWindows)
            {
                var vcInstallDir = Path.Combine(GetVSInstallPath(), "VC");

                if (version == null)
                {
                    var msvcToolsetVer
                        = File.ReadAllText(vcInstallDir + Path.Combine(@"\Auxiliary\Build", "Microsoft.VCToolsVersion.default.txt")).Trim();
                    version = Version.Parse(msvcToolsetVer);
                }

                yield return new IncludeDirRule(Path.Combine(vcInstallDir, $@"Tools\MSVC\{version}\include"));
            }
            else
            {
                var versionString = version.ToString();
                if (version.Minor == 0 && version.Revision == 0 && version.Build == 0)
                {
                    versionString = version.Major.ToString();
                }
                yield return new IncludeDirRule(Path.Combine("/usr", "include", "c++", versionString));
            }
        }
        
        private string GetVSInstallPath()
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

            Logger.Fatal("Unable to find compatible Visual Studio installation path.");

            return null;
        }

        private IEnumerable<IncludeDirRule> ResolveWindowsSdk(Version version)
        {
            yield return new IncludeDirRule($@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\{version}\shared");
            yield return new IncludeDirRule($@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\{version}\um");
            yield return new IncludeDirRule($@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\{version}\ucrt");
        }
    }
}
