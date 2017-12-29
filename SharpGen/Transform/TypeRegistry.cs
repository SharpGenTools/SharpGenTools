using SharpGen.Config;
using SharpGen.Logging;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpGen.Transform
{
    public class TypeRegistry
    {
        private readonly Dictionary<string, (CsTypeBase CSharpType, CsTypeBase MarshalType)> _mapCppNameToCSharpType = new Dictionary<string, (CsTypeBase CSharpType, CsTypeBase MarshalType)>();
        private readonly Dictionary<string, CsTypeBase> _mapDefinedCSharpType = new Dictionary<string, CsTypeBase>();

        private Logger Logger { get; }

        public TypeRegistry(Logger logger)
        {
            Logger = logger;
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
        /// Imports a defined C# type by name.
        /// </summary>
        /// <param name = "typeName">Name of the C# type.</param>
        /// <returns>The C# type base</returns>
        public CsTypeBase ImportType(string typeName)
        {
            if (!_mapDefinedCSharpType.TryGetValue(typeName, out CsTypeBase cSharpType))
            {
                var type = Type.GetType(typeName);
                if (type == null)
                {
                    Logger.Warning("Type [{0}] is not defined", typeName);
                    cSharpType = new CsTypeBase { Name = typeName };
                    DefineType(cSharpType);
                    return cSharpType;
                }
                return ImportType(type);
            }
            return cSharpType;
        }

        public CsTypeBase ImportType(Type type)
        {
            var typeName = type.FullName;
            var sizeOf = 0;
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
            var cSharpType = new CsTypeBase { Name = typeName, Type = type, SizeOf = sizeOf };
            DefineType(cSharpType);
            return cSharpType;
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
                    var oldType = FindBoundType(tag.Replace);
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
                _mapCppNameToCSharpType.Add(cppName, (type, marshalType));
            }
        }

        /// <summary>
        ///   Finds the C# type binded from a C++ type name.
        /// </summary>
        /// <param name = "cppName">Name of a c++ type</param>
        /// <returns>A C# type or null</returns>
        public CsTypeBase FindBoundType(string cppName)
        {
            if (cppName == null)
                return null;
            _mapCppNameToCSharpType.TryGetValue(cppName, out var typeMap);
            return typeMap.CSharpType;
        }

        /// <summary>
        ///   Finds the C# marshal type binded from a C++ typename.
        /// </summary>
        /// <param name = "cppName">Name of a c++ type</param>
        /// <returns>A C# type or null</returns>
        public CsTypeBase FindBoundMarshalType(string cppName)
        {
            if (cppName == null)
                return null;
            _mapCppNameToCSharpType.TryGetValue(cppName, out var typeMap);
            return typeMap.MarshalType;
        }

        public IEnumerable<(string CppType, CsTypeBase CSharpType)> GetTypeBindings()
        {
            return from record in _mapCppNameToCSharpType
                   select (record.Key, record.Value.CSharpType);
        }
    }
}
