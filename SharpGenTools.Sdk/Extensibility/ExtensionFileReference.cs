// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using SharpGen.Doc;
using SharpGenTools.Sdk.Internal;
using SharpGenTools.Sdk.Internal.Roslyn;

namespace SharpGenTools.Sdk.Extensibility;

/// <summary>
/// Represents analyzers stored in an analyzer assembly file.
/// </summary>
/// <remarks>
/// Analyzer are read from the file, owned by the reference, and doesn't change 
/// since the reference is accessed until the reference object is garbage collected.
/// </remarks>
internal sealed class ExtensionFileReference : ExtensionReference, IEquatable<ExtensionReference>
{
    private const string DotDelimiterString = ".";
    private static readonly string DocProviderNamespace = typeof(IDocProvider).Namespace!;
    private const string DocProviderName = nameof(IDocProvider);

    private delegate bool ExtensionPredicate(ModuleMetadata module, InterfaceImplementation interfaceImpl);

    public override string FullPath { get; }

    private readonly Extensions<IDocProvider> _docProviders;

    private string? _lazyDisplay;
    private object? _lazyIdentity;
    private Assembly? _lazyAssembly;

    public event EventHandler<ExtensionLoadFailureEventArgs>? AnalyzerLoadFailed;

    /// <summary>
    /// Creates an ExtensionFileReference with the given <paramref name="fullPath"/> and <paramref name="assemblyLoader"/>.
    /// </summary>
    /// <param name="fullPath">Full path of the analyzer assembly.</param>
    /// <param name="assemblyLoader">Loader for obtaining the <see cref="Assembly"/> from the <paramref name="fullPath"/></param>
    public ExtensionFileReference(string fullPath, ExtensibilityAssemblyLoader assemblyLoader)
    {
        Utilities.RequireAbsolutePath(fullPath, nameof(fullPath));

        FullPath = fullPath;
        AssemblyLoader = assemblyLoader ?? throw new ArgumentNullException(nameof(assemblyLoader));

        _docProviders = new Extensions<IDocProvider>(this, IsDocProviderPredicate);

        // Note this analyzer full path as a dependency location, so that the analyzer loader
        // can correctly load analyzer dependencies.
        assemblyLoader.AddDependencyLocation(fullPath);
    }

    public ExtensibilityAssemblyLoader AssemblyLoader { get; }

    public override bool Equals(object? obj)
        => Equals(obj as ExtensionFileReference);

    public bool Equals(ExtensionFileReference? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        return other != null &&
               ReferenceEquals(AssemblyLoader, other.AssemblyLoader) &&
               FullPath == other.FullPath;
    }

    // legacy, for backwards compat:
    public bool Equals(ExtensionReference? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        return other switch
        {
            null => false,
            ExtensionFileReference fileReference => Equals(fileReference),
            _ => FullPath == other.FullPath
        };
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(RuntimeHelpers.GetHashCode(AssemblyLoader), FullPath.GetHashCode());
    }

    public override ImmutableArray<IDocProvider> GetDocumentationProviders()
    {
        return _docProviders.GetExtensionsForAllLanguages();
    }

    public override string Display
    {
        get
        {
            if (_lazyDisplay == null)
            {
                InitializeDisplayAndId();
            }

            return _lazyDisplay;
        }
    }

    public override object Id
    {
        get
        {
            if (_lazyIdentity == null)
            {
                InitializeDisplayAndId();
            }

            return _lazyIdentity;
        }
    }

    [MemberNotNull(nameof(_lazyIdentity), nameof(_lazyDisplay))]
    private void InitializeDisplayAndId()
    {
        try
        {
            // AssemblyName.GetAssemblyName(path) is not available on CoreCLR.
            // Use our metadata reader to do the equivalent thing.
            using var reader = new PEReader(Utilities.OpenRead(FullPath));

            var metadataReader = reader.GetMetadataReader();
            var assemblyIdentity = ReadAssemblyIdentityOrThrow(metadataReader);
            _lazyDisplay = assemblyIdentity.Name;
            _lazyIdentity = assemblyIdentity;
        }
        catch
        {
            _lazyDisplay = FileNameUtilities.GetFileName(FullPath, false);
            _lazyIdentity = _lazyDisplay;
        }
    }

    /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
    private static AssemblyIdentity ReadAssemblyIdentityOrThrow(MetadataReader reader)
    {
        var assemblyDef = reader.GetAssemblyDefinition();

        return CreateAssemblyIdentityOrThrow(reader,
                                             assemblyDef.Version,
                                             assemblyDef.Flags,
                                             assemblyDef.PublicKey,
                                             assemblyDef.Name,
                                             assemblyDef.Culture,
                                             false);
    }

    /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
    private static AssemblyIdentity CreateAssemblyIdentityOrThrow(
        MetadataReader reader,
        Version version,
        AssemblyFlags flags,
        BlobHandle publicKey,
        StringHandle name,
        StringHandle culture,
        bool isReference)
    {
        string nameStr = reader.GetString(name);
        var cultureName = culture.IsNil ? null : reader.GetString(culture);

        var publicKeyOrToken = reader.GetBlobContent(publicKey);
        bool hasPublicKey;

        if (isReference)
        {
            hasPublicKey = (flags & AssemblyFlags.PublicKey) != 0;
        }
        else
        {
            // Assembly definitions never contain a public key token, they only can have a full key or nothing,
            // so the flag AssemblyFlags.PublicKey does not make sense for them and is ignored.
            // See Ecma-335, Partition II Metadata, 22.2 "Assembly : 0x20".
            // This also corresponds to the behavior of the native C# compiler and sn.exe tool.
            hasPublicKey = !publicKeyOrToken.IsEmpty;
        }

        if (publicKeyOrToken.IsEmpty)
        {
            publicKeyOrToken = default;
        }

        return new AssemblyIdentity(
            nameStr,
            version,
            cultureName,
            publicKeyOrToken,
            hasPublicKey,
            (flags & AssemblyFlags.Retargetable) != 0,
            (AssemblyContentType) ((int) (flags & AssemblyFlags.ContentTypeMask) >> 9));
    }

    /// <summary>
    /// Adds the <see cref="ImmutableArray{T}"/> of <see cref="IDocProvider"/> defined in this assembly reference.
    /// </summary>
    internal void AddDocumentationProviders(ImmutableArray<IDocProvider>.Builder builder)
    {
        _docProviders.AddExtensions(builder);
    }

    private static ExtensionLoadFailureEventArgs CreateExtensionLoadFailedArgs(Exception e, string? typeNameOpt = null)
    {
        // unwrap:
        e = e as TargetInvocationException ?? e;

        // remove all line breaks from the exception message
        string message = e.Message.Replace("\r", string.Empty).Replace("\n", string.Empty);

        var errorCode = typeNameOpt != null
                            ? ExtensionLoadFailureEventArgs.FailureErrorCode.UnableToCreateExtension
                            : ExtensionLoadFailureEventArgs.FailureErrorCode.UnableToLoadExtension;

        return new ExtensionLoadFailureEventArgs(errorCode, message, e, typeNameOpt);
    }

    /// <summary>
    /// Opens the analyzer dll with the metadata reader and builds a map of language -> analyzer type names.
    /// </summary>
    /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
    /// <exception cref="IOException">IO error reading the metadata.</exception>
    private static ImmutableHashSet<string> GetAnalyzerTypeNameMap(string fullPath,
                                                                   ExtensionPredicate extensionPredicate)
    {
        using var assembly = AssemblyMetadata.CreateFromFile(fullPath);

        // This is longer than strictly necessary to avoid thrashing the GC with string allocations
        // in the call to GetFullyQualifiedTypeNames. Specifically, this checks for the presence of
        // supported languages prior to creating the type names.
        return (from module in assembly.GetModules()
                from typeDefHandle in module.GetMetadataReader().TypeDefinitions
                let typeDef = module.GetMetadataReader().GetTypeDefinition(typeDefHandle)
                where GetSupportedLanguages(typeDef, module, extensionPredicate)
                select GetFullyQualifiedTypeName(typeDef, module)).ToImmutableHashSet();
    }

    private static bool GetSupportedLanguages(TypeDefinition typeDef, ModuleMetadata module,
                                              ExtensionPredicate extensionPredicate) =>
        typeDef.GetInterfaceImplementations()
               .Select(x => module.GetMetadataReader().GetInterfaceImplementation(x))
               .Any(customAttrHandle => extensionPredicate(module, customAttrHandle));

    private static bool IsDocProviderPredicate(ModuleMetadata module, InterfaceImplementation interfaceImpl)
    {
        if (!GetTypeNamespaceAndName(module.GetMetadataReader(), interfaceImpl.Interface,
                                     out var namespaceHandle, out var nameHandle))
            return false;

        var comparer = module.GetMetadataReader().StringComparer;
        return comparer.Equals(nameHandle, DocProviderName) &&
               comparer.Equals(namespaceHandle, DocProviderNamespace);
    }

    /// <summary>
    /// Given a token for a type, return the type's name and namespace.  Only works for top level types. 
    /// namespaceHandle will be NamespaceDefinitionHandle for defs and StringHandle for refs. 
    /// </summary>
    /// <returns>True if the function successfully returns the name and namespace.</returns>
    private static bool GetTypeNamespaceAndName(MetadataReader metadataReader, EntityHandle typeDefOrRef,
                                                out StringHandle namespaceHandle, out StringHandle nameHandle)
    {
        nameHandle = default;
        namespaceHandle = default;

        try
        {
            return typeDefOrRef.Kind switch
            {
                HandleKind.TypeReference => GetTypeRefNamespaceAndName(
                    (TypeReferenceHandle) typeDefOrRef, ref namespaceHandle, ref nameHandle
                ),
                HandleKind.TypeDefinition => GetTypeDefNamespaceAndName(
                    (TypeDefinitionHandle) typeDefOrRef, ref namespaceHandle, ref nameHandle
                ),
                _ => false
            };
        }
        catch (BadImageFormatException)
        {
            return false;
        }

        bool GetTypeDefNamespaceAndName(TypeDefinitionHandle typeDefHandle, ref StringHandle namespaceHandle,
                                        ref StringHandle nameHandle)
        {
            var def = metadataReader.GetTypeDefinition(typeDefHandle);

            if (IsNested(def.Attributes))
            {
                // TODO - Support nested types. 
                return false;
            }

            nameHandle = def.Name;
            namespaceHandle = def.Namespace;
            return true;

            static bool IsNested(TypeAttributes flags) =>
                (flags & TypeAttributes.NestedFamANDAssem) != 0;
        }

        bool GetTypeRefNamespaceAndName(TypeReferenceHandle typeRefHandle, ref StringHandle namespaceHandle,
                                        ref StringHandle nameHandle)
        {
            var typeRefRow = metadataReader.GetTypeReference(typeRefHandle);
            var handleType = typeRefRow.ResolutionScope.Kind;

            if (handleType == HandleKind.TypeReference || handleType == HandleKind.TypeDefinition)
            {
                // TODO - Support nested types.  
                return false;
            }

            nameHandle = typeRefRow.Name;
            namespaceHandle = typeRefRow.Namespace;
            return true;
        }
    }

    private static string GetFullyQualifiedTypeName(TypeDefinition typeDef, ModuleMetadata module)
    {
        var declaringType = typeDef.GetDeclaringType();

        // Non nested type - simply get the full name
        if (declaringType.IsNil)
            return GetFullNameOrThrow(module, typeDef.Namespace, typeDef.Name);

        var declaringTypeDef = module.GetMetadataReader().GetTypeDefinition(declaringType);
        return GetFullyQualifiedTypeName(declaringTypeDef, module) + "+" +
               module.GetMetadataReader().GetString(typeDef.Name);
    }

    /// <exception cref="BadImageFormatException">An exception from metadata reader.</exception>
    private static string GetFullNameOrThrow(ModuleMetadata module, StringHandle namespaceHandle, StringHandle nameHandle)
    {
        var attributeTypeName = module.GetMetadataReader().GetString(nameHandle);
        var attributeTypeNamespaceName = module.GetMetadataReader().GetString(namespaceHandle);

        return BuildQualifiedName(attributeTypeNamespaceName, attributeTypeName);

        static string BuildQualifiedName(string qualifier, string name)
        {
            Debug.Assert(name != null);

            return string.IsNullOrEmpty(qualifier) ? name! : string.Concat(qualifier, DotDelimiterString, name);
        }
    }

    private sealed class Extensions<TExtension> where TExtension : class
    {
        private readonly ExtensionFileReference _reference;
        private readonly ExtensionPredicate _extensionPredicate;
        private ImmutableArray<TExtension> _lazyAllExtensions;
        private ImmutableHashSet<string>? _lazyExtensionTypeNameMap;

        internal Extensions(ExtensionFileReference reference, ExtensionPredicate extensionPredicate)
        {
            _reference = reference;
            _extensionPredicate = extensionPredicate;
            _lazyAllExtensions = default;
        }

        internal ImmutableArray<TExtension> GetExtensionsForAllLanguages()
        {
            if (_lazyAllExtensions.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(ref _lazyAllExtensions, CreateExtensionsForAllLanguages(this));
            }

            return _lazyAllExtensions;
        }

        private static ImmutableArray<TExtension> CreateExtensionsForAllLanguages(Extensions<TExtension> extensions)
        {
            // Get all analyzers in the assembly.
            var builder = ImmutableArray.CreateBuilder<TExtension>();
            extensions.AddExtensions(builder);
            return builder.ToImmutable();
        }

        internal ImmutableHashSet<string> GetExtensionTypeNameMap()
        {
            if (_lazyExtensionTypeNameMap == null)
            {
                var analyzerTypeNameMap = GetAnalyzerTypeNameMap(_reference.FullPath, _extensionPredicate);
                Interlocked.CompareExchange(ref _lazyExtensionTypeNameMap, analyzerTypeNameMap, null);
            }

            return _lazyExtensionTypeNameMap;
        }

        internal void AddExtensions(ImmutableArray<TExtension>.Builder builder)
        {
            ImmutableHashSet<string> analyzerTypeNameMap;
            Assembly analyzerAssembly;

            try
            {
                analyzerTypeNameMap = GetExtensionTypeNameMap();
                if (analyzerTypeNameMap.Count == 0)
                {
                    return;
                }

                analyzerAssembly = _reference.GetAssembly();
            }
            catch (Exception e)
            {
                _reference.AnalyzerLoadFailed?.Invoke(_reference, CreateExtensionLoadFailedArgs(e));
                return;
            }

            var initialCount = builder.Count;
            var reportedError = false;

            GetAnalyzersForTypeNames(analyzerAssembly, analyzerTypeNameMap, builder, ref reportedError);

            // If there were types implementing TExtension but couldn't be cast to TExtension, generate a diagnostic.
            // If we've reported errors already while trying to instantiate types, don't complain.
            if (builder.Count == initialCount && !reportedError)
            {
                _reference.AnalyzerLoadFailed?.Invoke(
                    _reference,
                    new ExtensionLoadFailureEventArgs(
                        ExtensionLoadFailureEventArgs.FailureErrorCode.InternalExtensionEntryPointCastError,
                        "Internal extensibility error: extension entry point cannot be casted to a target extension type"
                    )
                );
            }
        }

        private void GetAnalyzersForTypeNames(Assembly analyzerAssembly, IEnumerable<string> analyzerTypeNames,
                                              ImmutableArray<TExtension>.Builder builder, ref bool reportedError)
        {
            // Given the type names, get the actual System.Type and try to create an instance of the type through reflection.
            foreach (var typeName in analyzerTypeNames)
            {
                Type? type;
                try
                {
                    type = analyzerAssembly.GetType(typeName, true, false);
                }
                catch (Exception e)
                {
                    _reference.AnalyzerLoadFailed?.Invoke(_reference, CreateExtensionLoadFailedArgs(e, typeName));
                    reportedError = true;
                    continue;
                }

                Debug.Assert(type != null);

                TExtension? analyzer;
                try
                {
                    analyzer = Activator.CreateInstance(type) as TExtension;
                }
                catch (Exception e)
                {
                    _reference.AnalyzerLoadFailed?.Invoke(_reference, CreateExtensionLoadFailedArgs(e, typeName));
                    reportedError = true;
                    continue;
                }

                if (analyzer != null)
                {
                    builder.Add(analyzer);
                }
            }
        }
    }

    public Assembly GetAssembly()
    {
        if (_lazyAssembly == null)
        {
            _lazyAssembly = AssemblyLoader.LoadFromPath(FullPath);
        }

        return _lazyAssembly;
    }
}