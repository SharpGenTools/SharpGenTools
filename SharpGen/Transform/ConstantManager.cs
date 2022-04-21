using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;

namespace SharpGen.Transform;

public sealed class ConstantManager
{
    private readonly Dictionary<string, List<CsConstantBase>> _mapConstantToCSharpType = new();
    private readonly Ioc ioc;

    public ConstantManager(NamingRulesManager namingRules, Ioc ioc)
    {
        NamingRules = namingRules ?? throw new ArgumentNullException(nameof(namingRules));
        this.ioc = ioc ?? throw new ArgumentNullException(nameof(ioc));
    }

    private NamingRulesManager NamingRules { get; }
    private IDocumentationLinker DocumentationLinker => ioc.DocumentationLinker;

    /// <summary>
    /// Adds a list of constant gathered from macros/guids to a C# type.
    /// </summary>
    public void AddConstantFromMacroToCSharpType(CppElementFinder elementFinder, string macroRegexp,
                                                 string fullNameCSharpType, string type, string fieldName,
                                                 string valueMap, Visibility? visibility, string nameSpace,
                                                 bool isResultDescriptor)
    {
        var elementRegex = CppElementExtensions.BuildFindFullRegex(macroRegexp);

        string ValueMapFactory()
        {
            // $0: Name of the C++ macro
            // $1: Value of the C++ macro
            // $2: Name of the C#
            // $3: Name of current namespace
            StringBuilder sb = new(valueMap, valueMap.Length * 3 / 2);
            sb.Replace("{", "{{");
            sb.Replace("}", "}}");
            sb.Replace("$0", "{0}");
            sb.Replace("$1", "{1}");
            sb.Replace("$2", "{2}");
            sb.Replace("$3", "{3}");
            return sb.ToString();
        }

        Lazy<string> valueMapProcessed = valueMap is null ? null : new(ValueMapFactory, LazyThreadSafetyMode.None);

        foreach (var macroDef in elementFinder.Find<CppConstant>(elementRegex))
        {
            AddConstant(
                macroDef,
                isResultDescriptor
                    ? name => new CsResultConstant(macroDef, name, macroDef.Value, nameSpace)
                    : name => new CsExpressionConstant(macroDef, name, FindType(macroDef),
                                                       MapValue(macroDef, name, macroDef.Value))
            );
        }

        foreach (var guidDef in elementFinder.Find<CppGuid>(elementRegex))
        {
            var constantType = string.IsNullOrEmpty(type)
                                   ? ioc.TypeRegistry.ImportNonPrimitiveType(typeof(Guid))
                                   : ioc.TypeRegistry.ImportType(type);
            AddConstant(
                guidDef,
                constantType is CsFundamentalType {IsGuid: true}
                    ? name => new CsGuidConstant(guidDef, name, guidDef.Guid)
                    : name => new CsExpressionConstant(guidDef, name, constantType,
                                                       MapValue(guidDef, name, guidDef.Guid.ToString()))
            );
        }

        void AddConstant(CppElement cppDef, ConstantCreatorDelegate creator)
        {
            var finalFieldName = fieldName == null
                                     ? cppDef.Name
                                     : NamingRules.ConvertToPascalCase(
                                         Regex.Replace(cppDef.Name, macroRegexp, fieldName),
                                         NamingFlags.Default
                                     );

            var constant = AddConstantToCSharpType(
                cppDef, fullNameCSharpType, finalFieldName, creator
            );

            if (visibility is { } visibilityValue)
                constant.Visibility = visibilityValue;
        }

        string MapValue(CppElement cppDef, string finalFieldName, string value) =>
            valueMap is null
                ? value
                : string.Format(valueMapProcessed!.Value, cppDef.Name, value, finalFieldName, nameSpace);

        CsTypeBase FindType(CppConstant constant) => ioc.TypeRegistry.ImportType(
            string.IsNullOrEmpty(type) ? constant.TypeName : type
        );
    }

    private delegate CsConstantBase ConstantCreatorDelegate(string csName);

    /// <summary>
    /// Adds a specific C++ constant name/value to a C# type.
    /// </summary>
    /// <param name="cppElement">The C++ element to get the constant from.</param>
    /// <param name="csClassName">Name of the C# class to receive this constant.</param>
    /// <param name="fieldName">Name of the field.</param>
    /// <param name="creator"></param>
    /// <returns>The C# variable declared.</returns>
    private CsConstantBase AddConstantToCSharpType(CppElement cppElement, string csClassName,
                                                   string fieldName, ConstantCreatorDelegate creator)
    {
        if (!_mapConstantToCSharpType.TryGetValue(csClassName, out var constantDefinitions))
        {
            constantDefinitions = new();
            _mapConstantToCSharpType.Add(csClassName, constantDefinitions);
        }

        // Check that the constant is not already present
        foreach (var constantDefinition in constantDefinitions)
        {
            if (constantDefinition.CppElementName == cppElement.Name)
                return constantDefinition;
        }

        var constantToAdd = creator(fieldName);
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

        var qualifiedName = csType.QualifiedName;

        if (!_mapConstantToCSharpType.TryGetValue(qualifiedName, out var list))
            return;

        foreach (var constantDef in list)
            csType.Add(constantDef);
    }
}