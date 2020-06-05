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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using SharpGen.Logging;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Model;
using System.Reflection;
using System.Xml.Serialization;

namespace SharpGen.Transform
{
    /// <summary>
    /// This class is responsible for generating the C# model from C++ model.
    /// </summary>
    public class TransformManager
    {
        private readonly List<string> _includesToProcess = new List<string>();

        private readonly IDocumentationLinker docLinker;
        private readonly TypeRegistry typeRegistry;
        private readonly NamespaceRegistry namespaceRegistry;
        private readonly MarshalledElementFactory marshalledElementFactory;
        private readonly ConstantManager constantManager;
        private readonly InteropManager interopManager = new InteropManager();
        private readonly GroupRegistry groupRegistry = new GroupRegistry();

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformManager"/> class.
        /// </summary>
        public TransformManager(
            GlobalNamespaceProvider globalNamespace,
            NamingRulesManager namingRules,
            Logger logger,
            TypeRegistry typeRegistry,
            IDocumentationLinker docLinker,
            ConstantManager constantManager)
        {
            GlobalNamespace = globalNamespace;
            Logger = logger;
            NamingRules = namingRules;
            this.docLinker = docLinker;
            this.typeRegistry = typeRegistry;
            this.constantManager = constantManager;
            namespaceRegistry = new NamespaceRegistry(logger);
            marshalledElementFactory = new MarshalledElementFactory(Logger, GlobalNamespace, typeRegistry);

            EnumTransform = new EnumTransform(namingRules, logger, namespaceRegistry, typeRegistry);

            StructTransform = new StructTransform(namingRules, logger, namespaceRegistry, typeRegistry, marshalledElementFactory);

            FunctionTransform = new MethodTransform(namingRules, logger, groupRegistry, marshalledElementFactory, globalNamespace, interopManager);

            InterfaceTransform = new InterfaceTransform(namingRules, logger, globalNamespace, FunctionTransform, FunctionTransform, typeRegistry, namespaceRegistry);
        }

        /// <summary>
        /// Gets the naming rules manager.
        /// </summary>
        /// <value>The naming rules manager.</value>
        public NamingRulesManager NamingRules { get; private set; }
        
        /// <summary>
        /// Gets or sets the enum transformer.
        /// </summary>
        /// <value>The enum transformer.</value>
        private EnumTransform EnumTransform { get; set; }

        /// <summary>
        /// Gets or sets the struct transformer.
        /// </summary>
        /// <value>The struct transformer.</value>
        private StructTransform StructTransform { get; set; }

        /// <summary>
        /// Gets or sets the method transformer.
        /// </summary>
        /// <value>The method transformer.</value>
        private MethodTransform FunctionTransform { get; set; }

        /// <summary>
        /// Gets or sets the interface transformer.
        /// </summary>
        /// <value>The interface transformer.</value>
        private InterfaceTransform InterfaceTransform { get; set; }

        public GlobalNamespaceProvider GlobalNamespace { get; }
        public Logger Logger { get; }

        /// <summary>
        /// Initializes this instance with the specified C++ module and config.
        /// </summary>
        /// <param name="cppModule">The C++ module.</param>
        /// <param name="config">The root config file.</param>
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

            var moduleToTransform = new CppModule
            {
                Name = cppModule.Name
            };

            foreach (var include in cppModule.Includes
                                             .Where(cppInclude => _includesToProcess.Contains(cppInclude.Name))
                                             .ToArray())
            {
                moduleToTransform.Add(include);
            }

            return moduleToTransform;
        }

        /// <summary>
        /// Adds the include to process.
        /// </summary>
        /// <param name="includeId">The include id.</param>
        private void AddIncludeToProcess(string includeId)
        {
            if (!_includesToProcess.Contains(includeId))
                _includesToProcess.Add(includeId);
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
                typeRegistry.BindType(bindingRule.From, typeRegistry.ImportType(bindingRule.To),
                         string.IsNullOrEmpty(bindingRule.Marshal) ?
                         null
                         : typeRegistry.ImportType(bindingRule.Marshal));
            }
        }

        private void ProcessDefines(ConfigFile file)
        {
            foreach (var defineRule in file.Extension.OfType<DefineExtensionRule>())
            {
                CsTypeBase defineType = null;

                if (defineRule.Enum != null)
                {
                    var newEnum = new CsEnum
                    {
                        Name = defineRule.Enum,
                        UnderlyingType = !string.IsNullOrWhiteSpace(defineRule.UnderlyingType)
                            ? (CsFundamentalType)typeRegistry.ImportType(defineRule.UnderlyingType)
                            : null
                    };
                    defineType = newEnum;

                    if (defineRule.SizeOf.HasValue && newEnum.UnderlyingType == null)
                    {
                        var size = defineRule.SizeOf.Value;

                        switch (size)
                        {
                            case 1:
                                newEnum.UnderlyingType = typeRegistry.ImportType(typeof(byte));
                                break;
                            case 2:
                                newEnum.UnderlyingType = typeRegistry.ImportType(typeof(short));
                                break;
                            case 4:
                                newEnum.UnderlyingType = typeRegistry.ImportType(typeof(int));
                                break;
                            default:
                                break;
                        }
                    }
                }
                else if (defineRule.Struct != null)
                {
                    var newStruct = new CsStruct { Name = defineRule.Struct };
                    defineType = newStruct;
                    if (defineRule.HasCustomMarshal.HasValue)
                        newStruct.HasMarshalType = defineRule.HasCustomMarshal.Value;

                    if (defineRule.IsStaticMarshal.HasValue)
                        newStruct.IsStaticMarshal = defineRule.IsStaticMarshal.Value;

                    if (defineRule.HasCustomNew.HasValue)
                        newStruct.HasCustomNew = defineRule.HasCustomNew.Value;

                    if (defineRule.SizeOf.HasValue)
                        newStruct.SetSize(defineRule.SizeOf.Value);

                    if (defineRule.Align.HasValue)
                        newStruct.Align = defineRule.Align.Value;

                    if (defineRule.IsNativePrimitive.HasValue)
                        newStruct.IsNativePrimitive = defineRule.IsNativePrimitive.Value;
                }
                else if (defineRule.Interface != null)
                {
                    var iface =  new CsInterface
                    {
                        Name = defineRule.Interface,
                        ShadowName = defineRule.ShadowName,
                        VtblName = defineRule.VtblName
                    };
                    if (defineRule.NativeImplementation != null)
                    {
                        iface.NativeImplementation = new CsInterface { Name = defineRule.NativeImplementation, IsDualCallback = true };
                        iface.IsCallback = true;
                        iface.IsDualCallback = true;
                        typeRegistry.DefineType(iface.NativeImplementation);
                    }
                    defineType = iface;
                }
                else
                {
                    Logger.Error(LoggingCodes.MissingElementInRule, "Invalid rule [{0}]. Requires one of enum, struct, or interface", defineRule);
                    continue;
                }

                // Define this type
                typeRegistry.DefineType(defineType);
            }
        }

        private void ProcessExtensions(CppElementFinder elementFinder, ConfigFile file)
        {
            // Register defined Types from <extension> tag
            foreach (var extensionRule in file.Extension)
            {
                if (extensionRule is CreateExtensionRule createRule)
                {
                    if (createRule.NewClass != null)
                    {
                        var functionGroup = CreateCsGroup(file.Namespace, createRule.NewClass);
                        if (createRule.Visibility.HasValue)
                            functionGroup.Visibility = createRule.Visibility.Value;
                    }
                    else
                        Logger.Error(LoggingCodes.MissingElementInRule, "Invalid rule [{0}]. Requires class", createRule);
                }
                else if (extensionRule is ConstantRule constantRule)
                {
                    HandleConstantRule(elementFinder, constantRule, file.Namespace);
                }
                else if (extensionRule is ContextRule contextRule)
                {
                    HandleContextRule(elementFinder, file, contextRule);
                }
            }
        }

        private void AttachIncludes(ConfigFile file)
        {
            // Add all includes file
            foreach (var includeRule in file.Includes)
            {
                if (includeRule.Attach.HasValue && includeRule.Attach.Value)
                {
                    AddIncludeToProcess(includeRule.Id);
                    namespaceRegistry.MapIncludeToNamespace(includeRule.Id, includeRule.Namespace ?? file.Namespace, includeRule.Output);
                }
                else
                {
                    // include will be processed
                    if (includeRule.AttachTypes.Count > 0)
                        AddIncludeToProcess(includeRule.Id);

                    foreach (var attachType in includeRule.AttachTypes)
                        namespaceRegistry.AttachTypeToNamespace($"^{attachType}$", includeRule.Namespace ?? file.Namespace, includeRule.Output);
                }
            }

            // Add extensions if any
            if (file.Extension.Count > 0)
            {
                AddIncludeToProcess(file.ExtensionId);
                namespaceRegistry.MapIncludeToNamespace(file.ExtensionId, file.Namespace, null);
            }
        }

        private void ProcessMappings(CppElementFinder elementFinder, ConfigFile file)
        {
            // Perform all mappings from <mappings> tag
            foreach (var configRule in file.Mappings)
            {
                var ruleUsed = false;
                if (configRule is MappingRule mappingRule)
                {
                    if (mappingRule.Enum != null)
                        ruleUsed = elementFinder.ExecuteRule<CppEnum>(mappingRule.Enum, mappingRule);
                    else if (mappingRule.EnumItem != null)
                        ruleUsed = elementFinder.ExecuteRule<CppEnumItem>(mappingRule.EnumItem, mappingRule);
                    else if (mappingRule.Struct != null)
                        ruleUsed = elementFinder.ExecuteRule<CppStruct>(mappingRule.Struct, mappingRule);
                    else if (mappingRule.Field != null)
                        ruleUsed = elementFinder.ExecuteRule<CppField>(mappingRule.Field, mappingRule);
                    else if (mappingRule.Interface != null)
                        ruleUsed = elementFinder.ExecuteRule<CppInterface>(mappingRule.Interface, mappingRule);
                    else if (mappingRule.Function != null)
                        ruleUsed = elementFinder.ExecuteRule<CppFunction>(mappingRule.Function, mappingRule);
                    else if (mappingRule.Method != null)
                        ruleUsed = elementFinder.ExecuteRule<CppMethod>(mappingRule.Method, mappingRule);
                    else if (mappingRule.Parameter != null)
                        ruleUsed = elementFinder.ExecuteRule<CppParameter>(mappingRule.Parameter, mappingRule);
                    else if (mappingRule.Element != null)
                        ruleUsed = elementFinder.ExecuteRule<CppElement>(mappingRule.Element, mappingRule);
                    else if (mappingRule.DocItem != null)
                    {
                        docLinker.AddOrUpdateDocLink(mappingRule.DocItem, mappingRule.MappingNameFinal);
                        ruleUsed = true;
                    }
                }
                else if (configRule is ContextRule contextRule)
                {
                    HandleContextRule(elementFinder, file, contextRule);
                    ruleUsed = true;
                }
                else if (configRule is RemoveRule removeRule)
                {
                    if (removeRule.Enum != null)
                        ruleUsed = RemoveElements<CppEnum>(elementFinder, removeRule.Enum);
                    else if (removeRule.EnumItem != null)
                        ruleUsed = RemoveElements<CppEnumItem>(elementFinder, removeRule.EnumItem);
                    else if (removeRule.Struct != null)
                        ruleUsed = RemoveElements<CppStruct>(elementFinder, removeRule.Struct);
                    else if (removeRule.Field != null)
                        ruleUsed = RemoveElements<CppField>(elementFinder, removeRule.Field);
                    else if (removeRule.Interface != null)
                        ruleUsed = RemoveElements<CppInterface>(elementFinder, removeRule.Interface);
                    else if (removeRule.Function != null)
                        ruleUsed = RemoveElements<CppFunction>(elementFinder, removeRule.Function);
                    else if (removeRule.Method != null)
                        ruleUsed = RemoveElements<CppMethod>(elementFinder, removeRule.Method);
                    else if (removeRule.Parameter != null)
                        ruleUsed = RemoveElements<CppParameter>(elementFinder, removeRule.Parameter);
                    else if (removeRule.Element != null)
                        ruleUsed = RemoveElements<CppElement>(elementFinder, removeRule.Element);
                }
                else if (configRule is MoveRule moveRule)
                {
                    ruleUsed = true;
                    if (moveRule.Struct != null)
                        StructTransform.MoveStructToInner(moveRule.Struct, moveRule.To);
                    else if (moveRule.Method != null)
                        InterfaceTransform.MoveMethodsToInnerInterface(moveRule.Method, moveRule.To, moveRule.Property, moveRule.Base);
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
                item.Parent.Remove(item);
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

        private void Init(IEnumerable<ConfigFile> configFiles)
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

            var asm = new CsAssembly
            {
                Interop = interopManager
            };

            foreach (var ns in namespaceRegistry.Namespaces)
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
                            if (typeRegistry.FindBoundType(cppItem.Name) == null)
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

            var csNameSpace = namespaceRegistry.GetOrCreateNamespace(namespaceName);

            foreach (var cSharpFunctionGroup in csNameSpace.Classes)
            {
                if (cSharpFunctionGroup.Name == className)
                    return cSharpFunctionGroup;
            }


            var group = new CsGroup {Name = className};
            csNameSpace.Add(group);

            groupRegistry.RegisterGroup(namespaceName + "." + className, group);

            return group;
        }

        /// <summary>
        /// Handles the constant rule.
        /// </summary>
        /// <param name="constantRule">The constant rule.</param>
        private void HandleConstantRule(CppElementFinder elementFinder, ConstantRule constantRule, string nameSpace)
        {
            constantManager.AddConstantFromMacroToCSharpType(
                elementFinder,
                constantRule.Macro ?? constantRule.Guid,
                constantRule.ClassName,
                constantRule.Type,
                constantRule.Name,
                constantRule.Value,
                constantRule.Visibility,
                nameSpace);
        }


        public (IEnumerable<BindRule> bindings, IEnumerable<DefineExtensionRule> defines) GenerateTypeBindingsForConsumers()
        {
            return (from record in typeRegistry.GetTypeBindings()
                   select new BindRule(record.CppType, record.CSharpType.QualifiedName, record.MarshalType?.QualifiedName),
                   GenerateDefinesForMappedTypes());
        }

        private IEnumerable<DefineExtensionRule> GenerateDefinesForMappedTypes()
        {
            foreach (var (_, CSharpType, _) in typeRegistry.GetTypeBindings())
            {
                switch (CSharpType)
                {
                    case CsEnum csEnum:
                        yield return new DefineExtensionRule
                        {
                            Enum = csEnum.QualifiedName,
                            SizeOf = csEnum.Size,
                            UnderlyingType = csEnum.UnderlyingType?.BuiltinTypeName
                        };
                        break;
                    case CsStruct csStruct:
                        yield return new DefineExtensionRule
                        {
                            Struct = csStruct.QualifiedName,
                            SizeOf = csStruct.Size,
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
                            VtblName = csInterface.VtblName
                        };
                        break;
                }
            }
        }
    }
}