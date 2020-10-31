using SharpGen.Config;
using SharpGen.Logging;
using SharpGen.VisualStudioSetup;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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
                    Logger.Message($"Resolving Windows SDK: version {sdkRule.Version}");
                    return ResolveWindowsSdk(sdkRule.Version);
                default:
                    Logger.Error(LoggingCodes.UnknownSdk, "Unknown SDK specified in an SDK rule.");
                    return Enumerable.Empty<IncludeDirRule>();
            }
        }

        private IEnumerable<IncludeDirRule> ResolveStdLib(string version)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var vsInstallDir = GetVSInstallPath();

                version ??= File.ReadAllText(
                    Path.Combine(vsInstallDir, "VC", "Auxiliary", "Build", "Microsoft.VCToolsVersion.default.txt")
                );

                yield return new IncludeDirRule(
                    Path.Combine(vsInstallDir, "VC", "Tools", "MSVC", version.Trim(), "include")
                );
            }
            else
            {
                yield return new IncludeDirRule(Path.Combine("/usr", "include", "c++", version));
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
                if (fetched > 0)
                {
                    var instance2 = (ISetupInstance2)instances[0];
                    var state = instance2.GetState();
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

            Logger.Fatal("Unable to find a Visual Studio installation that has the Visual C++ Toolchain installed.");

            return null;
        }

        private IEnumerable<IncludeDirRule> ResolveWindowsSdk(string version)
        {
            yield return new IncludeDirRule($@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\{version}\shared");
            yield return new IncludeDirRule($@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\{version}\um");
            yield return new IncludeDirRule($@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\{version}\ucrt");
            yield return new IncludeDirRule($@"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\{version}\winrt");
        }
    }
}
