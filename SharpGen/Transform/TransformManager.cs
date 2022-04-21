// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpGen.Logging;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;

namespace SharpGen.Transform;

/// <summary>
/// This class is responsible for generating the C# model from C++ model.
/// </summary>
public sealed class TransformManager
{
    private IDocumentationLinker DocLinker => ioc.DocumentationLinker;
    private TypeRegistry TypeRegistry => ioc.TypeRegistry;
    private Logger Logger => ioc.Logger;
    private NamespaceRegistry NamespaceRegistry { get; }
    private readonly HashSet<string> includesToProcess = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly ConstantManager constantManager;
    private readonly GroupRegistry groupRegistry = new();
    private readonly Ioc ioc;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformManager"/> class.
    /// </summary>
    public TransformManager(NamingRulesManager namingRules, ConstantManager constantManager, Ioc ioc)
    {
        NamingRules = namingRules;
        this.constantManager = constantManager;
        this.ioc = ioc ?? throw new ArgumentNullException(nameof(ioc));
        NamespaceRegistry = new NamespaceRegistry(ioc);
        MarshalledElementFactory marshalledElementFactory = new(ioc);
        InteropSignatureTransform interopSignatureTransform = new(ioc);

        EnumTransform = new EnumTransform(namingRules, NamespaceRegistry, ioc);
        StructTransform = new StructTransform(namingRules, NamespaceRegistry, marshalledElementFactory, ioc);
        FunctionTransform = new MethodTransform(namingRules, groupRegistry, marshalledElementFactory, interopSignatureTransform, ioc);
        InterfaceTransform = new InterfaceTransform(namingRules, FunctionTransform, FunctionTransform, NamespaceRegistry, interopSignatureTransform, ioc);
    }

    /// <summary>
    /// Gets the naming rules manager.
    /// </summary>
    /// <value>The naming rules manager.</value>
    private NamingRulesManager NamingRules { get; }
        
    /// <summary>
    /// Gets or sets the enum transformer.
    /// </summary>
    /// <value>The enum transformer.</value>
    private EnumTransform EnumTransform { get; }

    /// <summary>
    /// Gets or sets the struct transformer.
    /// </summary>
    /// <value>The struct transformer.</value>
    private StructTransform StructTransform { get; }

    /// <summary>
    /// Gets or sets the method transformer.
    /// </summary>
    /// <value>The method transformer.</value>
    private MethodTransform FunctionTransform { get; }

    /// <summary>
    /// Gets or sets the interface transformer.
    /// </summary>
    /// <value>The interface transformer.</value>
    private InterfaceTransform InterfaceTransform { get; }

    /// <summary>
    /// Initializes this instance with the specified C++ module and config.
    /// </summary>
    /// <returns>The module to transform after mapping rules have been applied.</returns>
    private CppModule MapModule(CppModule cppModule, IReadOnlyCollection<ConfigFile> configFiles)
    {
        var numberOfConfigFilesToParse = configFiles.Count;

        var indexFile = 0;

        // Process each config file
        foreach (var configFile in configFiles)
        {
            Logger.Progress(
                30 + (indexFile * 30) / numberOfConfigFilesToParse,
                "Processing mapping rules [{0}]",
                configFile.Id
            );

            ProcessCppModuleWithConfig(cppModule, configFile);
            indexFile++;
        }

        // Strip out includes we aren't processing from transformation

        var moduleToTransform = new CppModule(cppModule.Name);

        moduleToTransform.AddRange(
            cppModule.Includes
                     .Where(cppInclude => includesToProcess.Contains(cppInclude.Name))
        );

        return moduleToTransform;
    }

    /// <summary>
    /// Adds the include to process.
    /// </summary>
    /// <param name="includeId">The include id.</param>
    private void AddIncludeToProcess(string includeId)
    {
        includesToProcess.Add(includeId);
    }

    /// <summary>
    /// Process the specified config file.
    /// </summary>
    /// <param name="file">The file.</param>
    private void ProcessCppModuleWithConfig(CppModule cppModule, ConfigFile file)
    {
        Logger.PushLocation(file.AbsoluteFilePath);
        try
        {
            Logger.Message($"Process rules for config [{file.Id}] namespace [{file.Namespace}]");

            var elementFinder = new CppElementFinder(cppModule);
            if (file.Namespace != null)
            {
                AttachIncludes(file);
                ProcessExtensions(elementFinder, file); 
            }
            ProcessMappings(elementFinder, file);
        }
        finally
        {
            Logger.PopLocation();
        }
    }

    private void UpdateNamingRules(ConfigFile file)
    {
        foreach (var namingRule in file.Naming)
        {
            if (namingRule is NamingRuleShort)
                NamingRules.AddShortNameRule(namingRule.Name, namingRule.Value);
        }
    }

    private void RegisterBindings(ConfigFile file)
    {
        foreach (var bindingRule in file.Bindings)
        {
            TypeRegistry.BindType(
                bindingRule.From,
                TypeRegistry.ImportType(bindingRule.To),
                string.IsNullOrEmpty(bindingRule.Marshal)
                    ? null
                    : TypeRegistry.ImportType(bindingRule.Marshal),
                file.Id
            );
        }
    }

    private void ProcessDefines(ConfigFile file)
    {
        foreach (var defineRule in file.Extension.OfType<DefineExtensionRule>())
        {
            CsTypeBase defineType;

            if (defineRule.Enum != null)
            {
                var underlyingType = string.IsNullOrWhiteSpace(defineRule.UnderlyingType)
                                         ? null
                                         : TypeRegistry.ImportPrimitiveType(defineRule.UnderlyingType);

                if (defineRule.SizeOf is {} size && underlyingType == null)
                {
                    underlyingType = size switch
                    {
                        1 => TypeRegistry.UInt8,
                        2 => TypeRegistry.Int16,
                        4 => TypeRegistry.Int32,
                        8 => TypeRegistry.Int64,
                        _ => null
                    };
                }

                defineType = new CsEnum(null, defineRule.Enum, underlyingType);
            }
            else if (defineRule.Struct != null)
            {
                var newStruct = new CsStruct(null, defineRule.Struct);
                defineType = newStruct;
                if (defineRule.HasCustomMarshal is { } hasCustomMarshal)
                    newStruct.HasMarshalType = hasCustomMarshal;

                if (defineRule.IsStaticMarshal is { } isStaticMarshal)
                    newStruct.IsStaticMarshal = isStaticMarshal;

                if (defineRule.HasCustomNew is { } hasCustomNew)
                    newStruct.HasCustomNew = hasCustomNew;

                if (defineRule.SizeOf is { } size)
                    newStruct.StructSize = checked((uint) size);

                if (defineRule.Align is { } align)
                    newStruct.Align = align;

                if (defineRule.IsNativePrimitive is { } isNativePrimitive)
                    newStruct.IsNativePrimitive = isNativePrimitive;
            }
            else if (defineRule.Interface != null)
            {
                CsInterface iface = new(null, defineRule.Interface);

                if (defineRule.IsCallbackInterface is { } isCallbackInterface)
                    iface.IsCallback = isCallbackInterface;
                if (defineRule.ShadowName is { } shadowName)
                    iface.ShadowName = shadowName;
                if (defineRule.VtblName is { } vtblName)
                    iface.VtblName = vtblName;

                if (defineRule.NativeImplementation != null)
                {
                    iface.NativeImplementation = new CsInterface(null, defineRule.NativeImplementation)
                    {
                        IsDualCallback = true
                    };
                    iface.IsCallback = true;
                    iface.IsDualCallback = true;
                    TypeRegistry.DefineType(iface.NativeImplementation);
                }

                defineType = iface;
            }
            else
            {
                Logger.Error(LoggingCodes.MissingElementInRule, "Invalid rule [{0}]. Requires one of enum, struct, or interface", defineRule);
                continue;
            }

            // Define this type
            TypeRegistry.DefineType(defineType);
        }
    }

    private void ProcessExtensions(CppElementFinder elementFinder, ConfigFile file)
    {
        // Register defined Types from <extension> tag
        foreach (var extensionRule in file.Extension)
        {
            switch (extensionRule)
            {
                case CreateExtensionRule { NewClass: { } newClass } createRule:
                {
                    var functionGroup = CreateCsGroup(file.Namespace, newClass);
                    if (createRule is { Visibility: { } visibility })
                        functionGroup.Visibility = visibility;
                    break;
                }
                case CreateExtensionRule createRule:
                    Logger.Error(LoggingCodes.MissingElementInRule, "Invalid rule [{0}]. Requires class",
                                 createRule);
                    break;
                case ConstantRule constantRule:
                    HandleConstantRule(elementFinder, constantRule, file.Namespace);
                    break;
                case ContextRule contextRule:
                    HandleContextRule(elementFinder, file, contextRule);
                    break;
            }
        }
    }

    private void AttachIncludes(ConfigFile file)
    {
        // Add all includes file
        foreach (var includeRule in file.Includes)
        {
            var includeRuleId = includeRule.Id;
            if (includeRule.Attach.HasValue && includeRule.Attach.Value)
            {
                AddIncludeToProcess(includeRuleId);
                NamespaceRegistry.MapIncludeToNamespace(includeRuleId, includeRule.Namespace ?? file.Namespace);
            }
            else
            {
                // include will be processed
                if (includeRule.AttachTypes.Count > 0)
                    AddIncludeToProcess(includeRuleId);

                foreach (var attachType in includeRule.AttachTypes)
                    NamespaceRegistry.AttachTypeToNamespace($"^{attachType}$", includeRule.Namespace ?? file.Namespace);
            }
        }

        // Add extensions if any
        if (file.Extension.Count > 0)
        {
            var fileExtensionId = file.ExtensionId;
            AddIncludeToProcess(fileExtensionId);
            NamespaceRegistry.MapIncludeToNamespace(fileExtensionId, file.Namespace);
        }
    }

    private void ProcessMappings(CppElementFinder elementFinder, ConfigFile file)
    {
        // Perform all mappings from <mappings> tag
        foreach (var configRule in file.Mappings)
        {
            var ruleUsed = false;
            switch (configRule)
            {
                case MappingRule {Enum: { }} mappingRule:
                    ruleUsed = elementFinder.ExecuteRule<CppEnum>(mappingRule.Enum, mappingRule);
                    break;
                case MappingRule {EnumItem: { }} mappingRule:
                    ruleUsed = elementFinder.ExecuteRule<CppEnumItem>(mappingRule.EnumItem, mappingRule);
                    break;
                case MappingRule {Struct: { }} mappingRule:
                    ruleUsed = elementFinder.ExecuteRule<CppStruct>(mappingRule.Struct, mappingRule);
                    break;
                case MappingRule {Field: { }} mappingRule:
                    ruleUsed = elementFinder.ExecuteRule<CppField>(mappingRule.Field, mappingRule);
                    break;
                case MappingRule {Interface: { }} mappingRule:
                    ruleUsed = elementFinder.ExecuteRule<CppInterface>(mappingRule.Interface, mappingRule);
                    break;
                case MappingRule {Function: { }} mappingRule:
                    ruleUsed = elementFinder.ExecuteRule<CppFunction>(mappingRule.Function, mappingRule);
                    break;
                case MappingRule {Method: { }} mappingRule:
                    ruleUsed = elementFinder.ExecuteRule<CppMethod>(mappingRule.Method, mappingRule);
                    break;
                case MappingRule {Parameter: { }} mappingRule:
                    ruleUsed = elementFinder.ExecuteRule<CppParameter>(mappingRule.Parameter, mappingRule);
                    break;
                case MappingRule {Element: { }} mappingRule:
                    ruleUsed = elementFinder.ExecuteRule<CppElement>(mappingRule.Element, mappingRule);
                    break;
                case MappingRule {DocItem: { }} mappingRule:
                    DocLinker.AddOrUpdateDocLink(mappingRule.DocItem, mappingRule.MappingNameFinal);
                    ruleUsed = true;
                    break;
                case RemoveRule {Enum: { }} removeRule:
                    ruleUsed = RemoveElements<CppEnum>(elementFinder, removeRule.Enum);
                    break;
                case RemoveRule {EnumItem: { }} removeRule:
                    ruleUsed = RemoveElements<CppEnumItem>(elementFinder, removeRule.EnumItem);
                    break;
                case RemoveRule {Struct: { }} removeRule:
                    ruleUsed = RemoveElements<CppStruct>(elementFinder, removeRule.Struct);
                    break;
                case RemoveRule {Field: { }} removeRule:
                    ruleUsed = RemoveElements<CppField>(elementFinder, removeRule.Field);
                    break;
                case RemoveRule {Interface: { }} removeRule:
                    ruleUsed = RemoveElements<CppInterface>(elementFinder, removeRule.Interface);
                    break;
                case RemoveRule {Function: { }} removeRule:
                    ruleUsed = RemoveElements<CppFunction>(elementFinder, removeRule.Function);
                    break;
                case RemoveRule {Method: { }} removeRule:
                    ruleUsed = RemoveElements<CppMethod>(elementFinder, removeRule.Method);
                    break;
                case RemoveRule {Parameter: { }} removeRule:
                    ruleUsed = RemoveElements<CppParameter>(elementFinder, removeRule.Parameter);
                    break;
                case RemoveRule {Element: { }} removeRule:
                    ruleUsed = RemoveElements<CppElement>(elementFinder, removeRule.Element);
                    break;
                case ContextRule contextRule:
                    HandleContextRule(elementFinder, file, contextRule);
                    ruleUsed = true;
                    break;
                case MoveRule moveRule:
                {
                    if (moveRule.Struct != null)
                        StructTransform.MoveStructToInner(moveRule.Struct, moveRule.To);
                    else if (moveRule.Method != null)
                        InterfaceTransform.MoveMethodsToInnerInterface(moveRule.Method, moveRule.To, moveRule.Property, moveRule.Base);
                    ruleUsed = true;
                    break;
                }
            }

            if (!ruleUsed)
            {
                Logger.Warning(LoggingCodes.UnusedMappingRule, "Mapping rule [{0}] did not match any elements.", configRule);
            }
        }
    }

    private static bool RemoveElements<T>(CppElementFinder finder, string regex)
        where T : CppElement
    {
        var matchedAny = false;
        foreach (var item in finder.Find<T>(regex).ToList())
        {
            matchedAny = true;
            item.RemoveFromParent();
        }

        return matchedAny;
    }

    /// <summary>
    /// Handles the context rule.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <param name="contextRule">The context rule.</param>
    /// <param name="moduleMapper">The C++ Module we are handling the context rule for.</param>
    private void HandleContextRule(CppElementFinder moduleMapper, ConfigFile file, ContextRule contextRule)
    {
        if (contextRule is ClearContextRule)
            moduleMapper.ClearCurrentContexts();
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

            moduleMapper.AddContexts(contextIds);
        }
    }

    private void Init(IReadOnlyCollection<ConfigFile> configFiles)
    {
        // We have to do these steps first, otherwise we'll get undefined types for types we've mapped, which breaks the mapping.
        foreach (var configFile in configFiles)
        {
            Logger.RunInContext(
                configFile.AbsoluteFilePath,
                () =>
                {
                    // Update Naming Rules
                    UpdateNamingRules(configFile);

                    ProcessDefines(configFile);
                });
        }

        foreach (var configFile in configFiles)
        {
            Logger.RunInContext(
                configFile.AbsoluteFilePath,
                () =>
                {
                    RegisterBindings(configFile);
                });
        }
    }

    /// <summary>
    ///   Maps all C++ types to C#
    /// </summary>
    /// <param name="cppModule">The C++ module to parse.</param>
    /// <param name="configFile">The config file to use to transform the C++ module into C# assemblies.</param>
    public (CsAssembly assembly, IEnumerable<DefineExtensionRule> consumerExtensions) Transform(CppModule cppModule, ConfigFile configFile)
    {
        Init(configFile.ConfigFilesLoaded);

        var moduleToTransform = MapModule(cppModule, configFile.ConfigFilesLoaded);

        var selectedCSharpType = new List<CsBase>();

        // Prepare transform by defining/registering all types to process
        selectedCSharpType.AddRange(PrepareTransform(moduleToTransform, EnumTransform));
        selectedCSharpType.AddRange(PrepareTransform(moduleToTransform, StructTransform));
        selectedCSharpType.AddRange(PrepareTransform(moduleToTransform, InterfaceTransform));
        selectedCSharpType.AddRange(PrepareTransform<CppFunction, CsFunction>(moduleToTransform, FunctionTransform));

        // Transform all types
        Logger.Progress(65, "Transforming enums...");
        ProcessTransform(EnumTransform, selectedCSharpType.OfType<CsEnum>());
        Logger.Progress(70, "Transforming structs...");
        ProcessTransform(StructTransform, selectedCSharpType.OfType<CsStruct>());
        Logger.Progress(75, "Transforming interfaces...");
        ProcessTransform(InterfaceTransform, selectedCSharpType.OfType<CsInterface>());
        Logger.Progress(80, "Transforming functions...");
        ProcessTransform(FunctionTransform, selectedCSharpType.OfType<CsFunction>());

        CsAssembly asm = new();

        foreach (var ns in NamespaceRegistry.Namespaces)
        {
            foreach (var group in ns.Classes)
            {
                constantManager.AttachConstants(group);
            }
            asm.Add(ns);
        }

        return (asm, configFile.ConfigFilesLoaded.SelectMany(file => file.Extension.OfType<DefineExtensionRule>()));
    }

    /// <summary>
    /// Prepares a transformer from C++ to C# model.
    /// </summary>
    /// <typeparam name="TCppElement">The C++ type of data to process</typeparam>
    /// <param name="transform">The transform.</param>
    /// <param name="typeToProcess">The type to process.</param>
    private IEnumerable<TCsElement> PrepareTransform<TCppElement, TCsElement>(CppModule cppModule, ITransformPreparer<TCppElement, TCsElement> transform)
        where TCppElement : CppElement
        where TCsElement : CsBase
    {
        var csElements = new List<TCsElement>();
        // Predefine all structs, typedefs and interfaces
        foreach (var cppInclude in cppModule.Includes)
        {
            foreach (var cppItem in cppInclude.Iterate<TCppElement>())
            {
                Logger.RunInContext(
                    cppItem.ToString(),
                    () =>
                    {
                        // If already mapped, it means that there is already a predefined mapping
                        if (TypeRegistry.FindBoundType(cppItem.Name) == null)
                        {
                            var csElement = transform.Prepare(cppItem);
                            if (csElement != null)
                                csElements.Add(csElement);
                        }
                    });
            }
        }
        return csElements;
    }

    /// <summary>
    /// Processes a transformer from C++ to C# model.
    /// </summary>
    /// <typeparam name="T">The C++ type of data to process</typeparam>
    /// <param name="transform">The transform.</param>
    /// <param name="typeToProcess">The type to process.</param>
    private void ProcessTransform<T>(ITransformer<T> transform, IEnumerable<T> typeToProcess)
        where T : CsBase
    {
        foreach (var csItem in typeToProcess)
        {
            Logger.RunInContext(
                csItem.CppElement.ToString(),
                () =>
                {
                    transform.Process(csItem);
                    constantManager.AttachConstants(csItem);
                });
        }
    }

    /// <summary>
    /// Creates the C# class container used to group together loose elements (i.e. functions, constants).
    /// </summary>
    /// <param name="namespaceName">Name of the namespace.</param>
    /// <param name="className">Name of the class.</param>
    /// 
    /// <returns>The C# class container</returns>
    private CsGroup CreateCsGroup(string namespaceName, string className)
    {
        if (className == null) throw new ArgumentNullException(nameof(className));

        if (className.Contains("."))
        {
            namespaceName = Path.GetFileNameWithoutExtension(className);
            className = Path.GetExtension(className).Trim('.');
        }

        var csNameSpace = NamespaceRegistry.GetOrCreateNamespace(namespaceName);

        foreach (var cSharpFunctionGroup in csNameSpace.Classes)
        {
            if (cSharpFunctionGroup.Name == className)
                return cSharpFunctionGroup;
        }


        CsGroup group = new(className);
        csNameSpace.Add(group);

        groupRegistry.RegisterGroup(namespaceName + "." + className, group);

        return group;
    }

    /// <summary>
    /// Handles the constant rule.
    /// </summary>
    /// <param name="constantRule">The constant rule.</param>
    private void HandleConstantRule(CppElementFinder elementFinder, ConstantRule constantRule, string nameSpace) =>
        constantManager.AddConstantFromMacroToCSharpType(
            elementFinder,
            constantRule.Macro ?? constantRule.Guid,
            constantRule.ClassName,
            constantRule.Type,
            constantRule.Name,
            constantRule.Value,
            constantRule.Visibility,
            nameSpace,
            constantRule.IsResultDescriptor ?? false
        );


    public (IEnumerable<BindRule> bindings, IEnumerable<DefineExtensionRule> defines) GenerateTypeBindingsForConsumers()
    {
        return (from record in TypeRegistry.GetTypeBindings()
                select new BindRule(record.CppType, record.CSharpType.QualifiedName, record.MarshalType?.QualifiedName),
                GenerateDefinesForMappedTypes());
    }

    private IEnumerable<DefineExtensionRule> GenerateDefinesForMappedTypes()
    {
        foreach (var (_, CSharpType, _) in TypeRegistry.GetTypeBindings())
        {
            switch (CSharpType)
            {
                case CsEnum csEnum:
                    CsFundamentalType tempQualifier = csEnum.UnderlyingType;
                    yield return new DefineExtensionRule
                    {
                        Enum = csEnum.QualifiedName,
                        SizeOf = checked((int) csEnum.Size),
                        UnderlyingType = tempQualifier?.Name
                    };
                    break;
                case CsStruct csStruct:
                    yield return new DefineExtensionRule
                    {
                        Struct = csStruct.QualifiedName,
                        SizeOf = checked((int) csStruct.Size),
                        Align = csStruct.Align,
                        HasCustomMarshal = csStruct.HasCustomMarshal,
                        HasCustomNew = csStruct.HasCustomNew,
                        IsStaticMarshal = csStruct.IsStaticMarshal,
                        IsNativePrimitive = csStruct.IsNativePrimitive
                    };
                    break;
                case CsInterface csInterface:
                    yield return new DefineExtensionRule
                    {
                        Interface = csInterface.QualifiedName,
                        NativeImplementation = csInterface.NativeImplementation?.QualifiedName,
                        ShadowName = csInterface.ShadowName,
                        VtblName = csInterface.VtblName,
                        IsCallbackInterface = csInterface.IsCallback
                    };
                    break;
            }
        }
    }
}