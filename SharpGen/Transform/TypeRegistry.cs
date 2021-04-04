using SharpGen.Logging;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpGen.Transform
{
    public partial class TypeRegistry
    {
        private readonly Dictionary<string, (CsTypeBase CSharpType, CsTypeBase MarshalType)> _mapCppNameToCSharpType = new();
        private readonly Dictionary<string, CsTypeBase> _mapDefinedCSharpType = new();

        private Logger Logger { get; }
        private IDocumentationLinker DocLinker { get; }

        public TypeRegistry(Logger logger, IDocumentationLinker docLinker)
        {
            Logger = logger;
            DocLinker = docLinker;
        }

        /// <summary>
        /// Defines and register a C# type.
        /// </summary>
        /// <param name = "type">The C# type.</param>
        public void DefineType(CsTypeBase type) => DefineTypeImpl(type, type.QualifiedName);

        private void DefineTypeImpl(CsTypeBase type, string typeName)
        {
            if (!_mapDefinedCSharpType.ContainsKey(typeName))
                _mapDefinedCSharpType.Add(typeName, type);
        }

        /// <summary>
        /// Imports a defined C# type by name.
        /// </summary>
        /// <param name = "typeName">Name of the C# type.</param>
        /// <returns>The C# type base</returns>
        public CsTypeBase ImportType(string typeName)
        {
            var primitiveType = ImportPrimitiveType(typeName);
            if (primitiveType != null)
                return primitiveType;

            if (_mapDefinedCSharpType.TryGetValue(typeName, out var cSharpType))
                return cSharpType;

            var type = Type.GetType(typeName);

            if (type == null)
            {
                Logger.Warning(LoggingCodes.TypeNotDefined, "Type [{0}] is not defined", typeName);
                cSharpType = new CsUndefinedType(typeName);
                DefineTypeImpl(cSharpType, typeName);
                return cSharpType;
            }

            var transformedTypeName = type.FullName;

            if (transformedTypeName == null)
            {
                Logger.Warning(LoggingCodes.TypeNotDefined, "Type [{0}] has null {1}", typeName, nameof(type.FullName));
                cSharpType = new CsUndefinedType(typeName);
                DefineTypeImpl(cSharpType, typeName);
                return cSharpType;
            }

            if (_mapDefinedCSharpType.TryGetValue(transformedTypeName, out cSharpType))
                return cSharpType;

            cSharpType = new CsFundamentalType(type, transformedTypeName);
            DefineTypeImpl(cSharpType, transformedTypeName);
            return cSharpType;
        }

        public static CsFundamentalType ImportPrimitiveType(string typeName)
        {
            if (typeName == null)
                return null;

            typeName = typeName.Trim();

            if (typeName.Length == 0)
                return null;

            if (PrimitiveTypeEntriesByName.TryGetValue(typeName, out var entry))
                return entry;

            var typeNameParts = typeName.Split(new[] {'*'}, 2, StringSplitOptions.RemoveEmptyEntries);

            var baseTypeName = typeNameParts[0].Trim();

            var pointerCountInt = typeName.Count(x => x == '*');
            var pointerCount = checked((byte) pointerCountInt);

            CsFundamentalType FindOrCreate(PrimitiveTypeCode typeCode)
            {
                PrimitiveTypeIdentity identity = new(typeCode, pointerCount);
                return FindPrimitiveTypeImpl(identity, baseTypeName, typeName);
            }

            switch (pointerCount)
            {
                case 0:
                    break;
                case 1 when typeName == "void*":
                    throw new Exception(
                        $"void* is supposed to have been found in {nameof(PrimitiveTypeEntriesByName)}"
                    );
                default:
                    if (PrimitiveTypeEntriesByName.TryGetValue(baseTypeName, out var baseEntry))
                    {
                        // ReSharper disable once PossibleInvalidOperationException
                        entry = FindOrCreate(baseEntry.PrimitiveTypeIdentity.Value.Type);
                    }

                    break;
            }

            if (entry == null)
            {
                var type = Type.GetType(baseTypeName);

                var baseEntry = PrimitiveRuntimeTypesByCode.Where(x => x.Value == type).Take(1).ToArray();

                if (baseEntry.Length == 1)
                    entry = FindOrCreate(baseEntry[0].Key);
            }

            return entry;
        }

        public static CsFundamentalType ImportPrimitiveType(PrimitiveTypeCode code, byte pointerCount = 0)
        {
            if (pointerCount == 1 && code == PrimitiveTypeCode.Void)
                return VoidPtr;

            PrimitiveTypeIdentity baseIdentity = new(code);
            var baseEntry = PrimitiveTypeEntriesByIdentity[baseIdentity];

            if (pointerCount == 0)
                return baseEntry;

            PrimitiveTypeIdentity identity = new(code, pointerCount);

            return FindPrimitiveTypeImpl(identity, baseEntry.QualifiedName, null);
        }

        private static CsFundamentalType FindPrimitiveTypeImpl(PrimitiveTypeIdentity identity,
                                                               string baseTypeName,
                                                               string requestFullName)
        {
            var pointerCount = identity.PointerCount;
            var processedTypeName = baseTypeName + new string('*', pointerCount);

            if (!PrimitiveTypeEntriesByIdentity.TryGetValue(identity, out var entry))
            {
                var runtimeType = PrimitiveRuntimeTypesByCode[identity.Type];
                for (byte i = 0; i < pointerCount; i++)
                    runtimeType = runtimeType.MakePointerType();

                entry = new CsFundamentalType(runtimeType, identity, processedTypeName);

                PrimitiveTypeEntriesByIdentity.Add(identity, entry);
                PrimitiveTypeEntriesByName.Add(processedTypeName, entry);
            }

            if (!string.IsNullOrEmpty(requestFullName) && requestFullName != processedTypeName)
                // Add alias, like System.Boolean for bool
                PrimitiveTypeEntriesByName.Add(requestFullName, entry);

            return entry;
        }

        /// <summary>
        /// Maps a C++ type name to a C# class
        /// </summary>
        /// <param name = "cppName">Name of the CPP.</param>
        /// <param name = "type">The C# type.</param>
        /// <param name = "marshalType">The C# marshal type</param>
        public void BindType(string cppName, CsTypeBase type, CsTypeBase marshalType = null)
        {
            if (_mapCppNameToCSharpType.TryGetValue(cppName, out var old))
            {
                Logger.Warning(
                    LoggingCodes.DuplicateBinding,
                    "Mapping C++ element [{0}] to CSharp type [{1}/{2}] is already mapped to [{3}/{4}]",
                    cppName, type.CppElementName, type.QualifiedName, old.CSharpType.CppElementName,
                    old.CSharpType.QualifiedName
                );
            }
            else
            {
                _mapCppNameToCSharpType.Add(cppName, (type, marshalType));
                DocLinker.AddOrUpdateDocLink(cppName, type.QualifiedName);
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

        public IEnumerable<(string CppType, CsTypeBase CSharpType, CsTypeBase MarshalType)> GetTypeBindings()
        {
            return from record in _mapCppNameToCSharpType
                   select (record.Key, record.Value.CSharpType, record.Value.MarshalType);
        }
    }
}
