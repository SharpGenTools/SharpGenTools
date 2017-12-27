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
using SharpGen.TextTemplating;
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

        private readonly IDocumentationAggregator docAggregator;
        private readonly TypeRegistry typeRegistry;
        private readonly NamespaceRegistry namespaceRegistry;
        private readonly MarshalledElementFactory marshalledElementFactory;
        private readonly ConstantManager constantManager;
        private readonly GroupRegistry groupRegistry = new GroupRegistry();
        private readonly AssemblyManager assemblyManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformManager"/> class.
        /// </summary>
        public TransformManager(
            GlobalNamespaceProvider globalNamespace,
            NamingRulesManager namingRules,
            Logger logger,
            TypeRegistry typeRegistry,
            IDocumentationAggregator docAggregator,
            ConstantManager constantManager,
            AssemblyManager assemblyManager)
        {
            GlobalNamespace = globalNamespace;
            Logger = logger;
            NamingRules = namingRules;
            this.docAggregator = docAggregator;
            this.typeRegistry = typeRegistry;
            this.constantManager = constantManager;
            this.assemblyManager = assemblyManager;
            namespaceRegistry = new NamespaceRegistry(logger, assemblyManager);
            marshalledElementFactory = new MarshalledElementFactory(Logger, GlobalNamespace, typeRegistry);

            EnumTransform = new EnumTransform(namingRules, logger, namespaceRegistry, typeRegistry);

            StructTransform = new StructTransform(namingRules, logger, namespaceRegistry, typeRegistry, marshalledElementFactory);

            FunctionTransform = new MethodTransform(namingRules, logger, groupRegistry, marshalledElementFactory, globalNamespace, typeRegistry);

            InterfaceTransform = new InterfaceTransform(namingRules, logger, globalNamespace, FunctionTransform, FunctionTransform, typeRegistry, namespaceRegistry);
        }

        public bool ForceGenerator { get; set; }

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
        /// <param name="checkFilesPath">The path to place check files in.</param>
        /// <returns>Any rules that must flow to consuming projects.</returns>
        private List<DefineExtensionRule> Init(CppModule cppModule, ConfigFile config, string checkFilesPath)
        {
            var configFiles = config.ConfigFilesLoaded;

            // Compute dependencies first
            // In order to calculate which assembly we need to process
            foreach (var configFile in configFiles)
                ComputeDependencies(configFile);

            // Check which assembly to update
            foreach (var assembly in assemblyManager.Assemblies)
                CheckAssemblyUpdate(assembly, checkFilesPath);


            var numberOfConfigFilesToParse = 0;
            // If we don't need to process it, skip it silently
            foreach (var configFile in configFiles)
                if (configFile.IsMappingToProcess)
                    numberOfConfigFilesToParse++;

            var defines = new List<DefineExtensionRule>();
            var indexFile = 0;
            // Process each config file
            foreach (var configFile in configFiles)
            {
                if (configFile.IsMappingToProcess)
                {
                    Logger.Progress(30 + (indexFile*30)/numberOfConfigFilesToParse, "Processing mapping rules [{0}]", configFile.Assembly ?? configFile.Id);
                    defines.AddRange(ProcessCppModuleWithConfig(cppModule, configFile));
                    indexFile++;
                }
                else
                {
                    // Even if we don't need the mappings, we still need to add the defines so they will flow to consuming projects.
                    defines.AddRange(ProcessDefines(configFile));
                }
            }

            return defines;
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
        /// Computes assembly dependencies from a config file.
        /// </summary>
        /// <param name="file">The file.</param>
        private void ComputeDependencies(ConfigFile file)
        {
            if (string.IsNullOrEmpty(file.Assembly))
                return;

            var assembly = assemblyManager.GetOrCreateAssembly(file.Assembly);

            // Review all includes file
            foreach (var includeRule in file.Includes)
            {
                if (includeRule.Attach.HasValue && includeRule.Attach.Value)
                    // Link this assembly to its config file (for dependency checking)
                    assembly.AddLinkedConfigFile(file);
                else if (includeRule.AttachTypes.Count > 0)
                    // Link this assembly to its config file (for dependency checking)
                    assembly.AddLinkedConfigFile(file);
            }

            // Add link to extensions if any
            if (file.Extension.Count > 0)
                assembly.AddLinkedConfigFile(file);

            // Find all dependencies from all linked config files
            var dependencyList = new List<ConfigFile>();
            foreach (var linkedConfigFile in assembly.ConfigFilesLinked)
                linkedConfigFile.FindAllDependencies(dependencyList);

            // Add full dependency for this assembly from all config files
            foreach (var linkedConfigFile in dependencyList)
                assembly.AddLinkedConfigFile(linkedConfigFile);
        }

        /// <summary>
        /// Checks the assembly is up to date relative to its config dependencies.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="checkFilesPath">The path to where the check file is located.</param>
        private void CheckAssemblyUpdate(CsAssembly assembly, string checkFilesPath)
        {
            var maxUpdateTime = ConfigFile.GetLatestTimestamp(assembly.ConfigFilesLinked);

            if (File.Exists(assembly.CheckFileName))
            {
                if (maxUpdateTime > File.GetLastWriteTime(Path.Combine(checkFilesPath, assembly.CheckFileName)))
                    assembly.IsToUpdate = true;
            }
            else
            {
                assembly.IsToUpdate = true;
            }

            // Force generate
            if (ForceGenerator)
                assembly.IsToUpdate = true;

            if (assembly.IsToUpdate)
            {
                foreach (var linkedConfigFile in assembly.ConfigFilesLinked)
                    linkedConfigFile.IsMappingToProcess = true;
            }
            string updateForMessage = (assembly.IsToUpdate) ? "Config changed. Need to update from" : "Config unchanged. No need to update from";

            Logger.Message("Process assembly [{0}] => {1} dependencies: [{2}]", assembly.QualifiedName, updateForMessage,
                           string.Join(",", assembly.ConfigFilesLinked));
        }

        /// <summary>
        /// Process the specified config file.
        /// </summary>
        /// <param name="file">The file.</param>
        private IEnumerable<DefineExtensionRule> ProcessCppModuleWithConfig(CppModule cppModule, ConfigFile file)
        {
            Logger.PushLocation(file.AbsoluteFilePath);
            try
            {
                CsAssembly assembly = null;

                if (!string.IsNullOrEmpty(file.Assembly))
                    assembly = assemblyManager.GetOrCreateAssembly(file.Assembly);

                if (assembly != null)
                    Logger.Message("Process rules for assembly [{0}] and namespace [{1}]", file.Assembly, file.Namespace);

                // Update Naming Rules
                UpdateNamingRules(file);

                // Only attach includes when there is a bind to an assembly
                if (assembly != null)
                {
                    AttachIncludes(file);

                    ProcessExtensions(cppModule, file);
                }

                // Handle defines separately since they do not depend on the included C++
                // and they need to flow to consuming projects
                var defines = ProcessDefines(file);

                // Register bindings from <bindings> tag
                RegisterBindings(file);

                ProcessMappings(cppModule, file);
                return defines;
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

        private IEnumerable<DefineExtensionRule> ProcessDefines(ConfigFile file)
        {
            var defines = new List<DefineExtensionRule>();
            foreach (var defineRule in file.Extension.OfType<DefineExtensionRule>())
            {
                CsTypeBase defineType = null;

                if (defineRule.Enum != null)
                    defineType = new CsEnum { Name = defineRule.Enum };
                else if (defineRule.Struct != null)
                {
                    defineType = new CsStruct { Name = defineRule.Struct };
                    if (defineRule.HasCustomMarshal.HasValue)
                        ((CsStruct)defineType).HasMarshalType = defineRule.HasCustomMarshal.Value;

                    if (defineRule.IsStaticMarshal.HasValue)
                        ((CsStruct)defineType).IsStaticMarshal = defineRule.IsStaticMarshal.Value;

                    if (defineRule.HasCustomNew.HasValue)
                        ((CsStruct)defineType).HasCustomNew = defineRule.HasCustomNew.Value;
                }
                else if (defineRule.Interface != null)
                    defineType = new CsInterface { Name = defineRule.Interface };
                else if (defineRule.NewClass != null)
                    defineType = new CsClass { Name = defineRule.NewClass };
                else
                {
                    Logger.Error("Invalid rule [{0}]. Requires one of enum, struct, class or interface", defineRule);
                    continue;
                }

                if (defineRule.SizeOf.HasValue)
                    defineType.SizeOf = defineRule.SizeOf.Value;

                if (defineRule.Align.HasValue)
                    defineType.Align = defineRule.Align.Value;

                // Define this type
                typeRegistry.DefineType(defineType);
                defines.Add(defineRule);
            }
            return defines;
        }

        private void ProcessExtensions(CppModule cppModule, ConfigFile file)
        {
            // Register defined Types from <extension> tag
            foreach (var extensionRule in file.Extension)
            {
                if (extensionRule is CreateExtensionRule createRule)
                {
                    if (createRule.NewClass != null)
                    {
                        var functionGroup = CreateCsClassContainer(file.Assembly, file.Namespace, createRule.NewClass);
                        if (createRule.Visibility.HasValue)
                            functionGroup.Visibility = createRule.Visibility.Value;
                    }
                    else
                        Logger.Error("Invalid rule [{0}]. Requires class", createRule);
                }
                else if (extensionRule is ConstantRule constantRule)
                {
                    HandleConstantRule(cppModule, constantRule, file.Namespace);
                }
                else if (extensionRule is ContextRule contextRule)
                {
                    HandleContextRule(cppModule, file, contextRule);
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
                    // include will be processed
                    MapIncludeToNamespace(includeRule.Id, file.Assembly, includeRule.Namespace ?? file.Namespace, includeRule.Output);
                }
                else
                {
                    // include will be processed
                    if (includeRule.AttachTypes.Count > 0)
                        AddIncludeToProcess(includeRule.Id);

                    foreach (var attachType in includeRule.AttachTypes)
                        namespaceRegistry.AttachTypeToNamespace($"^{attachType}$", file.Assembly, includeRule.Namespace ?? file.Namespace, includeRule.Output);
                }
            }

            // Add extensions if any
            if (file.Extension.Count > 0)
                MapIncludeToNamespace(file.ExtensionId, file.Assembly, file.Namespace);
        }

        private void ProcessMappings(CppModule cppModule, ConfigFile file)
        {
            // Perform all mappings from <mappings> tag
            foreach (var configRule in file.Mappings)
            {
                if (configRule is MappingRule mappingRule)
                {
                    if (mappingRule.Enum != null)
                        cppModule.Tag<CppEnum>(mappingRule.Enum, mappingRule);
                    else if (mappingRule.EnumItem != null)
                        cppModule.Tag<CppEnumItem>(mappingRule.EnumItem, mappingRule);
                    else if (mappingRule.Struct != null)
                        cppModule.Tag<CppStruct>(mappingRule.Struct, mappingRule);
                    else if (mappingRule.Field != null)
                        cppModule.Tag<CppField>(mappingRule.Field, mappingRule);
                    else if (mappingRule.Interface != null)
                        cppModule.Tag<CppInterface>(mappingRule.Interface, mappingRule);
                    else if (mappingRule.Function != null)
                        cppModule.Tag<CppFunction>(mappingRule.Function, mappingRule);
                    else if (mappingRule.Method != null)
                        cppModule.Tag<CppMethod>(mappingRule.Method, mappingRule);
                    else if (mappingRule.Parameter != null)
                        cppModule.Tag<CppParameter>(mappingRule.Parameter, mappingRule);
                    else if (mappingRule.Element != null)
                        cppModule.Tag<CppElement>(mappingRule.Element, mappingRule);
                    else if (mappingRule.DocItem != null)
                        docAggregator.AddDocLink(mappingRule.DocItem, mappingRule.MappingNameFinal);
                }
                else if (configRule is ContextRule contextRule)
                {
                    HandleContextRule(cppModule, file, contextRule);
                }
                else if (configRule is RemoveRule removeRule)
                {
                    if (removeRule.Enum != null)
                        cppModule.Remove<CppEnum>(removeRule.Enum);
                    else if (removeRule.EnumItem != null)
                        cppModule.Remove<CppEnumItem>(removeRule.EnumItem);
                    else if (removeRule.Struct != null)
                        cppModule.Remove<CppStruct>(removeRule.Struct);
                    else if (removeRule.Field != null)
                        cppModule.Remove<CppField>(removeRule.Field);
                    else if (removeRule.Interface != null)
                        cppModule.Remove<CppInterface>(removeRule.Interface);
                    else if (removeRule.Function != null)
                        cppModule.Remove<CppFunction>(removeRule.Function);
                    else if (removeRule.Method != null)
                        cppModule.Remove<CppMethod>(removeRule.Method);
                    else if (removeRule.Parameter != null)
                        cppModule.Remove<CppParameter>(removeRule.Parameter);
                    else if (removeRule.Element != null)
                        cppModule.Remove<CppElement>(removeRule.Element);
                }
                else if (configRule is MoveRule moveRule)
                {
                    if (moveRule.Struct != null)
                        StructTransform.MoveStructToInner(moveRule.Struct, moveRule.To);
                    else if (moveRule.Method != null)
                        InterfaceTransform.MoveMethodsToInnerInterface(moveRule.Method, moveRule.To, moveRule.Property, moveRule.Base);
                }
            }
        }

        /// <summary>
        /// Handles the context rule.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="contextRule">The context rule.</param>
        /// <param name="cppModule">The C++ Module we are handling the context rule for.</param>
        private void HandleContextRule(CppModule cppModule, ConfigFile file, ContextRule contextRule)
        {
            if (contextRule is ClearContextRule)
                cppModule.ClearContextFind();
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

                cppModule.AddContextRangeFind(contextIds);
            }
        }

        /// <summary>
        /// Dumps all enums, struct and interfaces to the specified file name.
        /// </summary>
        /// <param name="fileName">Name of the output file.</param>
        public void Dump(string fileName)
        {
            using (var logFile = File.OpenWrite(fileName))
            using (var log = new StreamWriter(logFile, Encoding.ASCII))
            {
                string csv = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                string format = "{1}{0}{2}{0}{3}{0}{4}";

                foreach (var assembly in assemblyManager.Assemblies)
                {
                    foreach (var ns in assembly.Namespaces)
                    {
                        foreach (var element in ns.Enums)
                            log.WriteLine(format, csv, "enum", ns.Name, element.Name, element.CppElementName);
                        foreach (var element in ns.Structs)
                            log.WriteLine(format, csv, "struct", ns.Name, element.Name, element.CppElementName);
                        foreach (var element in ns.Interfaces)
                            log.WriteLine(format, csv, "interface", ns.Name, element.Name, element.CppElementName);
                    }
                }
            }
        }

        /// <summary>
        ///   Maps all C++ types to C#
        /// </summary>
        /// <param name="cppModule">The C++ module to parse.</param>
        /// <param name="configFile">The config file to use to transform the C++ module into C# assemblies.</param>
        /// <param name="checkFilesPath">The path for the check files.</param>
        public (IEnumerable<CsAssembly> assemblies, IEnumerable<DefineExtensionRule> consumerExtensions) Transform(CppModule cppModule, ConfigFile configFile, string checkFilesPath)
        {
            var consumerDefines = Init(cppModule, configFile, checkFilesPath);
            var selectedCSharpType = new List<CsBase>();

            // Prepare transform by defining/registering all types to process
            selectedCSharpType.AddRange(PrepareTransform(cppModule, EnumTransform));
            selectedCSharpType.AddRange(PrepareTransform(cppModule, StructTransform));
            selectedCSharpType.AddRange(PrepareTransform(cppModule, InterfaceTransform));
            selectedCSharpType.AddRange(PrepareTransform<CppFunction, CsFunction>(cppModule, FunctionTransform));

            // Transform all types
            Logger.Progress(65, "Transforming enums...");
            ProcessTransform(EnumTransform, selectedCSharpType.OfType<CsEnum>());
            Logger.Progress(70, "Transforming structs...");
            ProcessTransform(StructTransform, selectedCSharpType.OfType<CsStruct>());
            Logger.Progress(75, "Transforming interfaces...");
            ProcessTransform(InterfaceTransform, selectedCSharpType.OfType<CsInterface>());
            Logger.Progress(80, "Transforming functions...");
            ProcessTransform(FunctionTransform, selectedCSharpType.OfType<CsFunction>());

            foreach (CsAssembly cSharpAssembly in assemblyManager.Assemblies)
                foreach (var ns in cSharpAssembly.Namespaces)
                {
                    // Sort items in this namespace
                    ns.Sort();

                    foreach (var cSharpFunctionGroup in ns.Classes)
                        constantManager.AttachConstants(cSharpFunctionGroup);
                }

            return (assemblyManager.Assemblies, consumerDefines);
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
            foreach (var cppInclude in cppModule.Includes.Where(cppInclude => _includesToProcess.Contains(cppInclude.Name)))
            {
                foreach (var cppItem in cppInclude.Iterate<TCppElement>())
                {
                    Logger.RunInContext(
                        cppItem.ToString(),
                        () =>
                        {
                            // If a struct is already mapped, it means that there is already a predefined mapping
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
        /// Creates the C# class container (used by functions, constants, variables).
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="namespaceName">Name of the namespace.</param>
        /// <param name="className">Name of the class.</param>
        /// <returns>The C# class container</returns>
        private CsClass CreateCsClassContainer(string assemblyName, string namespaceName, string className)
        {
            if (className == null) throw new ArgumentNullException(nameof(className));

            if (className.Contains("."))
            {
                namespaceName = Path.GetFileNameWithoutExtension(className);
                className = Path.GetExtension(className).Trim('.');
            }

            var csNameSpace = assemblyManager.GetOrCreateNamespace(assemblyName, namespaceName);

            foreach (var cSharpFunctionGroup in csNameSpace.Classes)
            {
                if (cSharpFunctionGroup.Name == className)
                    return cSharpFunctionGroup;
            }


            var group = new CsClass {Name = className};
            csNameSpace.Add(group);

            groupRegistry.RegisterGroup(namespaceName + "." + className, group);

            return group;
        }

        /// <summary>
        /// Handles the constant rule.
        /// </summary>
        /// <param name="constantRule">The constant rule.</param>
        private void HandleConstantRule(CppModule cppModule, ConstantRule constantRule, string nameSpace)
        {
            constantManager.AddConstantFromMacroToCSharpType(
                cppModule,
                constantRule.Macro ?? constantRule.Guid,
                constantRule.ClassName,
                constantRule.Type,
                constantRule.Name,
                constantRule.Value,
                constantRule.Visibility,
                nameSpace);
        }

        /// <summary>
        /// Maps a particular C++ include to a C# namespace.
        /// </summary>
        /// <param name="includeName">Name of the include.</param>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="nameSpace">The name space.</param>
        /// <param name="outputDirectory">The output directory.</param>
        private void MapIncludeToNamespace(string includeName, string assemblyName, string nameSpace, string outputDirectory = null)
        {
            AddIncludeToProcess(includeName);

            namespaceRegistry.MapIncludeToNamespace(includeName, assemblyName, nameSpace, outputDirectory);
        }

        /// <summary>
        /// Print statistics for this transform.
        /// </summary>
        public void PrintStatistics()
        {
            var globalStats = new Dictionary<string, int>();
            globalStats["interfaces"] = 0;
            globalStats["methods"] = 0;
            globalStats["parameters"] = 0;
            globalStats["enums"] = 0;
            globalStats["structs"] = 0;
            globalStats["fields"] = 0;
            globalStats["enumitems"] = 0;
            globalStats["functions"] = 0;
            
            foreach (var assembly in assemblyManager.Assemblies)
            {
                var stats = globalStats.ToDictionary(globalStat => globalStat.Key, globalStat => 0);

                foreach (var nameSpace in assembly.Items)
                {
                    // Enums, Structs, Interface, FunctionGroup
                    foreach (var item in nameSpace.Items)
                    {
                        if (item is CsInterface) stats["interfaces"]++;
                        else if (item is CsStruct) stats["structs"]++;
                        else if (item is CsEnum) stats["enums"]++;

                        foreach (var subitem in item.Items)
                        {
                            if (subitem is CsFunction)
                            {
                                stats["functions"]++;
                                stats["parameters"] += subitem.Items.Count;
                            }
                            else if (subitem is CsMethod)
                            {
                                stats["methods"]++;
                                stats["parameters"] += subitem.Items.Count;
                            }
                            else if (subitem is CsEnumItem)
                            {
                                stats["enumitems"]++;
                            }
                            else if (subitem is CsFieldBase)
                            {
                                stats["fields"]++;
                            }
                        }
                    }

                    foreach (var stat in stats) globalStats[stat.Key] += stat.Value;
                }

                Logger.Message("Assembly [{0}] Statistics", assembly.QualifiedName);
                foreach (var stat in stats)
                    Logger.Message("\tNumber of {0} : {1}", stat.Key, stat.Value);
            }
            Logger.Message("\n");

            Logger.Message("Global Statistics:");
            foreach (var stat in globalStats)
                Logger.Message("\tNumber of {0} : {1}", stat.Key, stat.Value);
        }

        public (IEnumerable<BindRule> bindings, IEnumerable<DefineExtensionRule> defines) GenerateTypeBindingsForConsumers()
        {
            return (from record in typeRegistry.GetTypeBindings()
                   select new BindRule(record.CppType, record.CSharpType.QualifiedName),
                   GenerateDefinesForMappedTypes());
        }

        private IEnumerable<DefineExtensionRule> GenerateDefinesForMappedTypes()
        {
            foreach (var mapping in typeRegistry.GetTypeBindings())
            {
                switch (mapping.CSharpType)
                {
                    case CsEnum csEnum:
                        yield return new DefineExtensionRule
                        {
                            Enum = csEnum.QualifiedName,
                            SizeOf = csEnum.SizeOf,
                            Align = csEnum.Align
                        };
                        break;
                    case CsClass csClass:
                        yield return new DefineExtensionRule
                        {
                            NewClass = csClass.QualifiedName,
                            SizeOf = csClass.SizeOf,
                            Align = csClass.Align
                        };
                        break;
                    case CsStruct csStruct:
                        yield return new DefineExtensionRule
                        {
                            Struct = csStruct.QualifiedName,
                            SizeOf = csStruct.SizeOf,
                            Align = csStruct.Align,
                            HasCustomMarshal = csStruct.HasCustomMarshal,
                            HasCustomNew = csStruct.HasCustomNew,
                            IsStaticMarshal = csStruct.IsStaticMarshal
                        };
                        break;
                    case CsInterface csInterface:
                        yield return new DefineExtensionRule
                        {
                            Interface = csInterface.QualifiedName,
                            SizeOf = csInterface.SizeOf,
                            Align = csInterface.Align
                        };
                        break;
                }
            }
        }
    }
}