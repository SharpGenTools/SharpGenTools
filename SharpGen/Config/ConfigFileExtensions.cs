using SharpGen.CppModel;
using SharpGen.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGen.Config
{
    static class ConfigFileExtensions
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
    }
}
