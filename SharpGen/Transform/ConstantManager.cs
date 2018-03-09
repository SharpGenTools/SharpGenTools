using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpGen.Transform
{
    public class ConstantManager
    {
        private readonly Dictionary<string, List<CsVariable>> _mapConstantToCSharpType = new Dictionary<string, List<CsVariable>>();

        public ConstantManager(NamingRulesManager namingRules, IDocumentationLinker linker)
        {
            NamingRules = namingRules;
            DocumentationLinker = linker;
        }

        public NamingRulesManager NamingRules { get; }
        public IDocumentationLinker DocumentationLinker { get; }

        /// <summary>
        /// Adds a list of constant gathered from macros/guids to a C# type.
        /// </summary>
        /// <param name="elementFinder">The C++ module to search.</param>
        /// <param name="macroRegexp">The macro regexp.</param>
        /// <param name="fullNameCSharpType">Full type of the name C sharp.</param>
        /// <param name="type">The type.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="valueMap">The value map.</param>
        /// <param name="visibility">The visibility.</param>
        /// <param name="nameSpace">The current namespace.</param>
        public void AddConstantFromMacroToCSharpType(CppElementFinder elementFinder, string macroRegexp, string fullNameCSharpType, string type, string fieldName, string valueMap,
                                                     Visibility? visibility, string nameSpace)
        {
            var constantDefinitions = elementFinder.Find<CppConstant>(macroRegexp);
            var regex = new Regex(macroRegexp);

            // $0: Name of the C++ macro
            // $1: Value of the C++ macro
            // $2: Name of the C#
            // $3: Name of current namespace
            if (valueMap != null)
            {
                valueMap = valueMap.Replace("{", "{{");
                valueMap = valueMap.Replace("}", "}}");
                valueMap = valueMap.Replace("$0", "{0}");
                valueMap = valueMap.Replace("$1", "{1}");
                valueMap = valueMap.Replace("$2", "{2}");
                valueMap = valueMap.Replace("$3", "{3}");
            }

            foreach (var macroDef in constantDefinitions)
            {
                var finalFieldName = fieldName == null ?
                    macroDef.Name
                    : NamingRules.ConvertToPascalCase(
                        regex.Replace(macroDef.Name, fieldName),
                        NamingFlags.Default);
                var finalValue = valueMap == null ?
                    macroDef.Value
                    : string.Format(valueMap, macroDef.Name, macroDef.Value, finalFieldName, nameSpace);

                var constant = AddConstantToCSharpType(macroDef, fullNameCSharpType, type, finalFieldName, finalValue);
                constant.Visibility = visibility ?? Visibility.Public | Visibility.Const;
            }

            var guidDefinitions = elementFinder.Find<CppGuid>(macroRegexp);
            foreach (var guidDef in guidDefinitions)
            {
                var finalFieldName = fieldName == null ?
                    guidDef.Name
                    : NamingRules.ConvertToPascalCase(
                        regex.Replace(guidDef.Name, fieldName),
                        NamingFlags.Default);
                var finalValue = valueMap == null ?
                    guidDef.Guid.ToString()
                    : string.Format(valueMap, guidDef.Name, guidDef.Guid.ToString(), finalFieldName, nameSpace);

                var constant = AddConstantToCSharpType(guidDef, fullNameCSharpType, type, finalFieldName, finalValue);
                constant.Visibility = visibility ?? Visibility.Public | Visibility.Static | Visibility.Readonly;
            }
        }

        /// <summary>
        /// Adds a specific C++ constant name/value to a C# type.
        /// </summary>
        /// <param name="cppElement">The C++ element to get the constant from.</param>
        /// <param name="csClassName">Name of the C# class to receive this constant.</param>
        /// <param name="typeName">The type name of the C# constant</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value of this constant.</param>
        /// <returns>The C# variable declared.</returns>
        private CsVariable AddConstantToCSharpType(CppElement cppElement, string csClassName, string typeName, string fieldName, string value)
        {
            if (!_mapConstantToCSharpType.TryGetValue(csClassName, out List<CsVariable> constantDefinitions))
            {
                constantDefinitions = new List<CsVariable>();
                _mapConstantToCSharpType.Add(csClassName, constantDefinitions);
            }

            // Check that the constant is not already present
            foreach (var constantDefinition in constantDefinitions)
            {
                if (constantDefinition.CppElementName == cppElement.Name)
                    return constantDefinition;
            }

            var constantToAdd = new CsVariable(typeName, fieldName, value)
            {
                CppElement = cppElement
            };
            constantDefinitions.Add(constantToAdd);

            DocumentationLinker.AddOrUpdateDocLink(cppElement.Name, constantToAdd.QualifiedName);

            return constantToAdd;
        }

        /// <summary>
        /// Tries to attach declared constants to this C# type.
        /// </summary>
        /// <param name="csType">The C# type</param>
        public void AttachConstants(CsBase csType)
        {
            foreach (var innerElement in csType.Items)
                AttachConstants(innerElement);

            foreach (var keyValuePair in _mapConstantToCSharpType)
            {
                if (csType.QualifiedName == keyValuePair.Key)
                {
                    foreach (var constantDef in keyValuePair.Value)
                        csType.Add(constantDef);
                }
            }
        }
    }
}
