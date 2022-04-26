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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using SharpGen.Logging;

namespace SharpGen.Config;

/// <summary>
/// Config File.
/// </summary>
[XmlRoot("config", Namespace=XmlNamespace)]
public partial class ConfigFile
{
    internal const string XmlNamespace = "urn:SharpGen.Config";

    private static readonly Lazy<XmlSerializer> Serializer =
        new(static () => new XmlSerializer(typeof(ConfigFile)), LazyThreadSafetyMode.PublicationOnly);

    private static readonly Lazy<XmlSerializerNamespaces> XmlNamespaces =
        new(static () => new XmlSerializerNamespaces(new XmlQualifiedName[] { new(string.Empty, XmlNamespace) }),
            LazyThreadSafetyMode.PublicationOnly);

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
    public string Id
    {
        get => id ??= Guid.NewGuid().ToString();
        set => id = value;
    }

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

    [XmlIgnore] public string ExtensionId => Id + "-ext";

    /// <summary>
    /// Gets the name of the extension header file.
    /// </summary>
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
    /// <param name="dependencyListOutput">The dependencies set to fill.</param>
    public void FindAllDependencies(ISet<ConfigFile> dependencyListOutput)
    {
        ConfigFile ConfigSelector(string dependConfigFileId) => GetRoot().mapIdToFile[dependConfigFileId];

        foreach (var linkedConfig in Depends.Select(ConfigSelector))
        {
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
        switch (objectToExpand)
        {
            case null:
                return null;
            case string str:
                return ExpandString(str, expandDynamicVariable, logger);
            case IList list:
            {
                for (int i = 0, count = list.Count; i < count; i++)
                    list[i] = ExpandVariables(list[i], expandDynamicVariable, logger);
                return list;
            }
        }

        var type = objectToExpand.GetType();
        if (!type.GetTypeInfo().IsPrimitive)
        {
            foreach (var propertyInfo in type.GetRuntimeProperties())
            {
                if (propertyInfo.GetCustomAttributes<XmlIgnoreAttribute>(false).Any())
                    continue;

                // Check that this field is "ShouldSerializable"
                var method = type.GetRuntimeMethod("ShouldSerialize" + propertyInfo.Name, Type.EmptyTypes);
                if (method is not null && !(bool) method.Invoke(objectToExpand, null))
                    continue;

                var oldValue = propertyInfo.GetValue(objectToExpand, null);
                var newValue = ExpandVariables(oldValue, expandDynamicVariable, logger);
                if (!ReferenceEquals(oldValue, newValue))
                    propertyInfo.SetValue(objectToExpand, newValue, null);
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
    private static readonly Regex ReplaceVariableRegex = new(@"\$\(([a-zA-Z_][\w_]*)\)", RegexOptions.Compiled);

    private static readonly Regex ReplaceDynamicVariableRegex = new(@"#\(([a-zA-Z_][\w_]*)\)", RegexOptions.Compiled);

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
                    var name = match.Groups[1].Value;
                    var localResult = GetVariable(name, logger) ?? Environment.GetEnvironmentVariable(name);
                    if (localResult is not null)
                        return localResult;
                    logger.Error(LoggingCodes.UnknownVariable, "Unable to substitute config/environment variable $({0}). Variable is not defined", name);
                    return string.Empty;
                }
            );
        }

        // Perform Dynamic Variable substitution
        if (expandDynamicVariable && ReplaceDynamicVariableRegex.Match(result).Success)
        {
            result = ReplaceDynamicVariableRegex.Replace(
                result,
                match =>
                {
                    var name = match.Groups[1].Value;
                    if (GetRoot().DynamicVariables.TryGetValue(name, out var localResult))
                        return localResult.Trim('"');

                    logger.Error(LoggingCodes.UnknownDynamicVariable,
                                 "Unable to substitute dynamic variable #({0}). Variable is not defined", name);
                    return string.Empty;
                }
            );
        }

        return result;
    }

    private void PostLoad(ConfigFile parent, string file, string[] macros, IReadOnlyCollection<KeyValue> variables, Logger logger)
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
    /// <returns>The loaded config</returns>
    private static ConfigFile Load(ConfigFile parent, string file, string[] macros,
                                   IReadOnlyCollection<KeyValue> variables, Logger logger)
    {
        if(!File.Exists(file))
        {
            logger.Error(LoggingCodes.ConfigNotFound, "Configuration file {0} not found.", file);
            return null;
        }

        try
        {
            logger.PushLocation(file);

            using MemoryStream preprocessedStream = new();

            {
                using var configStream = File.OpenRead(file);
                using var configReader = XmlReader.Create(configStream);
                Preprocessor.Preprocess(configReader, preprocessedStream, macros);
            }

            preprocessedStream.Position = 0;

            if (Serializer.Value.Deserialize(preprocessedStream) is ConfigFile config)
            {
                config.PostLoad(parent, file, macros, variables, logger);

                return config;
            }
        }
        catch (Exception ex)
        {
            logger.Error(LoggingCodes.UnableToParseConfig, "Unable to parse file [{0}]", ex, file);
        }
        finally
        {
            logger.PopLocation();
        }
        return null;
    }

    public ConfigFile GetRoot()
    {
        var root = this;
        while (root.Parent != null)
            root = root.Parent;
        return root;
    }

    private readonly Dictionary<string, ConfigFile> mapIdToFile = new Dictionary<string, ConfigFile>();
    private string id;

    private void Verify(Logger logger)
    {
        Depends.Remove("");

        // TODO: verify Depends
        foreach (var depend in Depends)
        {
            if (GetRoot().mapIdToFile.ContainsKey(depend)) continue;

            logger.Error(
                LoggingCodes.MissingConfigDependency,
                "Unable to resolve dependency [{0}] for config file [{1}]",
                depend, Id
            );
        }

        foreach (var includeDir in IncludeDirs)
        {
            includeDir.Path = ExpandString(includeDir.Path, false, logger);

            if (includeDir.Path.StartsWith("=") || Directory.Exists(includeDir.Path)) continue;

            logger.Error(
                LoggingCodes.IncludeDirectoryNotFound,
                "Include directory {0} from config file [{1}] not found",
                includeDir.Path, Id
            );
        }

        // Verify all dependencies
        foreach (var mappingFile in References)
            mappingFile.Verify(logger);
    }

    public void Load(string file, string[] macros, Logger logger, params KeyValue[] variables)
    {
        PostLoad(null, file, macros, variables, logger);
        Verify(logger);
        ExpandVariables(false, logger);
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
        Serializer.Value.Serialize(writer, this, XmlNamespaces.Value);
    }

    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return string.Format(CultureInfo.InvariantCulture, "config {0}", Id);
    }
}