using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SharpGen.Config;
using SharpGen.Logging;
using SharpGen.Parser;

namespace SharpGen.Platform
{
    public sealed class IncludeDirectoryResolver : IIncludeDirectoryResolver
    {
        private readonly Logger logger;
        private readonly List<IncludeDirRule> includeDirectoryList = new List<IncludeDirRule>();

        public IncludeDirectoryResolver(Logger logger)
        {
            this.logger = logger;
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

        public IEnumerable<string> IncludePaths
        {
            get
            {
                var paths = new List<string>();

                foreach (var directory in includeDirectoryList)
                {
                    var path = directory.Path;

                    // Is Using registry?
                    if (path.StartsWith("="))
                    {
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                            logger.Error(LoggingCodes.RegistryKeyNotFound, "Unable to resolve registry paths when not on Windows.");
                        }
                    }

                    if (directory.IsOverride)
                    {
                        paths.Add("-I\"" + path.TrimEnd('\\') + "\"");
                    }
                    else
                    {
                        paths.Add("-isystem\"" + path.TrimEnd('\\') + "\"");
                    }
                }

                foreach (var path in paths)
                {
                    logger.Message("Path used for castxml [{0}]", path);
                }

                return paths;
            }
        }

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
                        logger.Error(LoggingCodes.RegistryKeyNotFound, "Unable to locate key [{0}] in registry", registryPath);
                        success = false;

                    }
                    path = subKey.GetValue(subKeyStr).ToString();
                    logger.Message($"Resolved registry path {registryPath} to {path}");
                }

            }
            catch (Exception)
            {
                logger.Error(LoggingCodes.RegistryKeyNotFound, "Unable to locate key [{0}] in registry", registryPath);
                success = false;
            }
            return (path, success);
        }

    }
}
