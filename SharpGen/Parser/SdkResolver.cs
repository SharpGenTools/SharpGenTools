using System;
using SharpGen.Config;
using SharpGen.Logging;
using SharpGen.VisualStudioSetup;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace SharpGen.Parser
{
    public sealed class SdkResolver
    {
        private static readonly string[] WindowsSdkIncludes = {"shared", "um", "ucrt", "winrt"}; 

        public SdkResolver(Logger logger)
        {
            Logger = logger;
        }

        private Logger Logger { get; }

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
            var vsPathOverride = Environment.GetEnvironmentVariable("SHARPGEN_VS_OVERRIDE");
            if (!string.IsNullOrEmpty(vsPathOverride))
                return vsPathOverride;

            try
            {
                var query = new SetupConfiguration();
                var enumInstances = query.EnumInstances();

                int fetched;
                var instances = new ISetupInstance[1];
                do
                {
                    enumInstances.Next(1, instances, out fetched);
                    if (fetched <= 0)
                        continue;

                    var instance2 = (ISetupInstance2) instances[0];
                    var state = instance2.GetState();
                    if ((state & InstanceState.Registered) != InstanceState.Registered)
                        continue;

                    if (instance2.GetPackages().Any(Predicate))
                        return instance2.GetInstallationPath();

                } while (fetched > 0);

                static bool Predicate(ISetupPackageReference pkg) => pkg.GetId() == "Microsoft.VisualStudio.Component.VC.Tools.x86.x64";
            }
            catch (Exception e)
            {
                Logger.LogRawMessage(
                    LogLevel.Warning, LoggingCodes.VisualStudioDiscoveryError,
                    "Visual Studio installation discovery has thrown an exception", e
                );
            }

            Logger.Fatal("Unable to find a Visual Studio installation that has the Visual C++ Toolchain installed.");

            return null;
        }

        private IEnumerable<IncludeDirRule> ResolveWindowsSdk(string version)
        {
            var sdkPathOverride = Environment.GetEnvironmentVariable("SHARPGEN_SDK_OVERRIDE");

            if (!string.IsNullOrEmpty(sdkPathOverride))
            {
                foreach (var include in WindowsSdkIncludes)
                    yield return new IncludeDirRule(Path.Combine(sdkPathOverride, "Include", version, include));

                yield break;
            }

            const string prefix = @"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\";

            foreach (var include in WindowsSdkIncludes)
                yield return new IncludeDirRule(prefix + version + '\\' + include);
        }
    }
}
