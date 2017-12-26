using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpGen.Generator
{
    public class NamespaceRegistry
    {
        private readonly AssemblyManager manager;
        private readonly Dictionary<string, CsNamespace> _mapIncludeToNamespace = new Dictionary<string, CsNamespace>();
        private readonly Dictionary<Regex, CsNamespace> _mapTypeToNamespace = new Dictionary<Regex, CsNamespace>();

        public Logger Logger { get; }

        public NamespaceRegistry(Logger logger, AssemblyManager manager)
        {
            Logger = logger;
            this.manager = manager;
        }

        /// <summary>
        /// Maps a particular C++ include to a C# namespace.
        /// </summary>
        /// <param name="includeName">Name of the include.</param>
        /// <param name="assemblyName">The assembly.</param>
        /// <param name="nameSpace">The namespace.</param>
        /// <param name="outputDirectory">The output directory for the namespace.</param>
        public void MapIncludeToNamespace(string includeName, string assemblyName, string nameSpace, string outputDirectory)
        {
            var cSharpNamespace = manager.GetOrCreateNamespace(assemblyName, nameSpace);
            if (outputDirectory != null)
                cSharpNamespace.OutputDirectory = outputDirectory;
            _mapIncludeToNamespace.Add(includeName, cSharpNamespace);
        }

        /// <summary>
        /// Attaches C++ to a C# namespace using a regular expression query.
        /// </summary>
        /// <param name="typeNameRegex">The C++ regex selection.</param>
        /// <param name="assemblyName">The assembly.</param>
        /// <param name="namespaceName">The namespace.</param>
        /// <param name="outputDirectory">The output directory for the namespace.</param>
        public void AttachTypeToNamespace(string typeNameRegex, string assemblyName, string namespaceName, string outputDirectory)
        {
            var cSharpNamespace = manager.GetOrCreateNamespace(assemblyName, namespaceName);
            if (outputDirectory != null)
                cSharpNamespace.OutputDirectory = outputDirectory;
            _mapTypeToNamespace.Add(new Regex(typeNameRegex), cSharpNamespace);
        }

        public bool TryGetNamespaceForInclude(string includeName, out CsNamespace cSharpNamespace)
        {
            return _mapIncludeToNamespace.TryGetValue(includeName, out cSharpNamespace);
        }

        public (bool match, CsNamespace nameSpace) GetCsNamespaceForCppElement(CppElement element)
        {
            foreach (var regExp in _mapTypeToNamespace)
            {
                if (regExp.Key.Match(element.Name).Success)
                    return (true, regExp.Value);
            }
            return (false, default);
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
                return manager.GetOrCreateNamespace(tag.Assembly, tag.Namespace);
            }

            var namespaceFromElementName = GetCsNamespaceForCppElement(element);
            if (namespaceFromElementName.match)
            {
                return namespaceFromElementName.nameSpace;
            }

            if (!TryGetNamespaceForInclude(element.ParentInclude.Name, out CsNamespace ns))
            {
                Logger.Fatal("Unable to find namespace for element [{0}] from include [{1}]", element, element.ParentInclude.Name);
            }
            return ns;
        }
    }
}
