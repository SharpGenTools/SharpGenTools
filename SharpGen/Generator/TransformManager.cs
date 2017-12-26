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

namespace SharpGen.Generator
{
    /// <summary>
    /// This class is responsible for generating the C# model from C++ model.
    /// </summary>
    public class TransformManager
    {
        private readonly List<string> _includesToProcess = new List<string>();

        private readonly IDocumentationAggregator docAggregator;
        private readonly TypeRegistry typeRegistry;
        private readonly NamespaceRegistry namespaceRegistry = new NamespaceRegistry();
        private readonly ConstantManager constantManager;
        private readonly GroupRegistry groupRegistry = new GroupRegistry();

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformManager"/> class.
        /// </summary>
        public TransformManager(
            GlobalNamespaceProvider globalNamespace,
            NamingRulesManager namingRules,
            Logger logger,
            TypeRegistry typeRegistry,
            IDocumentationAggregator docAggregator,
            ConstantManager constantManager)
        {
            GlobalNamespace = globalNamespace;
            Logger = logger;

            NamingRules = namingRules;
            Assemblies = new List<CsAssembly>();

            EnumTransform = new EnumTransform();
            EnumTransform.Init(this, logger);

            StructTransform = new StructTransform();
            StructTransform.Init(this, logger);

            MethodTransform = new MethodTransform();
            MethodTransform.Init(this, logger);

            InterfaceTransform = new InterfaceTransform();
            InterfaceTransform.Init(this, logger);

            GeneratedPath = @".\";

            this.docAggregator = docAggregator;
            this.typeRegistry = typeRegistry;
            this.constantManager = constantManager;
        }

        public bool ForceGenerator { get; set; }

        /// <summary>
        /// Gets the naming rules manager.
        /// </summary>
        /// <value>The naming rules manager.</value>
        public NamingRulesManager NamingRules { get; private set; }

        /// <summary>
        /// Gets the C++ module used by this instance.
        /// </summary>
        /// <value>The the C++ module used by this instance.</value>
        public CppModule CppModule { get; private set; }

        /// <summary>
        /// Gets assembly list that are processed.
        /// </summary>
        /// <value>The assembly list that are processed.</value>
        private List<CsAssembly> Assemblies { get; set; }

        /// <summary>
        /// Gets or sets the generated path.
        /// </summary>
        /// <value>The generated path.</value>
        public string GeneratedPath { get; set; }

        /// <summary>
        /// Gets or sets the app type for which to generate C# code.
        /// </summary>
        /// <value>The app type.</value>
        public string AppType { get; set; }

        /// <summary>
        /// Gets or sets the enum transformer.
        /// </summary>
        /// <value>The enum transformer.</value>
        internal EnumTransform EnumTransform { get; set; }

        /// <summary>
        /// Gets or sets the struct transformer.
        /// </summary>
        /// <value>The struct transformer.</value>
        internal StructTransform StructTransform { get; set; }

        /// <summary>
        /// Gets or sets the method transformer.
        /// </summary>
        /// <value>The method transformer.</value>
        internal MethodTransform MethodTransform { get; set; }

        /// <summary>
        /// Gets or sets the interface transformer.
        /// </summary>
        /// <value>The interface transformer.</value>
        internal InterfaceTransform InterfaceTransform { get; set; }

        /// <summary>
        /// Gets a list of include to process.
        /// </summary>
        /// <value>The include to process.</value>
        private IEnumerable<CppInclude> IncludeToProcess
        {
            get { return CppModule.Includes.Where(cppInclude => _includesToProcess.Contains(cppInclude.Name)); }
        }

        public GlobalNamespaceProvider GlobalNamespace { get; }
        public Logger Logger { get; }
        public bool IncludeAssemblyNameFolder { get; internal set; }
        
        /// <summary>
        /// Initializes this instance with the specified C++ module and config.
        /// </summary>
        /// <param name="cppModule">The C++ module.</param>
        /// <param name="config">The root config file.</param>
        /// <param name="checkFilesPath">The path to place check files in.</param>
        /// <returns>Any rules that must flow to consuming projects.</returns>
        public List<DefineExtensionRule> Init(CppModule cppModule, ConfigFile config, string checkFilesPath)
        {
            CppModule = cppModule;
            var configFiles = config.ConfigFilesLoaded;

            // Compute dependencies first
            // In order to calculate which assembly we need to process
            foreach (var configFile in configFiles)
                ComputeDependencies(configFile);

            // Check which assembly to update
            foreach (var assembly in Assemblies)
                CheckAssemblyUpdate(assembly, checkFilesPath);


            int numberOfConfigFilesToParse = 0;
            // If we don't need to process it, skip it silently
            foreach (var configFile in configFiles)
                if (configFile.IsMappingToProcess)
                    numberOfConfigFilesToParse++;

            var defines = new List<DefineExtensionRule>();
            int indexFile = 0;
            // Process each config file
            foreach (var configFile in configFiles)
            {
                if (configFile.IsMappingToProcess)
                {
                    Logger.Progress(30 + (indexFile*30)/numberOfConfigFilesToParse, "Processing mapping rules [{0}]", configFile.Assembly ?? configFile.Id);
                    defines.AddRange(ProcessConfigFile(configFile));
                    indexFile++;
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

            var assembly = GetOrCreateAssembly(file.Assembly);

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
        private IEnumerable<DefineExtensionRule> ProcessConfigFile(ConfigFile file)
        {
            Logger.PushLocation(file.AbsoluteFilePath);
            try
            {
                CsAssembly assembly = null;

                if (!string.IsNullOrEmpty(file.Assembly))
                    assembly = GetOrCreateAssembly(file.Assembly);

                if (assembly != null)
                    Logger.Message("Process rules for assembly [{0}] and namespace [{1}]", file.Assembly, file.Namespace);

                // Update Naming Rules
                UpdateNamingRules(file);

                // Only attach includes when there is a bind to an assembly
                if (assembly != null)
                {
                    AttachIncludes(file);

                    ProcessExtensions(file);
                }

                // Handle defines separately since they do not depend on the included C++
                // and they need to flow to consuming projects
                var defines = ProcessDefines(file);

                // Register bindings from <bindings> tag
                RegisterBindings(file);

                ProcessMappings(file);
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
                BindType(bindingRule.From, typeRegistry.ImportType(bindingRule.To),
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

        private void ProcessExtensions(ConfigFile file)
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
                    HandleConstantRule(constantRule, file.Namespace);
                }
                else if (extensionRule is ContextRule contextRule)
                {
                    HandleContextRule(file, contextRule);
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
                        AttachTypeToNamespace($"^{attachType}$", file.Assembly, includeRule.Namespace ?? file.Namespace, includeRule.Output);
                }
            }

            // Add extensions if any
            if (file.Extension.Count > 0)
                MapIncludeToNamespace(file.ExtensionId, file.Assembly, file.Namespace);
        }

        private void ProcessMappings(ConfigFile file)
        {
            // Perform all mappings from <mappings> tag
            foreach (var configRule in file.Mappings)
            {
                if (configRule is MappingRule mappingRule)
                {
                    if (mappingRule.Enum != null)
                        CppModule.Tag<CppEnum>(mappingRule.Enum, mappingRule);
                    else if (mappingRule.EnumItem != null)
                        CppModule.Tag<CppEnumItem>(mappingRule.EnumItem, mappingRule);
                    else if (mappingRule.Struct != null)
                        CppModule.Tag<CppStruct>(mappingRule.Struct, mappingRule);
                    else if (mappingRule.Field != null)
                        CppModule.Tag<CppField>(mappingRule.Field, mappingRule);
                    else if (mappingRule.Interface != null)
                        CppModule.Tag<CppInterface>(mappingRule.Interface, mappingRule);
                    else if (mappingRule.Function != null)
                        CppModule.Tag<CppFunction>(mappingRule.Function, mappingRule);
                    else if (mappingRule.Method != null)
                        CppModule.Tag<CppMethod>(mappingRule.Method, mappingRule);
                    else if (mappingRule.Parameter != null)
                        CppModule.Tag<CppParameter>(mappingRule.Parameter, mappingRule);
                    else if (mappingRule.Element != null)
                        CppModule.Tag<CppElement>(mappingRule.Element, mappingRule);
                    else if (mappingRule.DocItem != null)
                        docAggregator.AddDocLink(mappingRule.DocItem, mappingRule.MappingNameFinal);
                }
                else if (configRule is ContextRule contextRule)
                {
                    HandleContextRule(file, contextRule);
                }
                else if (configRule is RemoveRule removeRule)
                {
                    if (removeRule.Enum != null)
                        CppModule.Remove<CppEnum>(removeRule.Enum);
                    else if (removeRule.EnumItem != null)
                        CppModule.Remove<CppEnumItem>(removeRule.EnumItem);
                    else if (removeRule.Struct != null)
                        CppModule.Remove<CppStruct>(removeRule.Struct);
                    else if (removeRule.Field != null)
                        CppModule.Remove<CppField>(removeRule.Field);
                    else if (removeRule.Interface != null)
                        CppModule.Remove<CppInterface>(removeRule.Interface);
                    else if (removeRule.Function != null)
                        CppModule.Remove<CppFunction>(removeRule.Function);
                    else if (removeRule.Method != null)
                        CppModule.Remove<CppMethod>(removeRule.Method);
                    else if (removeRule.Parameter != null)
                        CppModule.Remove<CppParameter>(removeRule.Parameter);
                    else if (removeRule.Element != null)
                        CppModule.Remove<CppElement>(removeRule.Element);
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
        private void HandleContextRule(ConfigFile file, ContextRule contextRule)
        {
            if (contextRule is ClearContextRule)
                CppModule.ClearContextFind();
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

                CppModule.AddContextRangeFind(contextIds);
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

                foreach (var assembly in Assemblies)
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
        public IEnumerable<CsAssembly> Transform()
        {
            var selectedCSharpType = new List<CsBase>();

            // Prepare transform by defining/registering all types to process
            PrepareTransform(EnumTransform, selectedCSharpType);
            PrepareTransform(StructTransform, selectedCSharpType);
            PrepareTransform(InterfaceTransform, selectedCSharpType);
            PrepareTransform<CppFunction, CsFunction>(MethodTransform, selectedCSharpType);

            // Transform all types
            Logger.Progress(65, "Transforming enums...");
            ProcessTransform(EnumTransform, selectedCSharpType);
            Logger.Progress(70, "Transforming structs...");
            ProcessTransform(StructTransform, selectedCSharpType);
            Logger.Progress(75, "Transforming interfaces...");
            ProcessTransform(InterfaceTransform, selectedCSharpType);
            Logger.Progress(80, "Transforming functions...");
            ProcessTransform<CsFunction, CppFunction>(MethodTransform, selectedCSharpType);

            foreach (CsAssembly cSharpAssembly in Assemblies)
                foreach (var ns in cSharpAssembly.Namespaces)
                {
                    // Sort items in this namespace
                    ns.Sort();

                    foreach (var cSharpFunctionGroup in ns.Classes)
                        constantManager.AttachConstants(cSharpFunctionGroup);
                }

            return Assemblies;
        }

        /// <summary>
        /// Prepares a transformer from C++ to C# model.
        /// </summary>
        /// <typeparam name="TCppElement">The C++ type of data to process</typeparam>
        /// <param name="transform">The transform.</param>
        /// <param name="typeToProcess">The type to process.</param>
        private void PrepareTransform<TCppElement, TCsElement>(ITransform<TCsElement, TCppElement> transform, List<CsBase> typeToProcess)
            where TCppElement : CppElement
            where TCsElement : CsBase
        {
            // Predefine all structs, typedefs and interfaces
            foreach (var cppInclude in IncludeToProcess)
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
                                    typeToProcess.Add(csElement);
                            }
                        });
                }
            }
        }

        /// <summary>
        /// Processes a transformer from C++ to C# model.
        /// </summary>
        /// <typeparam name="T">The C++ type of data to process</typeparam>
        /// <param name="transform">The transform.</param>
        /// <param name="typeToProcess">The type to process.</param>
        private void ProcessTransform<T, TCppElement>(ITransform<T, TCppElement> transform, IEnumerable<CsBase> typeToProcess) where T : CsBase where TCppElement: CppElement
        {
            foreach (var csItem in typeToProcess.OfType<T>())
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
        /// Gets the C# type from a C++ type.
        /// </summary>
        /// <typeparam name="T">The C# type to return</typeparam>
        /// <param name="cppType">The C++ type to process.</param>
        /// <param name="isTypeUsedInStruct">if set to <c>true</c> this type is used in a struct declaration.</param>
        /// <returns>An instantiated C# type</returns>
        internal T CreateMarshalledElement<T>(CppType cppType, bool isTypeUsedInStruct = false) where T : CsMarshalBase, new()
        {
            CsTypeBase publicType = null;
            CsTypeBase marshalType = null;
            var interopType = new T
                                  {
                                      CppElement = cppType,
                                      IsArray = cppType.IsArray,
                                      ArrayDimension = cppType.ArrayDimension,
                                      // TODO: handle multidimension
                                      HasPointer = !string.IsNullOrEmpty(cppType.Pointer) && (cppType.Pointer.Contains("*") || cppType.Pointer.Contains("&")),
                                  };

            // Calculate ArrayDimension
            int arrayDimensionValue = 0;
            if (cppType.IsArray)
            {
                if (string.IsNullOrEmpty(cppType.ArrayDimension))
                    arrayDimensionValue = 0;
                else if (!int.TryParse(cppType.ArrayDimension, out arrayDimensionValue))
                    arrayDimensionValue = 1;
            }

            // If array Dimension is 0, then it is not an array
            if (arrayDimensionValue == 0)
            {
                cppType.IsArray = false;
                interopType.IsArray = false;
            }
            interopType.ArrayDimensionValue = arrayDimensionValue;

            string typeName = cppType.GetTypeNameWithMapping();

            switch (typeName)
            {
                case "char":
                    publicType = ImportType(typeof (byte));
                    if (interopType.HasPointer)
                        publicType = ImportType(typeof (string));
                    if (interopType.IsArray)
                    {
                        publicType = ImportType(typeof (string));
                        marshalType = ImportType(typeof (byte));
                    }
                    break;
                case "wchar_t":
                    publicType = ImportType(typeof (char));
                    interopType.IsWideChar = true;
                    if (interopType.HasPointer)
                        publicType = ImportType(typeof (string));
                    if (interopType.IsArray)
                    {
                        publicType = ImportType(typeof (string));
                        marshalType = ImportType(typeof (char));
                    }
                    break;
                default:

                    // If CppType is an array, try first to get the binding for this array
                    if (cppType.IsArray)
                        publicType = FindBoundType(typeName + "[" + cppType.ArrayDimension + "]");

                    // Else get the typeName
                    if (publicType == null)
                    {
                        // Try to get a declared struct
                        // If it fails, then this struct is unknown
                        publicType = FindBoundType(typeName);
                        if (publicType == null)
                        {
                            Logger.Fatal("Unknown type found [{0}]", typeName);
                        }
                    }
                    else
                    {
                        interopType.ArrayDimensionValue = 0;
                        interopType.IsArray = false;
                    }

                    // Get a MarshalType if any
                    marshalType = typeRegistry.FindBoundMarshalType(typeName);

                    if (publicType is CsStruct)
                    {
                        var referenceStruct = publicType as CsStruct;

                        // If a structure was not already parsed, then parsed it before going further
                        if (!referenceStruct.IsFullyMapped)
                        {
                            StructTransform.Process(referenceStruct);
                        }


                        // If referenced structure has a specialized marshalling, then specify marshalling
                        if (referenceStruct.HasMarshalType && !interopType.HasPointer)
                        {
                            marshalType = publicType;
                        }
                    }
                    else if (publicType is CsEnum)
                    {
                        var referenceEnum = publicType as CsEnum;
                        // Fixed array of enum should be mapped to their respective blittable type
                        if (interopType.IsArray)
                        {
                            marshalType = ImportType(referenceEnum.Type);
                        }
                    }
                    break;
            }

            // Set bool to int conversion case
            interopType.IsBoolToInt = marshalType != null && marshalType.Type == typeof (int) && publicType.Type == typeof (bool);

            // Default IntPtr type for pointer, unless modified by specialized type (like char* map to string)
            if (interopType.HasPointer)
            {
                if (isTypeUsedInStruct)
                {
                    publicType = ImportType(typeof (IntPtr));
                }
                else
                {
                    if (typeName == "void")
                        publicType = ImportType(typeof (IntPtr));

                    marshalType = ImportType(typeof (IntPtr));
                }

                switch (typeName)
                {
                    case "char":
                        publicType = ImportType(typeof (string));
                        marshalType = ImportType(typeof (IntPtr));
                        break;
                    case "wchar_t":
                        publicType = ImportType(typeof (string));
                        marshalType = ImportType(typeof (IntPtr));
                        interopType.IsWideChar = true;
                        break;
                }
            }
            else
            {
                if (isTypeUsedInStruct)
                {
                    // Special case for Size type, as it is default marshal to IntPtr for method parameter
                    if (publicType.QualifiedName == GlobalNamespace.GetTypeName("PointerSize"))
                        marshalType = null;
                }
            }

            interopType.PublicType = publicType;
            interopType.HasMarshalType = (marshalType != null || cppType.IsArray);
            if (marshalType == null)
                marshalType = publicType;
            interopType.MarshalType = marshalType;

            // Update the SizeOf according to the SizeOf MarshalType
            interopType.SizeOf = interopType.MarshalType.SizeOf * ((interopType.ArrayDimensionValue > 1) ? interopType.ArrayDimensionValue : 1);

            return interopType;
        }

        /// <summary>
        /// Resolves the namespace for a C++ element.
        /// </summary>
        /// <param name="element">The C++ element.</param>
        /// <returns>The attached namespace for this C++ element.</returns>
        internal CsNamespace ResolveNamespace(CppElement element)
        {
            var tag = element.GetTagOrDefault<MappingRule>();

            // If a type is redispatched to another namespace
            if (!string.IsNullOrEmpty(tag.Assembly) && !string.IsNullOrEmpty(tag.Namespace))
            {
                return GetOrCreateNamespace(tag.Assembly, tag.Namespace);
            }

            var namespaceFromElementName = namespaceRegistry.GetCsNamespaceForCppElement(element);
            if (namespaceFromElementName.match)
            {
                return namespaceFromElementName.nameSpace;
            }

            if (!namespaceRegistry.TryGetNamespaceForInclude(element.ParentInclude.Name, out CsNamespace ns))
            {
                Logger.Fatal("Unable to find namespace for element [{0}] from include [{1}]", element, element.ParentInclude.Name);
            }
            return ns;
        }

        /// <summary>
        /// Imports a defined C# type.
        /// </summary>
        /// <param name = "type">The C# type.</param>
        /// <returns>The C# type base</returns>
        public CsTypeBase ImportType(Type type)
        {
            return typeRegistry.ImportType(type.FullName);
        }

        /// <summary>
        /// Maps a C++ type name to a C# class
        /// </summary>
        /// <param name = "cppName">Name of the CPP.</param>
        /// <param name = "type">The C# type.</param>
        /// <param name = "marshalType">The C# marshal type</param>
        public void BindType(string cppName, CsTypeBase type, CsTypeBase marshalType = null)
        {
            typeRegistry.BindType(cppName, type, marshalType);
        }

        /// <summary>
        ///   Finds the C# type binded from a C++ type name.
        /// </summary>
        /// <param name = "cppName">Name of a c++ type</param>
        /// <returns>A C# type or null</returns>
        public CsTypeBase FindBoundType(string cppName)
        {
            return typeRegistry.FindBoundType(cppName);
        }

        public IEnumerable<string> GetDocItems(CsBase element) => docAggregator.GetDocItems(element);

        /// <summary>
        /// Gets a C# assembly by its name.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <returns>A C# assembly</returns>
        private CsAssembly GetOrCreateAssembly(string assemblyName)
        {
            var selectedAssembly = Assemblies.FirstOrDefault(assembly => assembly.Name == assemblyName);
            if (selectedAssembly == null)
            {
                selectedAssembly = new CsAssembly(assemblyName, AppType);
                selectedAssembly.RootDirectory = IncludeAssemblyNameFolder ? Path.Combine(GeneratedPath, selectedAssembly.Name) : GeneratedPath;
                Assemblies.Add(selectedAssembly);
            }

            return selectedAssembly;
        }

        /// <summary>
        /// Gets the C# namespace by its name and its assembly name.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="namespaceName">Name of the namespace.</param>
        /// <returns>A C# namespace</returns>
        private CsNamespace GetOrCreateNamespace(string assemblyName, string namespaceName)
        {
            if (assemblyName == null)
                assemblyName = namespaceName;

            var selectedAssembly = GetOrCreateAssembly(assemblyName);
            var selectedCsNamespace = selectedAssembly.Namespaces.FirstOrDefault(nameSpaceObject => nameSpaceObject.Name == namespaceName);
            if (selectedCsNamespace == null)
            {
                selectedCsNamespace = new CsNamespace(selectedAssembly, namespaceName);
                selectedAssembly.Add(selectedCsNamespace);
            }
            return selectedCsNamespace;
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

            var csNameSpace = GetOrCreateNamespace(assemblyName, namespaceName);

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
        /// Finds a C# class container by name.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <returns></returns>
        public CsClass FindCsClassContainer(string className) => groupRegistry.FindGroup(className);

        /// <summary>
        /// Handles the constant rule.
        /// </summary>
        /// <param name="constantRule">The constant rule.</param>
        private void HandleConstantRule(ConstantRule constantRule, string nameSpace)
        {
            constantManager.AddConstantFromMacroToCSharpType(
                CppModule,
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

            var cSharpNamespace = GetOrCreateNamespace(assemblyName, nameSpace);
            if (outputDirectory != null)
                cSharpNamespace.OutputDirectory = outputDirectory;
            namespaceRegistry.MapIncludeToNamespace(includeName, cSharpNamespace);
        }

        /// <summary>
        /// Attaches C++ to a C# namespace using a regular expression query.
        /// </summary>
        /// <param name="typeNameRegex">The C++ regex selection.</param>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="namespaceName">Name of the namespace.</param>
        /// <param name="outputDirectory">The output directory.</param>
        private void AttachTypeToNamespace(string typeNameRegex, string assemblyName, string namespaceName, string outputDirectory = null)
        {
            var cSharpNamespace = GetOrCreateNamespace(assemblyName, namespaceName);
            if (outputDirectory != null)
                cSharpNamespace.OutputDirectory = outputDirectory;
            namespaceRegistry.AttachTypeToNamespace(typeNameRegex, cSharpNamespace);
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

            Assemblies.Sort((left, right) => String.Compare(left.QualifiedName, right.QualifiedName, StringComparison.Ordinal));

            foreach (var assembly in Assemblies)
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