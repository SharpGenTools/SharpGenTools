using System.Collections.Generic;
using System.Linq;
using SharpGen.CppModel;
using SharpGen.Logging;

namespace SharpGen.Config;

public static class ConfigExtensions
{
    public static CppModule CreateSkeletonModule(this ConfigFile config)
    {
        var module = new CppModule(config.Id);
        foreach (var includeRule in config.ConfigFilesLoaded.SelectMany(cfg => cfg.Includes))
        {
            var cppInclude = module.FindInclude(includeRule.Id);
            if (cppInclude != null)
                continue;

            module.Add(new CppInclude(includeRule.Id));
        }

        return module;
    }

    public static void ExpandDynamicVariables(this ConfigFile config, Logger logger, CppModule module)
    {
        // Load all defines and store them in the config file to allow dynamic variable substitution
        foreach (var cppInclude in module.Includes)
        {
            foreach (var cppDefine in cppInclude.Macros)
            {
                config.DynamicVariables[cppDefine.Name] = cppDefine.Value;
            }
        }

        // Expand all variables with all dynamic variables
        config.ExpandVariables(true, logger);
    }

    public static void GetFilesWithIncludesAndExtensionHeaders(this ConfigFile configRoot,
                                                               out HashSet<ConfigFile> filesWithIncludes,
                                                               out HashSet<ConfigFile> filesWithExtensionHeaders)
    {
        filesWithExtensionHeaders = new HashSet<ConfigFile>(ConfigFile.IdComparer);
        filesWithIncludes = new HashSet<ConfigFile>(ConfigFile.IdComparer);

        // Check if the file has any includes related config
        foreach (var configFile in configRoot.ConfigFilesLoaded)
        {
            // Build prolog
            var includesAnyFiles = configFile.IncludeProlog.Count > 0 || configFile.Includes.Count > 0 ||
                                   configFile.References.Count > 0;

            if (configFile.Extension.Any(rule => rule.GeneratesExtensionHeader()))
            {
                filesWithExtensionHeaders.Add(configFile);
                includesAnyFiles = true;
            }

            // If this config file has any include rules
            if (includesAnyFiles)
                filesWithIncludes.Add(configFile);
        }
    }

    /// <summary>
    /// Checks if this rule is creating headers extension.
    /// </summary>
    /// <param name="rule">The rule to check.</param>
    /// <returns>true if the rule is creating an header extension.</returns>
    public static bool GeneratesExtensionHeader(this ExtensionBaseRule rule)
    {
        return rule is CreateCppExtensionRule createCpp && !string.IsNullOrEmpty(createCpp.Macro)
            || rule is ConstantRule constant && !string.IsNullOrEmpty(constant.Macro);
    }
}