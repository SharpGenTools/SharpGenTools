using SharpGen.CppModel;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpGen.Generator
{
    class NamespaceRegistry
    {
        private readonly Dictionary<string, CsNamespace> _mapIncludeToNamespace = new Dictionary<string, CsNamespace>();
        private readonly Dictionary<Regex, CsNamespace> _mapTypeToNamespace = new Dictionary<Regex, CsNamespace>();

        /// <summary>
        /// Maps a particular C++ include to a C# namespace.
        /// </summary>
        /// <param name="includeName">Name of the include.</param>
        /// <param name="cSharpNamespace">The name space.</param>
        public void MapIncludeToNamespace(string includeName, CsNamespace cSharpNamespace)
        {
            _mapIncludeToNamespace.Add(includeName, cSharpNamespace);
        }

        /// <summary>
        /// Attaches C++ to a C# namespace using a regular expression query.
        /// </summary>
        /// <param name="typeNameRegex">The C++ regex selection.</param>
        /// <param name="cSharpNamespace">The namespace.</param>
        public void AttachTypeToNamespace(string typeNameRegex, CsNamespace cSharpNamespace)
        {
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
    }
}
