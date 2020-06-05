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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using SharpGen.Logging;
using System.Reflection;

namespace SharpGen.Config
{
    /// <summary>
    /// Config File.
    /// </summary>
    [XmlRoot("config", Namespace=XmlNamespace)]
    public partial class ConfigFile
    {
        internal const string XmlNamespace = "urn:SharpGen.Config";

        /// <summary>
        /// Gets dynamic variables used by dynamic variable substitution #(MyVariable)
        /// </summary>
        /// <value>The dynamic variables.</value>
        [XmlIgnore]
        public Dictionary<string, string> DynamicVariables { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the parent of this mapping file.
        /// </summary>
        /// <value>The parent.</value>
        [XmlIgnore]
        public ConfigFile Parent { get; set; }

        /// <summary>
        /// Gets or sets the path of this MappingFile. If not null, used when saving this file.
        /// </summary>
        /// <value>The path.</value>
        [XmlIgnore]
        public string FilePath { get; set; }

        [XmlIgnore]
        public string AbsoluteFilePath
        {
            get
            {
                if (FilePath == null)
                    return null;
                if (Path.IsPathRooted(FilePath))
                    return FilePath;
                if (Parent?.AbsoluteFilePath != null)
                    return Path.Combine(Path.GetDirectoryName(Parent.AbsoluteFilePath), FilePath);
                return Path.GetFullPath(Path.Combine(".", FilePath));
            }
        }

        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("depends")]
        public List<string> Depends { get; set; } = new List<string>();

        [XmlElement("namespace")]
        public string Namespace { get; set; }

        [XmlElement("assembly")]
        public string Assembly
        {
            // The assembly attribute is no longer supported, but we leave the option
            // so that config files don't break when the option is specified.
            get => string.Empty;
            set { }
        }

        [XmlElement("var")]
        public List<KeyValue> Variables { get; set; } = new List<KeyValue>();

        [XmlElement("file")]
        public List<string> Files { get; set; } = new List<string>();

        [XmlIgnore]
        public List<ConfigFile> References { get; set; } = new List<ConfigFile>();

        [XmlElement("sdk")]
        public List<SdkRule> Sdks { get; set; } = new List<SdkRule>();

        [XmlElement("include-dir")]
        public List<IncludeDirRule> IncludeDirs { get; set; } = new List<IncludeDirRule>();

        [XmlElement("include-prolog")]
        public List<string> IncludeProlog { get; set; } = new List<string>();

        [XmlElement("include")]
        public List<IncludeRule> Includes { get; set; } = new List<IncludeRule>();

        [XmlArray("naming"),XmlArrayItem(typeof(NamingRuleShort))]
        public List<NamingRule> Naming { get; set; } = new List<NamingRule>();

        [XmlElement("context-set")]
        public List<ContextSetRule> ContextSets { get; set; }

        [XmlArray("extension")]
        [XmlArrayItem(typeof(ContextRule))]
        [XmlArrayItem(typeof(ClearContextRule))]
        [XmlArrayItem(typeof(CreateExtensionRule))]
        [XmlArrayItem(typeof(CreateCppExtensionRule))]
        [XmlArrayItem(typeof(DefineExtensionRule))]
        [XmlArrayItem(typeof(ConstantRule))]
        public List<ExtensionBaseRule> Extension { get; set; } = new List<ExtensionBaseRule>();

        [XmlIgnore]
        public string ExtensionId => Id + "-ext";

        /// <summary>
        /// Gets the name of the extension header file.
        /// </summary>
        /// <value>The name of the extension header file.</value>
        [XmlIgnore]
        public string ExtensionFileName => ExtensionId + ".h";

        /// <summary>
        /// Gets the name of this configs' primary header file.
        /// </summary>
        [XmlIgnore]
        public string HeaderFileName => Id + ".h";

        [XmlArray("bindings")]
        public List<BindRule> Bindings { get; set; } = new List<BindRule>();

        [XmlArray("mapping")]
        [XmlArrayItem(typeof(MappingRule))]
        [XmlArrayItem(typeof(MoveRule))]
        [XmlArrayItem(typeof(RemoveRule))]
        [XmlArrayItem(typeof(ContextRule))]
        [XmlArrayItem(typeof(ClearContextRule))]
        public List<ConfigBaseRule> Mappings { get; set; } = new List<ConfigBaseRule>();

        /// <summary>
        /// Finds all dependencies ConfigFile from this instance.
        /// </summary>
        /// <param name="dependencyListOutput">The dependencies list to fill.</param>
        public void FindAllDependencies(List<ConfigFile> dependencyListOutput)
        {
            foreach (var dependConfigFileId in Depends)
            {
                var linkedConfig = GetRoot().mapIdToFile[dependConfigFileId];
                if (!dependencyListOutput.Contains(linkedConfig))
                    dependencyListOutput.Add(linkedConfig);

                linkedConfig.FindAllDependencies(dependencyListOutput);
            }
        }

        /// <summary>
        /// Finds a context set by Id.
        /// </summary>
        /// <param name="contextSetId">The context set id.</param>
        /// <returns></returns>
        public ContextSetRule FindContextSetById(string contextSetId)
        {
            return ContextSets?.FirstOrDefault(contextSetRule => contextSetRule.Id == contextSetId);
        }

        /// <summary>
        /// Expands all dynamic variables used inside Bindings and Mappings tags.
        /// </summary>
        /// <param name="expandDynamicVariable">if set to <c>true</c> expand dynamic variables.</param>
        public void ExpandVariables(bool expandDynamicVariable, Logger logger)
        {
            ExpandVariables(Variables, expandDynamicVariable, logger);
            ExpandVariables(Includes, expandDynamicVariable, logger);
            ExpandVariables(IncludeDirs, expandDynamicVariable, logger);
            ExpandVariables(Bindings, expandDynamicVariable, logger);
            ExpandVariables(Mappings, expandDynamicVariable, logger);
            // Do it recursively
            foreach (var configFile in References)
                configFile.ExpandVariables(expandDynamicVariable, logger);
        }

        /// <summary>
        /// Iterate on all objects and sub-objects to expand dynamic variable
        /// </summary>
        /// <param name="objectToExpand">The object to expand.</param>
        /// <returns>the expanded object</returns>
        private object ExpandVariables(object objectToExpand, bool expandDynamicVariable, Logger logger)
        {
            if (objectToExpand == null)
                return null;
            if (objectToExpand is string str)
                return ExpandString(str, expandDynamicVariable, logger);
            if (objectToExpand.GetType().GetTypeInfo().IsPrimitive)
                return objectToExpand;
            if (objectToExpand is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                    list[i] = ExpandVariables(list[i], expandDynamicVariable, logger);
                return list;
            }
            foreach (var propertyInfo in objectToExpand.GetType().GetRuntimeProperties())
            {
                if (!propertyInfo.GetCustomAttributes<XmlIgnoreAttribute>(false).Any())
                {
                    // Check that this field is "ShouldSerializable"
                    var method = objectToExpand.GetType().GetRuntimeMethod("ShouldSerialize" + propertyInfo.Name, Type.EmptyTypes);
                    if (method != null && !((bool)method.Invoke(objectToExpand, null)))
                        continue;

                    propertyInfo.SetValue(objectToExpand, ExpandVariables(propertyInfo.GetValue(objectToExpand, null), expandDynamicVariable, logger), null);
                }
            }
            return objectToExpand;
        }


        /// <summary>
        /// Gets a variable value. Value is expanded if it contains any reference to other variables.
        /// </summary>
        /// <param name="variableName">Name of the variable.</param>
        /// <returns>the value of this variable</returns>
        private string GetVariable(string variableName, Logger logger)
        {
            foreach (var keyValue in Variables)
            {
                if (keyValue.Name == variableName)
                    return ExpandString(keyValue.Value, false, logger);
            }

            return Parent?.GetVariable(variableName, logger);
        }

        /// <summary>
        /// Regex used to expand variable
        /// </summary>
        static readonly Regex ReplaceVariableRegex = new Regex(@"\$\(([a-zA-Z_][\w_]*)\)", RegexOptions.Compiled);
        static readonly Regex ReplaceDynamicVariableRegex = new Regex(@"#\(([a-zA-Z_][\w_]*)\)", RegexOptions.Compiled);

        /// <summary>
        /// Expands a string using environment variable and variables defined in mapping files.
        /// </summary>
        /// <param name="str">The string to expand.</param>
        /// <returns>the expanded string</returns>
        public string ExpandString(string str, bool expandDynamicVariable, Logger logger)
        {
            var result = str;

            // Perform Config Variable substitution
            if (ReplaceVariableRegex.Match(result).Success)
            {
                result = ReplaceVariableRegex.Replace(
                    result,
                    match =>
                    {
                        string name = match.Groups[1].Value;
                        string localResult = GetVariable(name, logger);
                        if (localResult == null)
                            localResult = Environment.GetEnvironmentVariable(name);
                        if (localResult == null)
                        {
                            logger.Error(LoggingCodes.UnkownVariable, "Unable to substitute config/environment variable $({0}). Variable is not defined", name);
                            return "";
                        }
                        return localResult;
                    });
            }

            // Perform Dynamic Variable substitution
            if (expandDynamicVariable && ReplaceDynamicVariableRegex.Match(result).Success)
            {
                result = ReplaceDynamicVariableRegex.Replace(
                    result,
                    match =>
                    {
                        string name = match.Groups[1].Value;
                        string localResult;
                        if (!GetRoot().DynamicVariables.TryGetValue(name, out localResult))
                        {
                            logger.Error(LoggingCodes.UnkownDynamicVariable, "Unable to substitute dynamic variable #({0}). Variable is not defined", name);
                            return "";
                        }
                        localResult = localResult.Trim('"');
                        return localResult;
                    });
            }           
            return result;
        }

        private void PostLoad(ConfigFile parent, string file, string[] macros, IEnumerable<KeyValue> variables, Logger logger)
        {
            FilePath = file;
            Parent = parent;

            if (AbsoluteFilePath != null)
            {
                Variables.Add(new KeyValue("THIS_CONFIG_PATH", Path.GetDirectoryName(AbsoluteFilePath)));
            }

            Variables.AddRange(variables);

            // Load all dependencies
            foreach (var dependFile in Files)
            {
                var dependFilePath = ExpandString(dependFile, false, logger);
                if (!Path.IsPathRooted(dependFilePath) && AbsoluteFilePath != null)
                    dependFilePath = Path.Combine(Path.GetDirectoryName(AbsoluteFilePath), dependFilePath);
                
                var subMapping = Load(this, dependFilePath, macros, variables, logger);
                if (subMapping != null)
                {
                    subMapping.FilePath = dependFile;
                    References.Add(subMapping);
                }
            }

            // Clear all depends file
            Files.Clear();

            // Add this mapping file
            GetRoot().mapIdToFile.Add(Id, this);            
        }

        public IReadOnlyCollection<ConfigFile> ConfigFilesLoaded => GetRoot().mapIdToFile.Values;

        /// <summary>
        /// Loads the specified config file attached to a parent config file.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="file">The file.</param>
        /// <returns>The loaded config</returns>
        private static ConfigFile Load(ConfigFile parent, string file, string[] macros, IEnumerable<KeyValue> variables, Logger logger)
        {
            if(!File.Exists(file))
            {
                logger.Error(LoggingCodes.ConfigNotFound, "Configuration file {0} not found.", file);
                return null;
            }
            
            var deserializer = new XmlSerializer(typeof(ConfigFile));
            ConfigFile config = null;
            try
            {
                logger.PushLocation(file);

                config = (ConfigFile)deserializer.Deserialize(new StringReader(Preprocessor.Preprocess(File.ReadAllText(file), macros)));

                config?.PostLoad(parent, file, macros, variables, logger);
            }
            catch (Exception ex)
            {
                logger.Error(LoggingCodes.UnableToParseConfig, "Unable to parse file [{0}]", ex, file);
            }
            finally
            {
                logger.PopLocation();
            }
            return config;
        }

        public ConfigFile GetRoot()
        {
            var root = this;
            while (root.Parent != null)
                root = root.Parent;
            return root;
        }

        private readonly Dictionary<string,ConfigFile> mapIdToFile = new Dictionary<string, ConfigFile>();

        private void Verify(Logger logger)
        {
            Depends.Remove("");

            // TODO: verify Depends
            foreach (var depend in Depends)
            {
                if (!GetRoot().mapIdToFile.ContainsKey(depend))
                    logger.Error(LoggingCodes.MissingConfigDependency, $"Unable to resolve dependency [{depend}] for config file [{Id}]");
            }

            foreach (var includeDir in IncludeDirs)
            {
                includeDir.Path = ExpandString(includeDir.Path, false, logger);

                if (!includeDir.Path.StartsWith("=") && !Directory.Exists(includeDir.Path))
                    logger.Error(LoggingCodes.IncludeDirectoryNotFound, $"Include directory {includeDir.Path} from config file [{Id}] not found");
            }

            // Verify all dependencies
            foreach (var mappingFile in References)
                mappingFile.Verify(logger);
        }

        public static ConfigFile Load(ConfigFile root, string[] macros, Logger logger, params KeyValue[] variables)
        {
            root.PostLoad(null, null, macros, variables, logger);
            root.Verify(logger);
            root.ExpandVariables(false, logger);
            return root;
        }

        /// <summary>
        /// Loads a specified MappingFile.
        /// </summary>
        /// <param name="file">The MappingFile.</param>
        /// <returns>The MappingFile loaded</returns>
        public static ConfigFile Load(string file, string[] macros, Logger logger, params KeyValue[] variables)
        {
            var root =  Load(null, file, macros, variables, logger);
            root.Verify(logger);
            root.ExpandVariables(false, logger);
            return root;
        }

        public void Write(string file)
        {
            using var output = File.Create(file);

            Write(output);
        }

        /// <summary>
        /// Writes this MappingFile to the disk.
        /// </summary>
        /// <param name="writer">The writer.</param>
        public void Write(Stream writer)
        {
            //Create our own namespaces for the output
            var ns = new XmlSerializerNamespaces();
            ns.Add("", XmlNamespace);
            var serializer = new XmlSerializer(typeof(ConfigFile));
            serializer.Serialize(writer, this, ns);
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "config {0}", Id);
        }
    }
}