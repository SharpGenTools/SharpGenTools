using SharpGen.Config;
using SharpGen.CppModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SharpGen.Parser
{
    class CppExtensionHeaderGenerator
    {
        internal const string EndTagCustomEnumItem = "__sharpgen_enumitem__";
        internal const string EndTagCustomVariable = "__sharpgen_var__";
        private readonly Dictionary<string, string> _variableMacrosDefined = new Dictionary<string, string>();

        private readonly CppModule module;

        public CppExtensionHeaderGenerator(MacroManager macroManager, CppModule module)
        {
            MacroManager = macroManager;
            this.module = module;
        }

        public MacroManager MacroManager { get; }

        public CppModule GenerateExtensionHeaders(ConfigFile configRoot, string outputPath, HashSet<string> filesWithExtensions)
        {
            MacroManager.Parse(Path.Combine(outputPath, $"{configRoot.Id}.h"), module);

            // Dump includes
            foreach (var configFile in configRoot.ConfigFilesLoaded)
            {
                // Dump Create from macros
                if (filesWithExtensions.Contains(configFile.Id) && configFile.IsConfigUpdated)
                {
                    using (var extension = File.OpenWrite(Path.Combine(outputPath, configFile.ExtensionFileName)))
                    using (var extensionWriter = new StreamWriter(extension))
                    {
                        foreach (var typeBaseRule in configFile.Extension)
                        {
                            if (typeBaseRule.GeneratesExtensionHeader())
                                extensionWriter.Write(CreateCppFromMacro(typeBaseRule));
                            else if (typeBaseRule is ContextRule)
                                HandleContextRule(configFile, (ContextRule)typeBaseRule);
                        }
                    }
                }
            }

            return module;
        }


        /// <summary>
        /// Handles the context rule.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="contextRule">The context rule.</param>
        private void HandleContextRule(ConfigFile file, ContextRule contextRule)
        {
            if (contextRule is ClearContextRule)
                module.ClearContextFind();
            else
            {
                var contextIds = new List<string>();

                if (!string.IsNullOrEmpty(contextRule.ContextSetId))
                {
                    var contextSet = file.FindContextSetById(contextRule.ContextSetId);
                    if (contextSet != null)
                        contextIds.AddRange(contextSet.Contexts);
                }
                contextIds.AddRange(contextRule.Ids);

                module.AddContextRangeFind(contextIds);
            }
        }

        /// <summary>
        /// Creates a C++ declaration from a macro rule.
        /// </summary>
        /// <param name="rule">The macro rule.</param>
        /// <returns>A C++ declaration string</returns>
        private string CreateCppFromMacro(ConfigBaseRule rule)
        {
            if (rule is CreateCppExtensionRule)
            {
                return CreateEnumFromMacro((CreateCppExtensionRule)rule);
            }

            if (rule is ConstantRule)
            {
                return CreateVariableFromMacro((ConstantRule)rule);
            }
            return "";
        }

        /// <summary>
        /// Creates a C++ enum declaration from a macro rule.
        /// </summary>
        /// <param name="createCpp">The macro rule.</param>
        /// <returns>A C++ enum declaration string</returns>
        private string CreateEnumFromMacro(CreateCppExtensionRule createCpp)
        {
            var cppEnumText = new StringBuilder();

            cppEnumText.AppendLine("// Enum created from: " + createCpp);
            cppEnumText.AppendLine("enum " + createCpp.Enum + " {");

            foreach (CppDefine macroDef in module.Find<CppDefine>(createCpp.Macro))
            {
                var macroName = macroDef.Name + EndTagCustomEnumItem;

                // Only add the macro once (could have multiple identical macro in different includes)
                if (!_variableMacrosDefined.ContainsKey(macroName))
                {
                    cppEnumText.AppendFormat("\t {0} = {1},\n", macroName, macroDef.Value);
                    _variableMacrosDefined.Add(macroName, macroDef.Value);
                }
            }
            cppEnumText.AppendLine("};");

            return cppEnumText.ToString();
        }

        /// <summary>
        /// Creates a C++ variable declaration from a macro rule.
        /// </summary>
        /// <param name="cstRule">The macro rule.</param>
        /// <returns>A C++ variable declaration string</returns>
        private string CreateVariableFromMacro(ConstantRule cstRule)
        {
            var builder = new StringBuilder();

            builder.AppendLine("// Variable created from: " + cstRule);

            foreach (CppDefine macroDef in module.Find<CppDefine>(cstRule.Macro))
            {
                var macroName = macroDef.Name + EndTagCustomVariable;

                // Only add the macro once (could have multiple identical macro in different includes)
                if (!_variableMacrosDefined.ContainsKey(macroName))
                {
                    builder.AppendFormat("extern \"C\" {0} {1} = {3}{2};\n", cstRule.CppType ?? cstRule.Type, macroName, macroDef.Name, cstRule.CppCast ?? "");
                    _variableMacrosDefined.Add(macroName, macroDef.Name);
                }
            }
            return builder.ToString();
        }
    }
}
