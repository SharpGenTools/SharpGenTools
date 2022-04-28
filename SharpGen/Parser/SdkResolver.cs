#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using SharpGen.Config;
using SharpGen.Logging;
using SharpGen.VisualStudioSetup;

namespace SharpGen.Parser;

public sealed class SdkResolver
{
    private static readonly string[] WindowsSdkIncludes = { "shared", "um", "ucrt", "winrt" };
    private static readonly char[] ComponentSeparator = { ';' };

    public SdkResolver(Logger logger)
    {
        Logger = logger;
    }

    private Logger Logger { get; }

    public IEnumerable<IncludeDirRule> ResolveIncludeDirsForSdk(SdkRule sdkRule)
    {
        if (sdkRule.Components is { } components && sdkRule.Name != SdkLib.WindowsSdk)
            Logger.Warning(null, $"Unexpected `{sdkRule._Name_}` SDK components specified: `{components}`.");

        switch (sdkRule.Name)
        {
            case SdkLib.StdLib:
                return ResolveStdLib(sdkRule.Version);
            case SdkLib.WindowsSdk:
                return ResolveWindowsSdk(sdkRule.Version, sdkRule.Components);
            default:
                Logger.Error(LoggingCodes.UnknownSdk, "Unknown SDK specified in an SDK rule.");
                return Enumerable.Empty<IncludeDirRule>();
        }
    }

    private IEnumerable<IncludeDirRule> ResolveStdLib(string? version)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (var vsInstallDir in GetVSInstallPath())
            {
                string actualVersion;
                if (version is not null)
                {
                    actualVersion = version;
                }
                else
                {
                    var defaultVersion = Path.Combine(
                        vsInstallDir, "VC", "Auxiliary", "Build", "Microsoft.VCToolsVersion.default.txt"
                    );

                    if (!File.Exists(defaultVersion))
                        continue;

                    actualVersion = File.ReadAllText(defaultVersion);
                }

                var path = Path.Combine(
                    vsInstallDir, "VC", "Tools", "MSVC", actualVersion.Trim(), "include"
                );

                if (!Directory.Exists(path))
                    continue;

                yield return new IncludeDirRule(path);
                yield break;
            }
        }
        else
        {
            if (version is not null)
            {
                var path = Path.Combine("/usr", "include", "c++", version);
                if (Directory.Exists(path))
                    yield return new IncludeDirRule(path);
            }
            else
            {
                DirectoryInfo cpp = new(Path.Combine("/usr", "include", "c++"));

                if (!cpp.Exists)
                    yield break;

                // TODO: pick latest if not specified
                var path = cpp.EnumerateDirectories().FirstOrDefault()?.FullName;
                if (path is not null)
                    yield return new IncludeDirRule(path);
            }
        }
    }

    private IReadOnlyCollection<string?> GetVSInstallPath()
    {
        var vsPathOverride = Environment.GetEnvironmentVariable("SHARPGEN_VS_OVERRIDE");
        if (!string.IsNullOrEmpty(vsPathOverride))
        {
            return new[] {vsPathOverride};
        }

        List<string?> paths = new();
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
                    paths.Add(instance2.GetInstallationPath());
            } while (fetched > 0);

            static bool Predicate(ISetupPackageReference pkg) =>
                pkg.GetId() == "Microsoft.VisualStudio.Component.VC.Tools.x86.x64";
        }
        catch (Exception e)
        {
            Logger.LogRawMessage(
                LogLevel.Warning, LoggingCodes.VisualStudioDiscoveryError,
                "Visual Studio installation discovery has thrown an exception", e
            );
        }

        if (paths.Count == 0)
            Logger.Fatal("Unable to find a Visual Studio installation that has the Visual C++ Toolchain installed.");

        return paths;
    }

    private IEnumerable<IncludeDirRule> ResolveWindowsSdk(string version, string? components)
    {
        var componentList = components is not null
                                ? components.Split(ComponentSeparator, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(static x => x.Trim())
                                            .ToArray()
                                : WindowsSdkIncludes;

        if (Environment.GetEnvironmentVariable("SHARPGEN_SDK_OVERRIDE") is { Length: > 0 } sdkPathOverride)
        {
            foreach (var include in componentList)
                yield return new IncludeDirRule(Path.Combine(sdkPathOverride, "Include", version, include));
        }

        const string prefix =
            @"=HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows Kits\Installed Roots\KitsRoot10;Include\";

        foreach (var include in componentList)
            yield return new IncludeDirRule(prefix + version + '\\' + include);
    }
}