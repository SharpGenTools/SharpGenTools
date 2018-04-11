using SharpGen.CppModel;
using SharpGen.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGen.Config
{
    public static class ConfigExtensions
    {
        public static CppModule CreateSkeletonModule(this ConfigFile config)
        {
            var module = new CppModule();
            foreach (var includeRule in config.ConfigFilesLoaded.SelectMany(cfg => cfg.Includes))
            {
                var cppInclude = module.FindInclude(includeRule.Id);
                if (cppInclude == null)
                {
                    cppInclude = new CppInclude { Name = includeRule.Id };
                    module.Add(cppInclude);
                }
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

        public static (HashSet<string> filesWithIncludes, HashSet<string> filesWithExtensionHeaders)
            GetFilesWithIncludesAndExtensionHeaders(this ConfigFile configRoot)
        {
            var filesWithExtensionHeaders = new HashSet<string>();

            var filesWithIncludes = new HashSet<string>();

            // Check if the file has any includes related config
            foreach (var configFile in configRoot.ConfigFilesLoaded)
            {
                var includesAnyFiles = false;

                // Build prolog
                if (configFile.IncludeProlog.Count > 0)
                    includesAnyFiles = true;

                if (configFile.Includes.Count > 0)
                    includesAnyFiles = true;

                if (configFile.References.Count > 0)
                    includesAnyFiles = true;

                if (configFile.Extension.Any(rule => rule.GeneratesExtensionHeader()))
                {
                    filesWithExtensionHeaders.Add(configFile.Id);
                    includesAnyFiles = true;
                }

                // If this config file has any include rules
                if (includesAnyFiles)
                    filesWithIncludes.Add(configFile.Id);
            }

            return (filesWithIncludes, filesWithExtensionHeaders);
        }

        /// <summary>
        /// Checks if this rule is creating headers extension.
        /// </summary>
        /// <param name="rule">The rule to check.</param>
        /// <returns>true if the rule is creating an header extension.</returns>
        public static bool GeneratesExtensionHeader(this ExtensionBaseRule rule)
        {
            return (rule is CreateCppExtensionRule createCpp && !string.IsNullOrEmpty(createCpp.Macro))
                || (rule is ConstantRule constant && !string.IsNullOrEmpty(constant.Macro));
        }
    }
}
