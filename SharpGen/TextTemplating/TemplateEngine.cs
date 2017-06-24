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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using SharpGen.Logging;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Linq;
#if NETSTANDARD1_5
using System.Runtime.Loader;
#endif

namespace SharpGen.TextTemplating
{
    /// <summary>
    /// Lightweight implementation of T4 engine, using Tokenizer from MonoDevelop.TextTemplating.
    /// </summary>
    public class TemplateEngine
    {
        private StringBuilder _doTemplateCode;
        private StringBuilder _doTemplateClassCode;
        private bool _isTemplateClassCode;
        private List<Directive> _directives;
        private Dictionary<string, ParameterValueType> _parameters;

        /// <summary>
        /// Occurs when an include needs to be found.
        /// </summary>
        public event EventHandler<TemplateIncludeArgs> OnInclude;


        /// <summary>
        /// Template code.
        /// Parameter {0} = List of import namespaces.
        /// Parameter {1} = List of parameters declaration.
        /// Parameter {2} = Body of template code
        /// Parameter {3} = Body of template class level code
        /// </summary>
        private const string GenericTemplateCodeText = @"
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
{0}
    public class TemplateImpl : SharpGen.TextTemplating.Templatizer
    {{
{1}
        public override void Process()
        {{
            // DoTemplate code
{2}
        }}

        // DoTemplateClass code
{3}
    }}
";
        public TemplateEngine(Logger logger)
        {
            _parameters = new Dictionary<string, ParameterValueType>();
            Logger = logger;
        }

        public Logger Logger { get; }


        /// <summary>
        /// Adds a text to Templatizer.Process section .
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="content">The content.</param>
        private void AddDoTemplateCode(Location location, string content)
        {
            if (_isTemplateClassCode)
                throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "Cannot add Process code [{0}] after Process Class level code", content));

            _doTemplateCode.AppendLine().AppendFormat("#line {0} \"{1}\"", location.Line, location.FileName).AppendLine();
            _doTemplateCode.Append(content);
        }

        /// <summary>
        /// Adds a text to at Templatizer class level.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="content">The content.</param>
        private void AddDoTemplateClassCode(Location location, string content)
        {
            _doTemplateClassCode.AppendLine().AppendFormat("#line {0} \"{1}\"", location.Line, location.FileName).AppendLine();
            _doTemplateClassCode.Append(content);
        }

        /// <summary>
        /// Adds some code to the current 
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="code">The code.</param>
        private void AddCode(Location location, string code)
        {
            if (_isTemplateClassCode)
                AddDoTemplateClassCode(location, code);
            else
                AddDoTemplateCode(location, code);
        }

        /// <summary>
        /// Add a multiline string to the template code.
        /// This methods decompose a string in lines and generate template code for each line
        /// using Templatizer.Write/WriteLine methods.
        /// </summary>
        /// <param name="location">The location in the text template file.</param>
        /// <param name="content">The content to add to the code.</param>
        private void AddContent(Location location, string content)        
        {
            content = content.Replace("\"", "\\\"");

            var reader = new StringReader(content);
            var lines = new List<string>();
            string line;
            while ((line = reader.ReadLine()) != null)
                lines.Add(line);

            Location fromLocation = location;

            for (int i = 0; i < lines.Count;i++)
            {
                line = lines[i];

                location = new Location(fromLocation.FileName, fromLocation.Line + i, fromLocation.Column);

                if ((i + 1) == lines.Count)
                {
                    if (content.EndsWith(line))
                    {
                        if (!string.IsNullOrEmpty(line))
                            AddCode(location, "Write(\"" + line + "\");\n");
                    }
                    else
                        AddCode(location, "WriteLine(\"" + line + "\");\n");
                }
                else
                {
                    AddCode(location, "WriteLine(\"" + line + "\");\n");
                }
            }
        }

        private void AddExpression(Location location, string content)
        {
            AddCode(location, "Write(" + content + ");\n");
        }
        
        /// <summary>
        /// Process a template and returns the processed file.
        /// </summary>
        /// <param name="templateText">The template text.</param>
        /// <returns></returns>
        public string ProcessTemplate(string templateText, string templateName)
        {
            Assembly templateAssembly = GenerateTemplateAssembly(templateText, templateName);
            // Get a new templatizer instance
            var templatizer = (Templatizer)Activator.CreateInstance(templateAssembly.GetType("TemplateImpl"));

            // Set all parameters for the template
            foreach (var parameterValueType in _parameters.Values)
            {
                var propertyInfo = templatizer.GetType().GetRuntimeProperty(parameterValueType.Name);
                propertyInfo.SetValue(templatizer, parameterValueType.Value, null);
            }

            // Run the templatizer
            templatizer.Process();

            // Returns the text
            return templatizer.ToString();
        }
        
        private Assembly GenerateTemplateAssembly(string templateText, string templateName)
        {
            // Initialize TemplateEngine state
            _doTemplateCode = new StringBuilder();
            _doTemplateClassCode = new StringBuilder();
            _isTemplateClassCode = false;
            Assembly templateAssembly = null;
            _directives = new List<Directive>();

            // Parse the T4 template text
            Parse(templateText, templateName);

            // Build parameters for template
            var parametersCode = new StringBuilder();
            foreach (var parameterValueType in _parameters.Values)
                parametersCode.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "public {0} {1} {{ get; set; }}\n", parameterValueType.Type.FullName, parameterValueType.Name));

            // Build import namespaces for template
            var importNamespaceCode = new StringBuilder();
            foreach (var directive in _directives)
            {
                if (directive.Name == "import")
                    importNamespaceCode.Append("using " + directive.Attributes["namespace"] + ";\n");
            }

            // Expand final template class code
            // Parameter {0} = List of import namespaces.
            // Parameter {1} = List of parameters declaration.
            // Parameter {2} = Body of template code
            // Parameter {3} = Body of template class level code
            string templateSourceCode = string.Format(GenericTemplateCodeText, importNamespaceCode, parametersCode, _doTemplateCode, _doTemplateClassCode);

            // Creates the C# compiler
            //The location of the .NET assemblies
            var assemblyPath = Path.GetDirectoryName(typeof(object).GetTypeInfo().Assembly.Location);

            /* 
                * Adding some necessary .NET assemblies
                * These assemblies couldn't be loaded correctly via the same construction as above,
                * in specific the System.Runtime.
                */
            var returnList = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(GetType().GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.CoreLib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll")),
                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly.Location)
            };
            var compilation = CSharpCompilation.Create($"SharpGen.{templateName}.{Guid.NewGuid()}", options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            compilation = compilation.AddReferences(returnList.ToArray());

            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(templateSourceCode));

            var memoryStream = new MemoryStream();
            // Compiles the code
            var compilerResults = compilation.Emit(memoryStream);

            var compilationErrors = compilerResults.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            // Output any errors
            foreach (var compilerError in compilationErrors)
                Logger.Error(compilerError.ToString());

            // If successful, gets the compiled assembly
            if (!compilationErrors.Any())
            {
#if NETSTANDARD1_5
                memoryStream.Seek(0, SeekOrigin.Begin);
                templateAssembly = AssemblyLoadContext.Default.LoadFromStream(memoryStream);
#else
                templateAssembly = Assembly.Load(memoryStream.ToArray());
#endif
            }
            else
            {
                Logger.Fatal("Template [{0}] contains error", templateName);
            }
            return templateAssembly;
        }

        /// <summary>
        /// Adds a parameter with the object to this template. 
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">an object.</param>
        /// <exception cref="ArgumentNullException">If value is null</exception>
        public void SetParameter(string name, object value)
        {
            if (value == null) throw new ArgumentNullException("value");

            SetParameter(name, value, value.GetType());
        }

        /// <summary>
        /// Adds a parameter with the object to this template. 
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">an object.</param>
        /// <param name="typeOf">Type of the object.</param>
        /// <exception cref="ArgumentNullException">If typeOf is null</exception>
        public void SetParameter(string name, object value, Type typeOf)
        {
            if (typeOf == null) throw new ArgumentNullException("typeOf");

            if (_parameters.ContainsKey(name))
                _parameters.Remove(name);

            _parameters.Add(name, new ParameterValueType { Name = name, Type = typeOf, Value = value });
        }

        /// <summary>
        /// An association between a Value and a Type.
        /// </summary>
        private class ParameterValueType
        {
            public string Name;
            public Type Type;
            public object Value;
        }

        /// <summary>
        /// Parses the specified template text.
        /// </summary>
        /// <param name="templateText">The template text.</param>
        private void Parse(string templateText, string templateName)
        {
            var tokeniser = new Tokeniser(templateName, templateText);

            AddCode(tokeniser.Location, "");

            bool skip = false;
            while ((skip || tokeniser.Advance()) && tokeniser.State != State.EOF)
            {
                skip = false;
                switch (tokeniser.State)
                {
                    case State.Block:
                        if (!String.IsNullOrEmpty(tokeniser.Value))
                            AddDoTemplateCode(tokeniser.Location, tokeniser.Value);
                        break;
                    case State.Content:
                        if (!String.IsNullOrEmpty(tokeniser.Value))
                            AddContent(tokeniser.Location, tokeniser.Value);
                        break;
                    case State.Expression:
                        if (!String.IsNullOrEmpty(tokeniser.Value))
                            AddExpression(tokeniser.Location, tokeniser.Value);
                        break;
                    case State.Helper:
                        _isTemplateClassCode = true;
                        if (!String.IsNullOrEmpty(tokeniser.Value))
                            AddDoTemplateClassCode(tokeniser.Location, tokeniser.Value);
                        break;
                    case State.Directive:
                        Directive directive = null;
                        string attName = null;
                        while (!skip && tokeniser.Advance())
                        {
                            switch (tokeniser.State)
                            {
                                case State.DirectiveName:
                                    if (directive == null)
                                        directive = new Directive {Name = tokeniser.Value.ToLower()};
                                    else
                                        attName = tokeniser.Value;
                                    break;
                                case State.DirectiveValue:
                                    if (attName != null && directive != null)
                                        directive.Attributes.Add(attName.ToLower(), tokeniser.Value);
                                    attName = null;
                                    break;
                                case State.Directive:
                                    //if (directive != null)
                                    //    directive.EndLocation = tokeniser.TagEndLocation;
                                    break;
                                default:
                                    skip = true;
                                    break;
                            }
                        }
                        if (directive != null)
                        {
                            if (directive.Name == "include")
                            {
                                string includeFile = directive.Attributes["file"];
                                if (OnInclude == null)
                                    throw new InvalidOperationException("Include file found. OnInclude event must be implemented");
                                var includeArgs = new TemplateIncludeArgs() {IncludeName = includeFile};
                                OnInclude(this, includeArgs);
                                Parse(includeArgs.Text ?? "", includeArgs.IncludeName);
                            }
                            _directives.Add(directive);
                        }
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// T4 Directive
        /// </summary>
        private class Directive
        {
            public Directive()
            {
                Attributes = new Dictionary<string, string>();
            }

            public string Name { get; set; }
            public Dictionary<string, string> Attributes { get; set; }
        }
    }
}