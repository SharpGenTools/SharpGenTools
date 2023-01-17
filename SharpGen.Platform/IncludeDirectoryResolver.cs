using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using SharpGen.Config;
using SharpGen.Logging;
using SharpGen.Parser;

namespace SharpGen.Platform;

public sealed class IncludeDirectoryResolver : IIncludeDirectoryResolver
{
    private Logger Logger => ioc.Logger;
    private readonly List<IncludeDirRule> includeDirectoryList = new();
    private readonly Ioc ioc;

    public IncludeDirectoryResolver(Ioc ioc)
    {
        this.ioc = ioc ?? throw new ArgumentNullException(nameof(ioc));
    }

    public void Configure(ConfigFile config)
    {
        AddDirectories(config.ConfigFilesLoaded.SelectMany(file => file.IncludeDirs));
    }

    public void AddDirectories(IEnumerable<IncludeDirRule> directories)
    {
        includeDirectoryList.AddRange(directories);
    }

    public void AddDirectories(params IncludeDirRule[] directories)
    {
        AddDirectories((IEnumerable<IncludeDirRule>) directories);
    }

    public IEnumerable<string> IncludeArguments
    {
        get
        {
            var items = IncludePaths;
            List<string> args = new(items.Count);

            foreach (var item in items)
            {
                var path = item.Path;
                args.Add(
                    item.Rule.IsOverride
                        ? $"-I\"{path}\""
                        : $"-isystem\"{path}\""
                );
            }

            foreach (var path in args)
            {
                Logger.Message("Path used for castxml [{0}]", path);
            }

            return args;
        }
    }

    public IReadOnlyList<Item> IncludePaths
    {
        get
        {
            var paths = new List<Item>();

            foreach (var directory in includeDirectoryList)
            {
                var path = directory.Path;

                // Is Using registry?
                if (path.StartsWith("="))
                {
#if NET6_0_OR_GREATER
                    if (OperatingSystem.IsWindows())
#else
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
#endif
                    {
                        var registryPath = path.Substring(1);
                        var indexOfSubPath = directory.Path.IndexOf(";");
                        var subPath = "";
                        if (indexOfSubPath >= 0)
                        {
                            subPath = registryPath.Substring(indexOfSubPath);
                            registryPath = registryPath.Substring(0, indexOfSubPath - 1);
                        }

                        var (registryPathPortion, success) = ResolveRegistryDirectory(registryPath);

                        if (!success)
                            continue;

                        path = Path.Combine(registryPathPortion, subPath);
                    }
                    else
                    {
                        Logger.Error(LoggingCodes.RegistryKeyNotFound,
                                     "Unable to resolve registry paths when not on Windows.");
                    }
                }

                paths.Add(new Item(path.TrimEnd('\\'), directory));
            }

            return paths;
        }
    }

    public sealed class Item
    {
        public Item(string path, IncludeDirRule rule)
        {
            Path = path;
            Rule = rule;
        }

        public string Path { get; }
        public IncludeDirRule Rule { get; }
    }

    [SupportedOSPlatform("windows")]
    private (string path, bool success) ResolveRegistryDirectory(string registryPath)
    {
        string path = null;
        var success = true;
        var indexOfKey = registryPath.LastIndexOf("\\");
        var subKeyStr = registryPath.Substring(indexOfKey + 1);
        registryPath = registryPath.Substring(0, indexOfKey);

        var indexOfHive = registryPath.IndexOf("\\");
        var hiveStr = registryPath.Substring(0, indexOfHive).ToUpper();
        registryPath = registryPath.Substring(indexOfHive + 1);

        try
        {
            var hive = RegistryHive.LocalMachine;
            switch (hiveStr)
            {
                case "HKEY_LOCAL_MACHINE":
                    hive = RegistryHive.LocalMachine;
                    break;
                case "HKEY_CURRENT_USER":
                    hive = RegistryHive.CurrentUser;
                    break;
                case "HKEY_CURRENT_CONFIG":
                    hive = RegistryHive.CurrentConfig;
                    break;
            }

            using (var rootKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry32))
            using (var subKey = rootKey.OpenSubKey(registryPath))
            {
                if (subKey == null)
                {
                    Logger.Error(LoggingCodes.RegistryKeyNotFound, "Unable to locate key [{0}] in registry",
                                 registryPath);
                    success = false;
                }

                path = subKey.GetValue(subKeyStr).ToString();
                Logger.Message($"Resolved registry path {registryPath} to {path}");
            }
        }
        catch (Exception)
        {
            Logger.Error(LoggingCodes.RegistryKeyNotFound, "Unable to locate key [{0}] in registry", registryPath);
            success = false;
        }

        return (path, success);
    }
}