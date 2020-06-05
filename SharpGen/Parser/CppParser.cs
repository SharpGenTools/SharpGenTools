// Copyright (c) 2010 SharpDX - Alexandre Mutel
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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SharpGen.Logging;
using SharpGen.Config;
using SharpGen.CppModel;

namespace SharpGen.Parser
{
    internal static class Extension
    {
        /// <summary>
        /// Get the value from an attribute.
        /// </summary>
        /// <param name="xElement">The <see cref="XElement"/> object to get the attribute from.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <returns></returns>
        public static string AttributeValue(this XElement xElement, string name) => xElement.Attribute(name)?.Value;
    }

    /// <summary>
    /// Full C++ Parser built on top of <see cref="CastXmlRunner"/>.
    /// </summary>
    public sealed class CppParser
    {
        private CppModule _group;
        private readonly HashSet<string> _includeToProcess = new HashSet<string>();
        private readonly Dictionary<string, bool> _includeIsAttached = new Dictionary<string, bool>();
        private readonly Dictionary<string, HashSet<string>> _includeAttachedTypes = new Dictionary<string, HashSet<string>>();
        private readonly HashSet<string> _boundTypes = new HashSet<string>();
        private readonly ConfigFile _configRoot;
        private CppInclude _currentCppInclude;
        private readonly Dictionary<string, XElement> _mapIdToXElement = new Dictionary<string, XElement>();
        private readonly Dictionary<string, List<XElement>> _mapFileToXElement = new Dictionary<string, List<XElement>>();
        private readonly Dictionary<string, int> _mapIncludeToAnonymousEnumCount = new Dictionary<string, int>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CppParser"/> class.
        /// </summary>
        public CppParser(Logger logger, ConfigFile configRoot)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configRoot = configRoot ?? throw new ArgumentNullException(nameof(configRoot));
            Initialize();
        }

        public string OutputPath { get; set; }

        public Logger Logger { get; }

        private void Initialize()
        {
            foreach (var bindRule in _configRoot.ConfigFilesLoaded.SelectMany(cfg => cfg.Bindings))
            {
                if (_boundTypes.Contains(bindRule.From))
                {
                    Logger.Warning(LoggingCodes.DuplicateBinding, $"Duplicate type bind for [{bindRule.From}] specified. First binding takes priority.");
                }
                else
                {
                    _boundTypes.Add(bindRule.From);
                }
            }

            foreach (var configFile in _configRoot.ConfigFilesLoaded)
            {
                foreach (var includeRule in configFile.Includes)
                {
                    _includeToProcess.Add(includeRule.Id);

                    // Handle attach types
                    // Set that the include is attached (so that all types inside are attached
                    var isIncludeFullyAttached = includeRule.Attach ?? false;
                    if (isIncludeFullyAttached || includeRule.AttachTypes.Count > 0)
                    {
                        // An include can be fully attached ( include rule is set to true)
                        // or partially attached (the include rule contains Attach for specific types)
                        // We need to know which includes are attached, if they are fully or partially
                        if (!_includeIsAttached.ContainsKey(includeRule.Id))
                            _includeIsAttached.Add(includeRule.Id, isIncludeFullyAttached);
                        else if (isIncludeFullyAttached)
                        {
                            _includeIsAttached[includeRule.Id] = true;
                        }

                        // Attach types if any
                        if (includeRule.AttachTypes.Count > 0)
                        {
                            if (!_includeAttachedTypes.TryGetValue(includeRule.Id, out HashSet<string> typesToAttach))
                            {
                                typesToAttach = new HashSet<string>();
                                _includeAttachedTypes.Add(includeRule.Id, typesToAttach);
                            }

                            // For specific attach types, register them
                            foreach (var attachTypeName in includeRule.AttachTypes)
                            {
                                typesToAttach.Add(attachTypeName);
                            }
                        }
                    }
                }

                // Register extension headers
                if (configFile.Extension.Any(rule => rule.GeneratesExtensionHeader()))
                {
                    _includeToProcess.Add(configFile.ExtensionId);
                    if (!_includeIsAttached.ContainsKey(configFile.ExtensionId))
                        _includeIsAttached.Add(configFile.ExtensionId, true);
                }
            }
        }

        /// <summary>
        /// Gets the name of the generated GCCXML file.
        /// </summary>
        /// <value>The name of the generated GCCXML file.</value>
        private string GccXmlFileName => Path.Combine(OutputPath, _configRoot.Id + "-gcc.xml");

        public string RootConfigHeaderFileName => Path.Combine(OutputPath, _configRoot.HeaderFileName);

        /// <summary>
        /// Gets or sets the GccXml doc.
        /// </summary>
        /// <value>The GccXml doc.</value>
        private XDocument GccXmlDoc { get; set; }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <returns></returns>
        public CppModule Run(CppModule groupSkeleton, StreamReader xmlReader)
        {
            _group = groupSkeleton;
            Logger.Message("Config files changed.");

            const string progressMessage = "Parsing C++ headers starts, please wait...";

            try
            {

                Logger.Progress(15, progressMessage);

                if (xmlReader != null)
                {
                    Parse(xmlReader);
                }

                Logger.Progress(30, progressMessage);
            }
            catch (Exception ex)
            {
                Logger.Error(null, "Unexpected error", ex);
            }
            finally
            {
                // Write back GCCXML document on the disk
                using (var stream = File.OpenWrite(GccXmlFileName))
                {
                    GccXmlDoc?.Save(stream);
                }

                Logger.Message("Parsing headers is finished.");
            }


            // Track number of included macros for statistics
            foreach (var cppInclude in _group.Includes)
            {
                IncludeMacroCounts.TryGetValue(cppInclude.Name, out var count);
                count += cppInclude.Macros.Count();
                IncludeMacroCounts[cppInclude.Name] = count;
            }

            return _group;
        }
        
        public Dictionary<string, int> IncludeMacroCounts { get; } = new Dictionary<string, int>();


        /// <summary>
        /// Parses the specified reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        private void Parse(StreamReader reader)
        {
            var doc = XDocument.Load(reader);

            GccXmlDoc = doc;

            // Collects all GccXml elements and build map from their id
            foreach (var xElement in doc.Elements("GCC_XML").Elements())
            {
                var id = xElement.Attribute("id").Value;
                _mapIdToXElement.Add(id, xElement);

                var file = xElement.AttributeValue("file");
                if (file != null)
                {
                    if (!_mapFileToXElement.TryGetValue(file, out List<XElement> elementsInFile))
                    {
                        elementsInFile = new List<XElement>();
                        _mapFileToXElement.Add(file, elementsInFile);
                    }
                    elementsInFile.Add(xElement);
                }
            }

            // Fix all structure names
            AdjustTypeNamesFromTypedefs(doc);

            // Find all elements that are referring to a context and attach them to
            // the context as child elements
            foreach (var xElement in _mapIdToXElement.Values)
            {
                var id = xElement.AttributeValue("context");
                if (id != null)
                {
                    xElement.Remove();
                    _mapIdToXElement[id].Add(xElement);
                }
            }

            ParseAllElements();
        }

        private void AdjustTypeNamesFromTypedefs(XDocument doc)
        {
            foreach (var xTypedef in doc.Elements("GCC_XML").Elements(CastXml.TagTypedef))
            {
                var xStruct = _mapIdToXElement[xTypedef.AttributeValue("type")];
                switch (xStruct.Name.LocalName)
                {
                    case CastXml.TagStruct:
                    case CastXml.TagUnion:
                    case CastXml.TagEnumeration:
                        var structName = xStruct.AttributeValue("name");
                        // Rename all structure starting with tagXXXX to XXXX
                        if (structName.StartsWith("tag") || structName.StartsWith("_") || string.IsNullOrEmpty(structName))
                        {
                            var typeName = xTypedef.AttributeValue("name");
                            xStruct.SetAttributeValue("name", typeName);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Parses a C++ function.
        /// </summary>
        /// <param name="xElement">The gccxml <see cref="XElement"/> that describes a C++ function.</param>
        /// <returns>A C++ function parsed</returns>
        private CppFunction ParseFunction(XElement xElement)
        {
            return ParseCallable<CppFunction>(xElement);
        }

        /// <summary>
        /// Parses a C++ parameters.
        /// </summary>
        /// <param name="xElement">The gccxml <see cref="XElement"/> that describes a C++ parameter.</param>
        /// <param name="methodOrFunction">The method or function to populate.</param>
        private void ParseParameters(XElement xElement, CppElement methodOrFunction)
        {
            var paramCount = 0;
            foreach (var parameter in xElement.Elements())
            {
                if (parameter.Name.LocalName != "Argument")
                    continue;

                var cppParameter = new CppParameter { Name = parameter.AttributeValue("name") };
                if (string.IsNullOrEmpty(cppParameter.Name))
                    cppParameter.Name = "arg" + paramCount;

                ParseAnnotations(parameter, cppParameter);

                // All parameters without any annotations are considerate as In
                if (cppParameter.Attribute == ParamAttribute.None)
                    cppParameter.Attribute = ParamAttribute.In;

                Logger.PushContext("Parameter:[{0}]", cppParameter.Name);

                ResolveAndFillType(parameter.AttributeValue("type"), cppParameter);
                methodOrFunction.Add(cppParameter);

                Logger.PopContext();
                paramCount++;
            }
        }

        /// <summary>
        /// Parses C++ annotations/attributes.
        /// </summary>
        /// <param name="xElement">The gccxml <see cref="XElement"/> that contains C++ annotations/attributes.</param>
        /// <param name="cppElement">The C++ element to populate.</param>
        private static void ParseAnnotations(XElement xElement, CppElement cppElement)
        {
            // Check that the xml contains the "attributes" attribute
            var attributes = xElement.AttributeValue("attributes");
            if (string.IsNullOrWhiteSpace(attributes))
                return;

            // Strip whitespaces inside annotate("...")
            var stripSpaces = new StringBuilder();
            var doubleQuoteCount = 0;
            for (var i = 0; i < attributes.Length; i++)
            {
                var addThisChar = true;
                var attributeChar = attributes[i];
                if (attributeChar == '(')
                {
                    doubleQuoteCount++;
                }
                else if (attributeChar == ')')
                {
                    doubleQuoteCount--;
                }
                else if (doubleQuoteCount > 0 && (char.IsWhiteSpace(attributeChar) | attributeChar == '"'))
                {
                    addThisChar = false;
                }
                if (addThisChar)
                    stripSpaces.Append(attributeChar);
            }
            attributes = stripSpaces.ToString();

            // Default calling convention
            var cppCallingConvention = CppCallingConvention.Unknown;

            // Default parameter attribute
            var paramAttribute = ParamAttribute.None;

            // Default Guid
            string guid = null;

            // Parse attributes
            const string gccXmlAttribute = "annotate(";
            var isPre = false;
            var isPost = false;
            var hasWritable = false;

            // Clang outputs attributes in reverse order
            // TODO: Check if applies to all declarations
            foreach (var item in attributes.Split(' ').Reverse())
            {
                var newItem = item;
                if (newItem.StartsWith(gccXmlAttribute))
                    newItem = newItem.Substring(gccXmlAttribute.Length);

                if (newItem.StartsWith("SAL_pre"))
                {
                    isPre = true;
                    isPost = false;
                }
                else if (newItem.StartsWith("SAL_post"))
                {
                    isPre = false;
                    isPost = true;
                }
                else if (isPost && newItem.StartsWith("SAL_valid"))
                {
                    paramAttribute |= ParamAttribute.Out;
                }
                else if (newItem.StartsWith("SAL_maybenull") || (newItem.StartsWith("SAL_null") && newItem.Contains("__maybe")))
                {
                    paramAttribute |= ParamAttribute.Optional;
                }
                else if (newItem.StartsWith("SAL_readableTo") || newItem.StartsWith("SAL_writableTo"))
                {
                    if (newItem.StartsWith("SAL_writableTo"))
                    {
                        if (isPre) paramAttribute |= ParamAttribute.Out;
                        hasWritable = true;
                    }

                    if (!newItem.Contains("SPECSTRINGIZE(1)") && !newItem.Contains("elementCount(1)"))
                        paramAttribute |= ParamAttribute.Buffer;
                }
                else if (newItem.StartsWith("__stdcall__"))
                {
                    cppCallingConvention = CppCallingConvention.StdCall;
                }
                else if (newItem.StartsWith("__cdecl__"))
                {
                    cppCallingConvention = CppCallingConvention.CDecl;
                }
                else if (newItem.StartsWith("__thiscall__"))
                {
                    cppCallingConvention = CppCallingConvention.ThisCall;
                }
                else if (newItem.StartsWith("uuid("))
                {
                    guid = newItem.Trim(')').Substring("uuid(".Length).Trim('"', '{', '}');
                }
            }

            // If no writable, than this is an In parameter
            if (!hasWritable)
            {
                paramAttribute |= ParamAttribute.In;
            }


            // Update CppElement based on its type
            if (cppElement is CppParameter param)
            {
                // Replace in & out with inout.
                // Todo check to use in & out instead of inout
                if ((paramAttribute & ParamAttribute.In) != 0 && (paramAttribute & ParamAttribute.Out) != 0)
                {
                    paramAttribute ^= ParamAttribute.In;
                    paramAttribute ^= ParamAttribute.Out;
                    paramAttribute |= ParamAttribute.InOut;
                }

                param.Attribute = paramAttribute;
            }
            else if (cppElement is CppCallable callable && cppCallingConvention != CppCallingConvention.Unknown)
            {
                callable.CallingConvention = cppCallingConvention;
            }
            else if (cppElement is CppInterface iface && guid != null)
            {
                iface.Guid = guid;
            }
        }

        /// <summary>
        /// Parses a C++ method or function.
        /// </summary>
        /// <typeparam name="T">The resulting C++ parsed element. Must be a subclass of <see cref="CppMethod"/>.</typeparam>
        /// <param name="xElement">The gccxml <see cref="XElement"/> that describes a C++ method/function declaration.</param>
        /// <returns>The C++ parsed T.</returns>
        private T ParseCallable<T>(XElement xElement) where T : CppCallable, new()
        {
            var cppCallable = new T { Name = xElement.AttributeValue("name") };

            Logger.PushContext("Callable:[{0}]", cppCallable.Name);

            // Parse annotations
            ParseAnnotations(xElement, cppCallable);

            // Parse parameters
            ParseParameters(xElement, cppCallable);

            cppCallable.ReturnValue = new CppReturnValue();
            ResolveAndFillType(xElement.AttributeValue("returns"), cppCallable.ReturnValue);

            Logger.PopContext();

            return cppCallable;
        }

        /// <summary>
        /// Parses a C++ COM interface.
        /// </summary>
        /// <param name="xElement">The gccxml <see cref="XElement"/> that describes a C++ COM interface declaration.</param>
        /// <returns>A C++ interface parsed</returns>
        private CppInterface ParseInterface(XElement xElement)
        {
            // If element is already transformed, return it
            var cppInterface = xElement.Annotation<CppInterface>();
            if (cppInterface != null)
                return cppInterface;

            // Else, create a new CppInterface
            cppInterface = new CppInterface { Name = xElement.AttributeValue("name") };
            xElement.AddAnnotation(cppInterface);

            // Enter Interface description
            Logger.PushContext("Interface:[{0}]", cppInterface.Name);

            // Calculate offset method using inheritance
            var offsetMethod = 0;

            var basesValue = xElement.AttributeValue("bases");
            var bases = basesValue != null ? basesValue.Split(' ') : Enumerable.Empty<string>();
            foreach (var xElementBaseId in bases)
            {
                if (string.IsNullOrEmpty(xElementBaseId))
                    continue;

                var xElementBase = _mapIdToXElement[xElementBaseId];
                var baseTypeName = xElementBase.AttributeValue("name");

                CppInterface cppInterfaceBase = null;
                Logger.RunInContext("Base", () => { cppInterfaceBase = ParseInterface(xElementBase); });

                if (string.IsNullOrEmpty(cppInterface.Base) && IsTypeBinded(xElementBase))
                    cppInterface.Base = cppInterfaceBase.Name;

                offsetMethod += cppInterfaceBase.TotalMethodCount;
            }

            // Parse annotations
            ParseAnnotations(xElement, cppInterface);

            int offsetMethodBase = offsetMethod;

            var methods = new List<CppMethod>();

            // Parse methods
            foreach (var method in xElement.Elements())
            {
                // Parse method with pure virtual (=0) and that do not override any other methods
                if (method.Name.LocalName == "Method" && !string.IsNullOrWhiteSpace(method.AttributeValue("pure_virtual"))
                    && string.IsNullOrWhiteSpace(method.AttributeValue("overrides")))
                {
                    var cppMethod = ParseCallable<CppMethod>(method);
                    methods.Add(cppMethod);
                    cppMethod.Offset = offsetMethod++;
                }
            }

            SetMethodsWindowsOffset(methods, offsetMethodBase);

            // Add the methods to the interface with the correct offsets
            foreach (var cppMethod in methods)
            {
                cppInterface.Add(cppMethod);
            }

            cppInterface.TotalMethodCount = offsetMethod;

            // Leave Interface
            Logger.PopContext();

            return cppInterface;
        }

        private static void SetMethodsWindowsOffset(IEnumerable<CppMethod> nativeMethods, int vtableIndexStart)
        {
            List<CppMethod> methods = new List<CppMethod>(nativeMethods);
            // The Visual C++ compiler breaks the rules of the COM ABI when overloaded methods are used.
            // It will group the overloads together in memory and lay them out in the reverse of their declaration order.
            // Since CastXML always lays them out in the order declared, we have to modify the order of the methods to match Visual C++.
            for (int i = 0; i < methods.Count; i++)
            {
                var name = methods[i].Name;

                // Look for overloads of this function
                for (int j = i + 1; j < methods.Count; j++)
                {
                    var nextMethod = methods[j];
                    if (nextMethod.Name == name)
                    {
                        // Remove this one from its current position further into the vtable
                        methods.RemoveAt(j);

                        // Put this one before all other overloads (aka reverse declaration order)
                        var k = i - 1;
                        while (k >= 0 && methods[k].Name == name)
                            k--;
                        methods.Insert(k + 1, nextMethod);
                        i++;
                    }
                }
            }

            int methodOffset = vtableIndexStart;
            foreach (var cppMethod in methods)
            {
                cppMethod.WindowsOffset = methodOffset++;
            }
        }

        /// <summary>
        /// Parses a C++ field declaration.
        /// </summary>
        /// <param name="xElement">The gccxml <see cref="XElement"/> that describes a C++ structure field declaration.</param>
        /// <returns>A C++ field parsed</returns>
        private CppField ParseField(XElement xElement, int fieldOffset)
        {
            var fieldName = xElement.AttributeValue("name");
            var cppField = new CppField
            {
                Name = string.IsNullOrEmpty(fieldName) ? $"field{fieldOffset}" : fieldName,
                Offset = fieldOffset
            };

            Logger.PushContext("Field:[{0}]", cppField.Name);

            // Handle bitfield info
            var bitField = xElement.AttributeValue("bits");
            if (!string.IsNullOrEmpty(bitField))
            {
                cppField.IsBitField = true;

                // Todo, int.Parse could failed?
                cppField.BitOffset = int.Parse(bitField);
            }

            ResolveAndFillType(xElement.AttributeValue("type"), cppField);

            Logger.PopContext();
            return cppField;
        }

        /// <summary>
        /// Parses a C++ struct or union declaration.
        /// </summary>
        /// <param name="xElement">The gccxml <see cref="XElement"/> that describes a C++ struct or union declaration.</param>
        /// <param name="cppParent">The C++ parent object (valid for anonymous inner declaration) .</param>
        /// <param name="innerAnonymousIndex">An index that counts the number of anonymous declaration in order to set a unique name</param>
        /// <returns>A C++ struct parsed</returns>
        private CppStruct ParseStructOrUnion(XElement xElement, CppElement cppParent = null, int innerAnonymousIndex = 0)
        {
            var cppStruct = xElement.Annotation<CppStruct>();
            if (cppStruct != null)
                return cppStruct;

            // Build struct name directly from the struct name or based on the parent
            var structName = GetStructName(xElement, cppParent, innerAnonymousIndex);

            // Create struct
            cppStruct = new CppStruct { Name = structName };
            xElement.AddAnnotation(cppStruct);
            var isUnion = (xElement.Name.LocalName == CastXml.TagUnion);
            cppStruct.IsUnion = isUnion;

            // Get align from structure
            cppStruct.Align = int.Parse(xElement.AttributeValue("align")) / 8;

            // By default, packing is platform x86/x64 dependent (4 or 8)
            // but because gccxml is running in x86, it outputs 4
            // So by default, we are reversing all align by 4 to 0
            // IF the packing is a true 4, than it will be reverse back by a later mapping rules
            if (cppStruct.Align == 4)
                cppStruct.Align = 0;

            // Enter struct/union description
            Logger.PushContext("{0}:[{1}]", xElement.Name.LocalName, cppStruct.Name);

            var basesValue = xElement.AttributeValue("bases");
            var bases = basesValue != null ? basesValue.Split(' ') : Enumerable.Empty<string>();

            cppStruct.Base = GetStructDirectBase(bases);

            // Parse all fields
            var fieldOffset = 0;
            var innerStructCount = 0;
            foreach (var field in xElement.Elements())
            {
                if (field.Name.LocalName != CastXml.TagField)
                    continue;

                // Parse the field
                var cppField = ParseField(field, fieldOffset);

                // Test if the field type is declared inside this struct or union
                var fieldName = field.AttributeValue("name");
                var fieldType = _mapIdToXElement[field.AttributeValue("type")];
                if (fieldType.AttributeValue("context") == xElement.AttributeValue("id"))
                {
                    var fieldSubStruct = ParseStructOrUnion(fieldType, cppStruct, innerStructCount++);
                    
                    // If fieldName is empty, then we need to inline fields from the struct/union.
                    if (string.IsNullOrEmpty(fieldName))
                    {
                        // Make a copy in order to remove fields
                        var listOfSubFields = new List<CppField>(fieldSubStruct.Fields);
                        // Copy the current field offset
                        var lastFieldOffset = fieldOffset;
                        foreach (var subField in listOfSubFields)
                        {
                            subField.Offset = subField.Offset + fieldOffset;
                            cppStruct.Add(subField);
                            lastFieldOffset = subField.Offset;
                        }
                        // Set the current field offset according to the inlined fields
                        if (!isUnion)
                            fieldOffset = lastFieldOffset;
                        // Don't add the current field, as it is actually an inline struct/union
                        cppField = null;
                    }
                    else
                    {
                        // Get the type name from the inner-struct and set it to the field
                        cppField.TypeName = fieldSubStruct.Name;
                        _currentCppInclude.Add(fieldSubStruct);
                    }
                }

                // Go to next field offset if not in union
                var goToNextFieldOffset = !isUnion;

                // Add the field if any
                if (cppField != null)
                {
                    cppStruct.Add(cppField);
                    // TODO managed multiple bitfield group
                    // Current implem is only working with a single set of consecutive bitfield in the same struct
                    goToNextFieldOffset = goToNextFieldOffset && !cppField.IsBitField;
                }

                if (goToNextFieldOffset)
                    fieldOffset++;
            }

            // Leave struct
            Logger.PopContext();

            return cppStruct;
        }

        private string GetStructDirectBase(IEnumerable<string> bases)
        {
            string baseName = default;
            foreach (var xElementBaseId in bases)
            {
                if (string.IsNullOrEmpty(xElementBaseId))
                    continue;

                var xElementBase = _mapIdToXElement[xElementBaseId];

                CppStruct cppStructBase = null;
                Logger.RunInContext("Base", () => { cppStructBase = ParseStructOrUnion(xElementBase); });

                if (string.IsNullOrEmpty(cppStructBase.Base))
                    baseName = cppStructBase.Name;
            }

            return baseName;
        }

        private static string GetStructName(XElement xElement, CppElement cppParent, int innerAnonymousIndex)
        {
            var structName = xElement.AttributeValue("name") ?? "";
            if (cppParent != null)
            {
                if (string.IsNullOrEmpty(structName))
                {
                    structName = cppParent.Name + "_INNER_" + innerAnonymousIndex;
                }
                else
                {
                    structName = cppParent.Name + "_" + structName + "_INNER";
                }
            }

            return structName;
        }

        /// <summary>
        /// Parses a C++ enum declaration.
        /// </summary>
        /// <param name="xElement">The gccxml <see cref="XElement"/> that describes a C++ enum declaration.</param>
        /// <returns>A C++ parsed enum</returns>
        private CppEnum ParseEnum(XElement xElement)
        {
            var cppEnum = new CppEnum { Name = xElement.AttributeValue("name") };

            // Doh! Anonymous Enum, need to handle them!
            if (cppEnum.Name.StartsWith("$") || string.IsNullOrEmpty(cppEnum.Name))
            {
                var includeFrom = GetIncludeIdFromFileId(xElement.AttributeValue("file"));

                if (!_mapIncludeToAnonymousEnumCount.TryGetValue(includeFrom, out int enumOffset))
                    _mapIncludeToAnonymousEnumCount.Add(includeFrom, enumOffset);

                cppEnum.Name = includeFrom.ToUpper() + "_ENUM_" + enumOffset;

                _mapIncludeToAnonymousEnumCount[includeFrom]++;
            }

            foreach (var xEnumItems in xElement.Elements())
            {
                var enumItemName = xEnumItems.AttributeValue("name");
                if (enumItemName.EndsWith(CppExtensionHeaderGenerator.EndTagCustomEnumItem))
                    enumItemName = enumItemName.Substring(0, enumItemName.Length - CppExtensionHeaderGenerator.EndTagCustomEnumItem.Length);

                cppEnum.Add(new CppEnumItem(enumItemName, xEnumItems.AttributeValue("init")));

            }
            return cppEnum;
        }

        /// <summary>
        /// Parses a C++ variable declaration/definition.
        /// </summary>
        /// <param name="xElement">The gccxml <see cref="XElement"/> that describes a C++ variable declaration/definition.</param>
        /// <returns>A C++ parsed variable</returns>
        private CppElement ParseVariable(XElement xElement)
        {
            var name = xElement.AttributeValue("name");
            if (name.EndsWith(CppExtensionHeaderGenerator.EndTagCustomVariable))
                name = name.Substring(0, name.Length - CppExtensionHeaderGenerator.EndTagCustomVariable.Length);

            var cppMarshallable = new CppMarshallable();
            ResolveAndFillType(xElement.AttributeValue("type"), cppMarshallable);


            var value = xElement.AttributeValue("init") ?? string.Empty;
            if (cppMarshallable.TypeName == "GUID")
            {
                var guid = ParseGuid(value);
                if (!guid.HasValue)
                    return null;
                return new CppGuid { Name = name, Guid = guid.Value };
            }

            // CastXML outputs initialization expressions. Cast to proper type.
            var match = Regex.Match(value, @"\((?:\(.+\))?(.+)\)");
            if (match.Success)
            {
                value = $"unchecked(({cppMarshallable.TypeName}){match.Groups[1].Value})";
            }

            // Handle C++ floating point literals
            value = value.Replace(".F", ".0F");

            return new CppConstant { Name = name, Value = value };
        }

        /// <summary>
        /// Parses a C++ GUID definition string.
        /// </summary>
        /// <param name="guidInitText">The text of a GUID gccxml initialization.</param>
        /// <returns>The parsed Guid</returns>
        private static Guid? ParseGuid(string guidInitText)
        {
            // init="{-1135593225ul, 9184u, 18784u, {150u, 218u, 51u, 171u, 175u, 89u, 53u, 236u}}"
            if (!guidInitText.StartsWith("{") && !guidInitText.EndsWith("}}"))
                return null;

            guidInitText = guidInitText.Replace("{", "");
            guidInitText = guidInitText.TrimEnd('}');
            guidInitText = guidInitText.Replace("u", "");
            guidInitText = guidInitText.Replace("U", "");
            guidInitText = guidInitText.Replace("l", "");
            guidInitText = guidInitText.Replace("L", "");
            guidInitText = guidInitText.Replace(" ", "");

            var guidElements = guidInitText.Split(',');

            if (guidElements.Length != 11)
                return null;

            var values = new int[guidElements.Length];
            for (int i = 0; i < guidElements.Length; i++)
            {
                var guidElement = guidElements[i];
                if (!long.TryParse(guidElement, out long value))
                    return null;

                values[i] = unchecked((int)value);
            }

            return new Guid(values[0], (short)values[1], (short)values[2], (byte)values[3], (byte)values[4], (byte)values[5], (byte)values[6], (byte)values[7],
                     (byte)values[8], (byte)values[9], (byte)values[10]);
        }

        /// <summary>
        /// Parses all C++ elements. This is the main method that iterates on all types.
        /// </summary>
        private void ParseAllElements()
        {
            foreach (var includeGccXmlId in _mapFileToXElement.Keys)
            {
                var includeId = GetIncludeIdFromFileId(includeGccXmlId);

                // Process only files listed inside the config files
                if (!_includeToProcess.Contains(includeId))
                    continue;

                // Process only files attached (fully or partially) to an assembly/namespace
                if (!_includeIsAttached.TryGetValue(includeId, out bool isIncludeFullyAttached))
                    continue;

                // Log current include being processed
                Logger.PushContext("Include:[{0}.h]", includeId);

                _currentCppInclude = _group.FindInclude(includeId);
                if (_currentCppInclude == null)
                {
                    _currentCppInclude = new CppInclude { Name = includeId };
                    _group.Add(_currentCppInclude);
                }

                ParseElementsInInclude(includeGccXmlId, includeId, isIncludeFullyAttached);

                Logger.PopContext();
            }
        }

        private void ParseElementsInInclude(string includeGccXmlId, string includeId, bool isIncludeFullyAttached)
        {
            foreach (var xElement in _mapFileToXElement[includeGccXmlId])
            {
                // If the element is not defined from a root namespace
                // than skip it, as it might be an inner type
                if (_mapIdToXElement[xElement.AttributeValue("context")].Name.LocalName != CastXml.TagNamespace)
                    continue;

                // If incomplete flag, than element cannot be parsed
                if (xElement.AttributeValue("incomplete") != null)
                    continue;


                var elementName = xElement.AttributeValue("name");

                // If this include is partially attached and the current type is not attached
                // Than skip it, as we are not mapping it
                if (!isIncludeFullyAttached && !_includeAttachedTypes[includeId].Contains(elementName))
                    continue;

                // Ignore CastXML built-in functions
                if (elementName.StartsWith("__builtin"))
                    continue;

                var cppElement = ParseElement(xElement);

                if (cppElement != null)
                    _currentCppInclude.Add(cppElement);
            }
        }

        private CppElement ParseElement(XElement xElement)
        {
            switch (xElement.Name.LocalName)
            {
                case CastXml.TagEnumeration:
                    return ParseEnum(xElement);
                case CastXml.TagFunction:
                    // TODO: Find better criteria for exclusion. In CastXML extern="1" only indicates an explicit external storage modifier.
                    // For now, exclude inline functions instead; may not be sensible since by default all functions have external linkage.
                    if (xElement.AttributeValue("inline") == null)
                        return ParseFunction(xElement);
                    break;
                case CastXml.TagClass:
                case CastXml.TagStruct:
                    return xElement.AttributeValue("abstract") != null ? (CppElement)ParseInterface(xElement) : ParseStructOrUnion(xElement);
                case CastXml.TagUnion:
                    return ParseStructOrUnion(xElement);
                case CastXml.TagVariable:
                    if (xElement.AttributeValue("init") != null)
                        return ParseVariable(xElement);
                    break;
            }
            return null;
        }

        /// <summary>
        /// Determines whether the specified type is a type included in the mapping process.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        /// 	<c>true</c> if the specified type is included in the mapping process; otherwise, <c>false</c>.
        /// </returns>
        private bool IsTypeFromIncludeToProcess(XElement type)
        {
            var fileId = type.AttributeValue("file");
            if (fileId != null)
                return _includeToProcess.Contains(GetIncludeIdFromFileId(fileId));
            return false;
        }

        /// <summary>
        /// Determines whether the specified type is bound in the mapping process and will be represented in the C# model.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>
        /// 	<c>true</c> if the specified type is bound in the mapping process; otherwise, <c>false</c>.
        /// </returns>
        private bool IsTypeBinded(XElement type)
            => IsTypeFromIncludeToProcess(type) || _boundTypes.Contains(type.AttributeValue("name"));

        /// <summary>
        /// Resolves a type to its fundamental type or a binded type.
        /// This methods is going through the type declaration in order to return the most fundamental type
        /// or to return a bind.
        /// </summary>
        /// <param name="typeId">The id of the type to resolve.</param>
        /// <param name="type">The C++ type to fill.</param>
        private void ResolveAndFillType(string typeId, CppMarshallable type)
        {
            var typeResolutionPath = new List<string>();

            var xType = _mapIdToXElement[typeId];

            var isTypeResolved = false;

            while (!isTypeResolved)
            {
                var name = xType.AttributeValue("name");
                if (name != null)
                    typeResolutionPath.Add(name);
                var nextType = xType.AttributeValue("type");
                switch (xType.Name.LocalName)
                {
                    case CastXml.TagFundamentalType:
                        type.TypeName = ConvertFundamentalType(name);
                        isTypeResolved = true;
                        break;
                    case CastXml.TagClass:
                    case CastXml.TagEnumeration:
                    case CastXml.TagStruct:
                    case CastXml.TagUnion:
                        type.TypeName = name;
                        isTypeResolved = true;
                        break;
                    case CastXml.TagTypedef:
                        if (_boundTypes.Contains(name))
                        {
                            type.TypeName = name;
                            isTypeResolved = true;
                        }
                        xType = _mapIdToXElement[nextType];
                        break;
                    case CastXml.TagPointerType:
                        xType = _mapIdToXElement[nextType];
                        type.Pointer = (type.Pointer ?? "") + "*";
                        break;
                    case CastXml.TagArrayType:
                        type.IsArray = true;
                        var maxArrayIndex = xType.AttributeValue("max");
                        var arrayDim = int.Parse(maxArrayIndex.TrimEnd('u')) + 1;
                        if (type.ArrayDimension == null)
                            type.ArrayDimension = arrayDim.ToString();
                        else
                            type.ArrayDimension += "," + arrayDim;
                        xType = _mapIdToXElement[nextType];
                        break;
                    case CastXml.TagReferenceType:
                        xType = _mapIdToXElement[nextType];
                        type.Pointer = (type.Pointer ?? "") + "&";
                        break;
                    case CastXml.TagCvQualifiedType:
                        xType = _mapIdToXElement[nextType];
                        type.Const = true;
                        break;
                    case CastXml.TagFunctionType:
                        // TODO, handle different calling convention
                        type.TypeName = "__function__stdcall";
                        isTypeResolved = true;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Unexpected tag type [{0}]", xType.Name.LocalName));
                }
            }
        }

        /// <summary>
        /// Converts a gccxml FundamentalType to a shorter form:
        ///	signed char          => char
        ///	long long int        => longlong
        ///	short unsigned int   => unsigned short
        ///	char                 => char
        ///	long unsigned int    => unsigned int
        ///	short int            => short
        ///	int                  => int
        ///	long int             => long
        ///	float                => float
        ///	unsigned char        => unsigned char
        ///	unsigned int         => unsigned int
        ///	wchar_t              => wchar_t
        ///	long long unsigned int => unsigned longlong
        ///	double               => double
        ///	void                 => void
        ///	long double          => long double
        /// </summary>
        /// <param name="typeName">Name of the gccxml fundamental type.</param>
        /// <returns>a shorten form</returns>
        private string ConvertFundamentalType(string typeName)
        {
            var types = typeName.Split(' ');

            var isUnsigned = false;
            var outputType = "";
            var shortCount = 0;
            var longCount = 0;

            foreach (var type in types)
            {
                switch (type)
                {
                    case "unsigned":
                        isUnsigned = true;
                        break;
                    case "signed":
                        outputType = "int";
                        break;
                    case "long":
                        longCount++;
                        break;
                    case "short":
                        shortCount++;
                        break;
                    case "bool":
                    case "void":
                    case "char":
                    case "double":
                    case "int":
                    case "float":
                    case "wchar_t":
                        outputType = type;
                        break;
                    default:
                        Logger.Error(LoggingCodes.UnknownFundamentalType, "Unhandled partial type [{0}] from Fundamental type [{1}]", type, typeName);
                        break;
                }
            }

            if (longCount == 1)
                outputType = "long";
            if (longCount == 1 && outputType == "double")
                outputType = "long double";     // 96 bytes, unhandled
            if (longCount == 2)
                outputType = "longlong";
            if (shortCount == 1)
                outputType = "short";
            if (isUnsigned)
                outputType = "unsigned " + outputType;
            return outputType;
        }


        /// <summary>
        /// Gets the include id from the file id.
        /// </summary>
        /// <param name="fileId">The file id.</param>
        /// <returns>A include id</returns>
        private string GetIncludeIdFromFileId(string fileId)
        {
            var filePath = _mapIdToXElement[fileId].AttributeValue("name");
            try
            {
                if (!File.Exists(filePath))
                    return "";
            }
            catch (ArgumentException)
            {
                return "";
            }
            return Path.GetFileNameWithoutExtension(filePath);
        }
    }
}