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
        private readonly Dictionary<string, List<CsVariable>> _mapConstantToCSharpType = new Dictionary<string, List<CsVariable>>();
        private readonly Dictionary<string, string> _docToCSharp = new Dictionary<string, string>();
        private readonly Dictionary<string, (CsTypeBase CSharpType, CsTypeBase MarshalType)> _mapCppNameToCSharpType = new Dictionary<string, (CsTypeBase CSharpType, CsTypeBase MarshalType)>();
        private readonly Dictionary<string, CsTypeBase> _mapDefinedCSharpType = new Dictionary<string, CsTypeBase>();

        private readonly Dictionary<string, CsNamespace> _mapIncludeToNamespace = new Dictionary<string, CsNamespace>();
        private readonly Dictionary<string, CsClass> _mapRegisteredFunctionGroup = new Dictionary<string, CsClass>();
        private readonly Dictionary<Regex, CsNamespace> _mapTypeToNamespace = new Dictionary<Regex, CsNamespace>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformManager"/> class.
        /// </summary>
        public TransformManager(GlobalNamespaceProvider globalNamespace, Logger logger)
        {
            GlobalNamespace = globalNamespace;
            Logger = logger;

            NamingRules = new NamingRulesManager();
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
        /// Gets assembly list that are processed. This is accessible after <see cref="Generate"/>
        /// method has been called.
        /// </summary>
        /// <value>The assembly list that are processed.</value>
        public List<CsAssembly> Assemblies { get; private set; }

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

        /// <summary>
        /// Gets or sets the name of the current assembly.
        /// </summary>
        /// <value>The name of the current assembly.</value>
        private string CurrentAssemblyName { get; set; }

        /// <summary>
        /// Gets or sets the name of the current namespace.
        /// </summary>
        /// <value>The name of the current namespace.</value>
        private string CurrentNamespaceName { get; set; }

        public GlobalNamespaceProvider GlobalNamespace { get; }
        public Logger Logger { get; }
        public string GeneratedCodeFolder { get; internal set; }
        public bool IncludeAssemblyNameFolder { get; internal set; }


        /// <summary>
        /// Use Roslyn to generate code.
        /// </summary>
        public bool UseRoslynCodeGen { get; set; }

        /// <summary>
        /// Initializes this instance with the specified C++ module and config.
        /// </summary>
        /// <param name="cppModule">The C++ module.</param>
        /// <param name="config">The root config file.</param>
        /// <param name="checkFilesPath">The path to place check files in.</param>
        /// <returns>Any rules that must flow to consuming projects.</returns>
        public List<ConfigBaseRule> Init(CppModule cppModule, ConfigFile config, string checkFilesPath)
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

            var defines = new List<ConfigBaseRule>();
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
                CurrentAssemblyName = file.Assembly;
                CurrentNamespaceName = file.Namespace;
                var defines = new List<DefineExtensionRule>();

                CsAssembly assembly = null;

                if (!string.IsNullOrEmpty(file.Assembly))
                    assembly = GetOrCreateAssembly(file.Assembly);

                if (assembly != null)
                    Logger.Message("Process rules for assembly [{0}] and namespace [{1}]", CurrentAssemblyName, CurrentNamespaceName);

                // Update Naming Rules
                foreach (var namingRule in file.Naming)
                {
                    if (namingRule is NamingRuleShort)
                        NamingRules.AddShortNameRule(namingRule.Name, namingRule.Value);
                }

                // Only attach includes when there is a bind to an assembly
                if (assembly != null)
                {
                    // Add all includes file
                    foreach (var includeRule in file.Includes)
                    {
                        if (includeRule.Attach.HasValue && includeRule.Attach.Value)
                        {
                            // include will be processed
                            MapIncludeToNamespace(includeRule.Id, null, includeRule.Namespace, includeRule.Output);
                        }
                        else
                        {
                            // include will be processed
                            if (includeRule.AttachTypes.Count > 0)
                                AddIncludeToProcess(includeRule.Id);

                            foreach (var attachType in includeRule.AttachTypes)
                                AttachTypeToNamespace("^" + attachType + "$", null, includeRule.Namespace, includeRule.Output);
                        }
                    }

                    // Add extensions if any
                    if (file.Extension.Count > 0)
                        MapIncludeToNamespace(file.ExtensionId);

                    // Register defined Types from <extension> tag
                    foreach (var extensionRule in file.Extension)
                    {
                        if (extensionRule is CreateExtensionRule createRule)
                        {
                            if (createRule.NewClass != null)
                            {
                                var functionGroup = CreateCsClassContainer(null, null, createRule.NewClass);
                                if (createRule.Visibility.HasValue)
                                    functionGroup.Visibility = createRule.Visibility.Value;
                            }
                            else
                                Logger.Error("Invalid rule [{0}]. Requires class", createRule);
                        }
                        else if (extensionRule is ConstantRule constantRule)
                        {
                            HandleConstantRule(constantRule);
                        }
                        else if (extensionRule is ContextRule contextRule)
                        {
                            HandleContextRule(file, contextRule);
                        }
                    }
                }

                // Handle defines separately since they do not depend on the included C++
                // and they need to flow to consuming projects
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
                    DefineType(defineType);
                    defines.Add(defineRule);
                }

                // Register bindings from <bindings> tag
                foreach (var bindingRule in file.Bindings)
                {
                    BindType(bindingRule.From, ImportType(bindingRule.To),
                             string.IsNullOrEmpty(bindingRule.Marshal) ? null : ImportType(bindingRule.Marshal));
                }

                ProcessMappings(file);
                return defines;
            }
            finally
            {
                Logger.PopLocation();
            }
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
                        AddDocLink(mappingRule.DocItem, mappingRule.MappingNameFinal);
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
        /// Generates the C# code.
        /// </summary>
        public void Generate(string checkFilesPath)
        {
            Transform();
            if (!UseRoslynCodeGen)
            {
                RunT4Generator();
            }
            else
            {
                RunRoslynGenerator();
            }
            // Update check files for all assemblies
            var processTime = DateTime.Now;
            foreach (CsAssembly assembly in Assemblies)
            {
                File.WriteAllText(Path.Combine(checkFilesPath, assembly.CheckFileName), "");
                File.SetLastWriteTime(Path.Combine(checkFilesPath, assembly.CheckFileName), processTime);
            }
        }

        private void RunRoslynGenerator()
        {
            throw new NotImplementedException();
        }

        private void RunT4Generator()
        {
            if (Logger.HasErrors)
                Logger.Fatal("Transform failed");

            // Configure TextTemplateEngine
            var engine = new TemplateEngine(Logger);
            engine.OnInclude += TextTemplatingCallback;
            engine.SetParameter("Generator", this);

            int indexToGenerate = 0;
            var templateNames = new[] { "Enumerations", "Structures", "Interfaces", "Functions", "LocalInterop" };

            var directoryToCreate = new HashSet<string>(StringComparer.CurrentCulture);

            // Iterates on templates
            foreach (string templateName in templateNames)
            {
                Logger.Progress(85 + (indexToGenerate * 15 / templateNames.Length), "Generating code for {0}...", templateName);
                indexToGenerate++;

                Logger.Message("\nGenerate {0}", templateName);
                string templateFileName = templateName + ".tt";

                string input = Utilities.GetResourceAsString("Templates." + templateFileName);

                // Iterates on assemblies
                foreach (var csAssembly in Assemblies)
                {
                    if (!csAssembly.IsToUpdate)
                        continue;

                    engine.SetParameter("Assembly", csAssembly);

                    string generatedDirectoryForAssembly = Path.Combine(csAssembly.RootDirectory, GeneratedCodeFolder ?? "Generated", AppType);

                    // Remove the generated directory before creating it
                    if (!directoryToCreate.Contains(generatedDirectoryForAssembly))
                    {
                        directoryToCreate.Add(generatedDirectoryForAssembly);
                        if (Directory.Exists(generatedDirectoryForAssembly))
                        {
                            foreach (var oldGeneratedFile in Directory.EnumerateFiles(generatedDirectoryForAssembly, "*.cs", SearchOption.AllDirectories))
                            {
                                try
                                {
                                    File.Delete(oldGeneratedFile);
                                }
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }

                    if (!Directory.Exists(generatedDirectoryForAssembly))
                        Directory.CreateDirectory(generatedDirectoryForAssembly);

                    Logger.Message("Process Assembly {0} => {1}", csAssembly.Name, generatedDirectoryForAssembly);

                    // LocalInterop is once generated per assembly
                    if (templateName == "LocalInterop")
                    {
                        Logger.Message("\tProcess Interop {0} => {1}", csAssembly.Name, generatedDirectoryForAssembly);

                        //Transform the text template.
                        string output = engine.ProcessTemplate(input, templateName);
                        string outputFileName = Path.GetFileNameWithoutExtension(templateFileName);

                        outputFileName = Path.Combine(generatedDirectoryForAssembly, outputFileName);
                        outputFileName = outputFileName + ".cs";
                        File.WriteAllText(outputFileName, output, Encoding.ASCII);
                    }
                    else
                    {
                        // Else, iterates on each namespace
                        foreach (var csNamespace in csAssembly.Namespaces)
                        {
                            engine.SetParameter("Namespace", csNamespace);

                            string subDirectory = csNamespace.OutputDirectory ?? ".";

                            string nameSpaceDirectory = generatedDirectoryForAssembly + "\\" + subDirectory;
                            if (!Directory.Exists(nameSpaceDirectory))
                                Directory.CreateDirectory(nameSpaceDirectory);

                            Logger.Message("\tProcess Namespace {0} => {1}", csNamespace.Name, nameSpaceDirectory);

                            //Transform the text template.
                            string output = engine.ProcessTemplate(input, templateName);
                            string outputFileName = Path.GetFileNameWithoutExtension(templateFileName);

                            outputFileName = Path.Combine(nameSpaceDirectory, outputFileName);
                            outputFileName = outputFileName + ".cs";
                            File.WriteAllText(outputFileName, output, Encoding.ASCII);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Callback used by the text templating engine.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private static void TextTemplatingCallback(object sender, TemplateIncludeArgs e)
        {
            e.Text = Utilities.GetResourceAsString("Templates." + e.IncludeName);
        }

        /// <summary>
        ///   Maps all C++ types to C#
        /// </summary>
        private void Transform()
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
                        AttachConstants(cSharpFunctionGroup);
                }
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
                    Logger.RunInContext(cppItem.ToString(), () =>
                                                                {
                                                                    // If a struct is already mapped, it means that there is already a predefined mapping
                                                                    if (FindBindType(cppItem.Name) == null)
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
                Logger.RunInContext(csItem.CppElement.ToString(), () =>
                                                                      {
                                                                          transform.Process(csItem);
                                                                          AttachConstants(csItem);
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
        internal T GetCsType<T>(CppType cppType, bool isTypeUsedInStruct = false) where T : CsMarshalBase, new()
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
                        publicType = FindBindType(typeName + "[" + cppType.ArrayDimension + "]");

                    // Else get the typeName
                    if (publicType == null)
                    {
                        // Try to get a declared struct
                        // If it fails, then this struct is unknown
                        publicType = FindBindType(typeName);
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
                    marshalType = FindBindMarshalType(typeName);

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
            interopType.SizeOf = interopType.MarshalType.SizeOf*((interopType.ArrayDimensionValue > 1) ? interopType.ArrayDimensionValue : 1);

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

            foreach (var regExp in _mapTypeToNamespace)
            {
                if (regExp.Key.Match(element.Name).Success)
                    return regExp.Value;
            }

            if (!_mapIncludeToNamespace.TryGetValue(element.ParentInclude.Name, out CsNamespace ns))
            {
                Logger.Fatal("Unable to find namespace for element [{0}] from include [{1}]", element, element.ParentInclude.Name);
            }
            return ns;
        }

        /// <summary>
        /// Defines and register a C# type.
        /// </summary>
        /// <param name = "type">The C# type.</param>
        public void DefineType(CsTypeBase type)
        {
            var qualifiedName = type.QualifiedName;
            if (!_mapDefinedCSharpType.ContainsKey(qualifiedName))
                _mapDefinedCSharpType.Add(qualifiedName, type);
        }

        /// <summary>
        /// Gets a C# type is registered.
        /// </summary>
        /// <param name = "type">The C# type.</param>
        public CsTypeBase GetTypeDefined(CsTypeBase type)
        {
            _mapDefinedCSharpType.TryGetValue(type.QualifiedName, out CsTypeBase outType);
            return outType;
        }

        /// <summary>
        /// Tests if a a C# type is registered.
        /// </summary>
        /// <param name = "type">The C# type.</param>
        public bool IsTypeDefined(CsTypeBase type)
        {
            return GetTypeDefined(type) != null;
        }

        /// <summary>
        /// Removes a C# type.
        /// </summary>
        /// <param name = "type">The C# type.</param>
        public bool RemoveDefineType(CsTypeBase type)
        {
            return _mapDefinedCSharpType.Remove(type.QualifiedName);
        }

        /// <summary>
        /// Imports a defined C# type.
        /// </summary>
        /// <param name = "type">The C# type.</param>
        /// <returns>The C# type base</returns>
        public CsTypeBase ImportType(Type type)
        {
            return ImportType(type.FullName);
        }

        /// <summary>
        /// Imports a defined C# type by name.
        /// </summary>
        /// <param name = "typeName">Name of the C# type.</param>
        /// <returns>The C# type base</returns>
        public CsTypeBase ImportType(string typeName)
        {
            if (!_mapDefinedCSharpType.TryGetValue(typeName, out CsTypeBase cSharpType))
            {
                var type = Type.GetType(typeName);
                var sizeOf = 0;
                if (type == null)
                    Logger.Warning("Type [{0}] is not defined", typeName);
                else
                {
                    try
                    {
#pragma warning disable 0618
                        sizeOf = Marshal.SizeOf(type);
#pragma warning restore 0618
                    }
                    catch (Exception)
                    {
                        Logger.Message($"Tried to get the size of type {typeName}, which is not a struct.");
                    }
                }
                cSharpType = new CsTypeBase {Name = typeName, Type = type, SizeOf = sizeOf};
                DefineType(cSharpType);
            }
            return cSharpType;
        }

        /// <summary>
        /// Adds a doc link.
        /// </summary>
        /// <param name="cppName">Name of the CPP.</param>
        /// <param name="cSharpName">Name of the c sharp.</param>
        public void AddDocLink(string cppName, string cSharpName)
        {
            if (!_docToCSharp.ContainsKey(cppName))
                _docToCSharp.Add(cppName, cSharpName);
        }

        /// <summary>
        /// Maps a C++ type name to a C# class
        /// </summary>
        /// <param name = "cppName">Name of the CPP.</param>
        /// <param name = "type">The C# type.</param>
        /// <param name = "marshalType">The C# marshal type</param>
        public void BindType(string cppName, CsTypeBase type, CsTypeBase marshalType = null)
        {
            // Check for type replacer
            if (type.CppElement != null)
            {
                var tag = type.CppElement.GetTagOrDefault<MappingRule>();
                if (tag.Replace != null)
                {
                    Logger.Warning("Replace type {0} -> {1}", cppName, tag.Replace);

                    // Remove old type from namespace if any
                    var oldType = FindBindType(tag.Replace);
                    if (oldType != null)
                    {
                        if (oldType.Parent != null)
                            oldType.Parent.Remove(oldType);
                    }

                    _mapCppNameToCSharpType.Remove(tag.Replace);

                    // Replace the name
                    cppName = tag.Replace;
                }
            }

            if (_mapCppNameToCSharpType.ContainsKey(cppName))
            {
                var old = _mapCppNameToCSharpType[cppName];
                Logger.Error("Mapping C++ element [{0}] to CSharp type [{1}/{2}] is already mapped to [{3}/{4}]", cppName, type.CppElementName,
                             type.QualifiedName, old.CSharpType.CppElementName, old.CSharpType.QualifiedName);
            }
            else
            {
                _mapCppNameToCSharpType.Add(cppName, new ValueTuple<CsTypeBase, CsTypeBase>(type, marshalType));
            }
        }

        /// <summary>
        ///   Finds the C# type binded from a C++ type name.
        /// </summary>
        /// <param name = "cppName">Name of a c++ type</param>
        /// <returns>A C# type or null</returns>
        public CsTypeBase FindBindType(string cppName)
        {
            if (cppName == null)
                return null;
            _mapCppNameToCSharpType.TryGetValue(cppName, out var typeMap);
            return typeMap.CSharpType;
        }

        /// <summary>
        ///   Finds the C# full name from a C++ name.
        /// </summary>
        /// <param name = "cppName">Name of a c++ type</param>
        /// <returns>Name of the C# type</returns>
        public string FindDocName(string cppName)
        {
            if (_docToCSharp.TryGetValue(cppName, out string cSharpName))
                return cSharpName;

            var cSharpType = FindBindType(cppName);
            if (cSharpType != null)
                return cSharpType.QualifiedName;

            return null;
        }

        private static readonly Regex RegexLinkStart = new Regex(@"^\s*\{\{.*?}}\s*(.*)", RegexOptions.Compiled);
        private static readonly Regex RegexLink = new Regex(@"\{\{(.*?)}}", RegexOptions.Compiled);
        private static readonly Regex RegexSpaceBegin = new Regex(@"^\s*(.*)", RegexOptions.Compiled);

        public List<string> GetDocItems(CsBase element)
        {
            var docItems = new List<string>();

            // If doc comments are already stored in an external file, than don't emit them
            if (!element.IsCodeCommentsExternal)
            {
                var description = element.Description;
                var remarks = element.Remarks;

                description = RegexSpaceBegin.Replace(description, "$1");

                description = RegexLink.Replace(description, RegexReplaceCReference);
                // evaluator => "<see cref=\"$1\"/>");

                string line = null;

                var stringReader = new StringReader(description);
                docItems.Add("<summary>");
                while ((line = stringReader.ReadLine()) != null)
                    docItems.Add(line);
                docItems.Add("</summary>");

                element.FillDocItems(docItems, this);

                if (!string.IsNullOrEmpty(remarks))
                {
                    remarks = RegexSpaceBegin.Replace(remarks, "$1");
                    remarks = RegexLink.Replace(remarks, RegexReplaceCReference);

                    stringReader = new StringReader(remarks);
                    docItems.Add("<remarks>");
                    while ((line = stringReader.ReadLine()) != null)
                        docItems.Add(line);
                    docItems.Add("</remarks>");
                }
            }

            docItems.Add(element.DocIncludeDirective);
            if (element.CppElement != null)
            {
                if (element.DocId != null)
                {
                    docItems.Add("<msdn-id>" + Utilities.EscapeXml(element.DocId) + "</msdn-id>");
                }
                docItems.Add("<unmanaged>" + Utilities.EscapeXml(element.DocUnmanagedName) + "</unmanaged>");
                docItems.Add("<unmanaged-short>" + Utilities.EscapeXml(element.DocUnmanagedShortName) + "</unmanaged-short>");
            }

            return docItems;
        }

        /// <summary>
        /// Gets the description as a single line of documentation.
        /// </summary>
        /// <value>The single doc.</value>
        public string GetSingleDoc(CsBase element)
        {
            string description = element.Description;

            if (RegexLinkStart.Match(description).Success)
                description = RegexLinkStart.Replace(description, "$1");

            description = RegexSpaceBegin.Replace(description, "$1");

            description = RegexLink.Replace(description, RegexReplaceCReference);

            StringBuilder docItems = new StringBuilder();
            string line = null;

            StringReader stringReader = new StringReader(description);
            while ((line = stringReader.ReadLine()) != null)
                docItems.Append(line);

            return docItems.ToString();
        }


        private static Regex regextWithMethodW = new Regex("([^W])::");
        private static Regex regexWithTypeW = new Regex("([^W])$");

        private string RegexReplaceCReference(Match match)
        {
            string matchName = match.Groups[1].Value;
            var csName = FindDocName(matchName);

            // Tries to match with W::
            if (csName == null && regextWithMethodW.Match(matchName).Success)
                csName = FindDocName(regextWithMethodW.Replace(matchName, "$1W::"));

            // Or with W
            if (csName == null && regexWithTypeW.Match(matchName).Success)
                csName = FindDocName(regexWithTypeW.Replace(matchName, "$1W"));

            if (csName == null)
                return matchName;

            if (csName.StartsWith("<"))
                return csName;
            return string.Format(CultureInfo.InvariantCulture, "<see cref=\"{0}\"/>", csName);
        }

        /// <summary>
        ///   Finds the C# marshal type binded from a C++ typename.
        /// </summary>
        /// <param name = "cppName">Name of a c++ type</param>
        /// <returns>A C# type or null</returns>
        public CsTypeBase FindBindMarshalType(string cppName)
        {
            if (cppName == null)
                return null;
            _mapCppNameToCSharpType.TryGetValue(cppName, out var typeMap);
            return typeMap.MarshalType;
        }

        /// <summary>
        /// Gets a C# assembly by its name.
        /// </summary>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <returns>A C# assembly</returns>
        private CsAssembly GetOrCreateAssembly(string assemblyName)
        {
            var selectedAssembly = (from assembly in Assemblies
                                    where assembly.Name == assemblyName
                                    select assembly).FirstOrDefault();
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
            var selectedCsNamespace = (from nameSpaceObject in selectedAssembly.Namespaces
                                       where nameSpaceObject.Name == namespaceName
                                       select nameSpaceObject).FirstOrDefault();
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
        private CsClass CreateCsClassContainer(string assemblyName = null, string namespaceName = null, string className = null)
        {
            if (className == null) throw new ArgumentNullException(nameof(className));

            if (className.Contains("."))
            {
                namespaceName = Path.GetFileNameWithoutExtension(className);
                className = Path.GetExtension(className).Trim('.');
            }

            assemblyName = assemblyName ?? CurrentAssemblyName;
            namespaceName = namespaceName ?? CurrentNamespaceName;

            var csNameSpace = GetOrCreateNamespace(assemblyName, namespaceName);

            foreach (var cSharpFunctionGroup in csNameSpace.Classes)
            {
                if (cSharpFunctionGroup.Name == className)
                    return cSharpFunctionGroup;
            }


            var group = new CsClass {Name = className};
            csNameSpace.Add(group);

            _mapRegisteredFunctionGroup.Add(namespaceName + "." + className, group);

            return group;
        }

        /// <summary>
        /// Finds a C# class container by name.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <returns></returns>
        public CsClass FindCsClassContainer(string className)
        {
            _mapRegisteredFunctionGroup.TryGetValue(className, out CsClass csClass);
            return csClass;
        }

        /// <summary>
        /// Handles the constant rule.
        /// </summary>
        /// <param name="constantRule">The constant rule.</param>
        private void HandleConstantRule(ConstantRule constantRule)
        {
            AddConstantFromMacroToCSharpType(constantRule.Macro ?? constantRule.Guid, constantRule.ClassName, constantRule.Type, constantRule.Name, constantRule.Value, constantRule.Visibility);
        }

        /// <summary>
        /// Adds a list of constant gathered from macros/guids to a C# type.
        /// </summary>
        /// <param name="macroRegexp">The macro regexp.</param>
        /// <param name="fullNameCSharpType">Full type of the name C sharp.</param>
        /// <param name="type">The type.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="valueMap">The value map.</param>
        /// <param name="visibility">The visibility.</param>
        private void AddConstantFromMacroToCSharpType(string macroRegexp, string fullNameCSharpType, string type, string fieldName = null, string valueMap = null,
                                                     Visibility? visibility = null)
        {
            var constantDefinitions = CppModule.Find<CppConstant>(macroRegexp);
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
                string finalFieldName = fieldName == null ? macroDef.Name : NamingRules.ConvertToPascalCase(regex.Replace(macroDef.Name, fieldName), NamingFlags.Default);
                string finalValue = valueMap == null ? macroDef.Value : string.Format(valueMap, macroDef.Name, macroDef.Value, finalFieldName, CurrentNamespaceName);
                AddConstantToCSharpType(macroDef, fullNameCSharpType, type, finalFieldName, finalValue).Visibility = visibility ?? Visibility.Public | Visibility.Const;
            }

            var guidDefinitions = CppModule.Find<CppGuid>(macroRegexp);
            foreach (var guidDef in guidDefinitions)
            {
                string finalFieldName = fieldName == null ? guidDef.Name : NamingRules.ConvertToPascalCase(regex.Replace(guidDef.Name, fieldName), NamingFlags.Default);
                string finalValue = valueMap == null ? guidDef.Guid.ToString() : string.Format(valueMap, guidDef.Name, guidDef.Guid.ToString(), finalFieldName, CurrentNamespaceName);
                AddConstantToCSharpType(guidDef, fullNameCSharpType, type, finalFieldName, finalValue).Visibility = visibility ?? Visibility.Public | Visibility.Static |
                                                                                                                          Visibility.Readonly;
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

            BindType(cppElement.Name, constantToAdd);

            return constantToAdd;
        }

        /// <summary>
        /// Tries to attach declared constants to this C# type.
        /// </summary>
        /// <param name="csType">The C# type</param>
        private void AttachConstants(CsBase csType)
        {
            foreach (var innerElement in csType.Items)
                AttachConstants(innerElement);

            foreach (KeyValuePair<string, List<CsVariable>> keyValuePair in _mapConstantToCSharpType)
            {
                if (csType.QualifiedName == keyValuePair.Key)
                {
                    foreach (var constantDef in keyValuePair.Value)
                        csType.Add(constantDef);
                }
            }
        }

        /// <summary>
        /// Maps a particular C++ include to a C# namespace.
        /// </summary>
        /// <param name="includeName">Name of the include.</param>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="nameSpace">The name space.</param>
        /// <param name="outputDirectory">The output directory.</param>
        private void MapIncludeToNamespace(string includeName, string assemblyName = null, string nameSpace = null, string outputDirectory = null)
        {
            AddIncludeToProcess(includeName);

            assemblyName = assemblyName ?? CurrentAssemblyName;
            nameSpace = nameSpace ?? CurrentNamespaceName;

            var cSharpNamespace = GetOrCreateNamespace(assemblyName, nameSpace);
            if (outputDirectory != null)
                cSharpNamespace.OutputDirectory = outputDirectory;
            _mapIncludeToNamespace.Add(includeName, cSharpNamespace);
        }

        /// <summary>
        /// Attaches C++ to a C# namespace using a regular expression query.
        /// </summary>
        /// <param name="typeNameRegex">The C++ regex selection.</param>
        /// <param name="assemblyName">Name of the assembly.</param>
        /// <param name="namespaceName">Name of the namespace.</param>
        /// <param name="outputDirectory">The output directory.</param>
        private void AttachTypeToNamespace(string typeNameRegex, string assemblyName = null, string namespaceName = null, string outputDirectory = null)
        {
            assemblyName = assemblyName ?? CurrentAssemblyName;
            namespaceName = namespaceName ?? CurrentNamespaceName;
            var cSharpNamespace = GetOrCreateNamespace(assemblyName, namespaceName);
            if (outputDirectory != null)
                cSharpNamespace.OutputDirectory = outputDirectory;
            _mapTypeToNamespace.Add(new Regex(typeNameRegex), cSharpNamespace);
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
            return (from record in _mapCppNameToCSharpType
                   where !(record.Value.CSharpType is CsMethod)
                   && !(record.Value.CSharpType is CsEnumItem)
                   select new BindRule(record.Key, record.Value.CSharpType.QualifiedName),
                   GenerateDefinesForMappedTypes());
        }

        private IEnumerable<DefineExtensionRule> GenerateDefinesForMappedTypes()
        {
            foreach (var mapping in _mapCppNameToCSharpType)
            {
                switch (mapping.Value.CSharpType)
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