using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ClangSharp;
using ClangSharp.Interop;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Logging;
using SharpGen.Parser;
using SharpGen.Platform.Clang;

namespace SharpGen.Platform
{
    public sealed class ClangSharpParser : IClangSharpHost
    {
        private static readonly Regex ArrayTypeRegex = new(@"\[(.+?)\]", RegexOptions.Compiled | RegexOptions.Singleline); 
        private CppModule _group;
        private readonly HashSet<string> _includeToProcess = new();
        private readonly Dictionary<string, bool> _includeIsAttached = new();
        private readonly Dictionary<string, HashSet<string>> _includeAttachedTypes = new();
        private readonly HashSet<string> _boundTypes = new();
        private readonly ConfigFile _configRoot;
        private CppInclude _currentCppInclude;
        private readonly Dictionary<string, int> _mapIncludeToAnonymousEnumCount = new();
        private IncludeDirectoryResolver _includeDirectoryResolver;
        private readonly ClangSharpStreamProvider streamProvider = new();
        private PInvokeGenerator _generator;
        /*
         * 
                    case BaseFieldDeclarationSyntax baseFieldDeclarationSyntax:
                        break;
                    case BaseMethodDeclarationSyntax baseMethodDeclarationSyntax:
                        break;
                    case BasePropertyDeclarationSyntax basePropertyDeclarationSyntax:
                        break;
                    case EnumDeclarationSyntax enumDeclarationSyntax:
                        break;
                    case TypeDeclarationSyntax syntax:
                        Parse(syntax);
                        break;
         */
        private readonly List<TypeDeclarationSyntax> typesTest = new(), typesStatic = new();
        private readonly List<StructDeclarationSyntax> types = new(), typesWithVtbl = new();
        private readonly List<EnumDeclarationSyntax> enums = new(), enumsTest = new();

        private static readonly RoslynAttribute NativeTypeName = new("NativeTypeName", 1);
        private static readonly RoslynAttribute SourceLocation = new("SourceLocation", 3);
        private static readonly RoslynAttribute NativeInheritance = new("NativeInheritance", 1);
        private static readonly RoslynAttribute HasVtbl = new("HasVtbl", 0);
        private static readonly RoslynAttribute DllImport = new("DllImport", null);
        private static readonly RoslynAttribute NativeTypeLayout = new("NativeTypeLayout", 5);

        private static readonly RoslynAttribute[] TrimAttributes =
        {
            NativeTypeName,
            SourceLocation,
            NativeInheritance,
            HasVtbl,
            NativeTypeLayout
        };

        private static readonly PInvokeGeneratorConfiguration GeneratorConfiguration = new(
            "libSharpGenGenerated",
            "SharpGenGenerated",
            options: PInvokeGeneratorConfigurationOptions.GenerateMacroBindings |
                     PInvokeGeneratorConfigurationOptions.GeneratePreviewCode |
                     PInvokeGeneratorConfigurationOptions.GenerateMultipleFiles |
                     PInvokeGeneratorConfigurationOptions.GenerateCppAttributes |
                     PInvokeGeneratorConfigurationOptions.GenerateVtblIndexAttribute |
                     PInvokeGeneratorConfigurationOptions.GenerateNativeInheritanceAttribute |
                     PInvokeGeneratorConfigurationOptions.GenerateAggressiveInlining |
                     PInvokeGeneratorConfigurationOptions.LogVisitedFiles
        );

        /// <summary>
        /// Initializes a new instance of the <see cref="CppParser"/> class.
        /// </summary>
        public ClangSharpParser(Logger logger, ConfigFile configRoot, IncludeDirectoryResolver resolver)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configRoot = configRoot ?? throw new ArgumentNullException(nameof(configRoot));
            _includeDirectoryResolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            Initialize();
        }

        public string OutputPath { get; set; }

        public Logger Logger { get; }

        public Dictionary<string, int> IncludeMacroCounts { get; } = new();

        private void Initialize()
        {
            foreach (var bindRule in _configRoot.ConfigFilesLoaded.SelectMany(cfg => cfg.Bindings))
            {
                if (_boundTypes.Contains(bindRule.From))
                {
                    Logger.Warning(LoggingCodes.DuplicateBinding,
                                   $"Duplicate type bind for [{bindRule.From}] specified. First binding takes priority.");
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

        private string RootConfigHeaderFileName => Path.Combine(OutputPath, _configRoot.HeaderFileName);

        /// <summary>
        /// Runs this instance.
        /// </summary>
        /// <returns></returns>
        public CppModule Run(CppModule groupSkeleton)
        {
            _group = groupSkeleton;
            Logger.Message("Config files changed.");

            // TODO: change
            const string progressMessage = "Parsing C++ headers starts, please wait...";

            var translationFlags = CXTranslationUnit_Flags.CXTranslationUnit_None |
                                   CXTranslationUnit_Flags.CXTranslationUnit_IncludeAttributedTypes |
                                   CXTranslationUnit_Flags.CXTranslationUnit_VisitImplicitAttributes |
                                   CXTranslationUnit_Flags.CXTranslationUnit_DetailedPreprocessingRecord;

            var language = "c++";
            var std = "c++17";

            var clangCommandLineArgs = new[]
            {
                "--verbose",
                "--extra-warnings",
                "-Wextra",
                "-Wpedantic",
                $"--language={language}",         // Treat subsequent input files as having type <language>
                $"--std={std}",                   // Language standard to compile for
                "-Wno-pragma-once-outside-header" // We are processing files which may be header files
            };

            clangCommandLineArgs = clangCommandLineArgs
                                  .Concat(
                                       _includeDirectoryResolver.IncludePaths.Select(
                                           x => "--include-directory=" + x.Path
                                       )
                                   ).ToArray();

            try
            {
                Logger.Progress(15, progressMessage);

                if (_generator != null)
                {
                    Logger.Error("TODO", $"Error TODO");
                }

                _generator = new PInvokeGenerator(
                    GeneratorConfiguration,
                    this
                );

                var filePath = RootConfigHeaderFileName;
                var translationUnitError = CXTranslationUnit.TryParse(
                    _generator.IndexHandle, filePath, clangCommandLineArgs,
                    Array.Empty<CXUnsavedFile>(), translationFlags, out var handle
                );
                var skipProcessing = false;

                if (translationUnitError != CXErrorCode.CXError_Success)
                {
                    // TODO
                    Logger.Error(
                        "TODO", $"Error: Parsing failed for '{filePath}' due to '{translationUnitError}'.");
                    skipProcessing = true;
                }
                else if (handle.NumDiagnostics != 0)
                {
                    Logger.Message($"Diagnostics for '{filePath}':");

                    for (uint i = 0; i < handle.NumDiagnostics; ++i)
                    {
                        using var diagnostic = handle.GetDiagnostic(i);

                        Logger.Message("    " + diagnostic.Format(CXDiagnostic.DefaultDisplayOptions));

                        skipProcessing |= diagnostic.Severity == CXDiagnosticSeverity.CXDiagnostic_Error;
                        skipProcessing |= diagnostic.Severity == CXDiagnosticSeverity.CXDiagnostic_Fatal;
                    }
                }

                if (skipProcessing)
                {
                    Logger.Fatal($"Skipping '{filePath}' due to one or more errors listed above.");
                    return _group;
                }

                try
                {
                    using var translationUnit = TranslationUnit.GetOrCreate(handle);
                    Logger.Message($"Processing '{filePath}'");

                    _generator.GenerateBindings(translationUnit, filePath, clangCommandLineArgs, translationFlags);
                }
                catch (Exception e)
                {
                    // TODO
                    Logger.Error(null, "TODO", e);
                }

                if (Logger.HasErrors)
                    return _group;

                if (_generator.Diagnostics.Count != 0)
                {
                    Logger.Message("Diagnostics for binding generation:");

                    foreach (var diagnostic in _generator.Diagnostics)
                    {
                        Logger.Message("    " + diagnostic);

#if false
                        if (diagnostic.Level == DiagnosticLevel.Warning)
                        {
                            if (exitCode >= 0)
                            {
                                exitCode++;
                            }
                        }
                        else if (diagnostic.Level == DiagnosticLevel.Error)
                        {
                            if (exitCode >= 0)
                            {
                                exitCode = -1;
                            }
                            else
                            {
                                exitCode--;
                            }
                        }
#endif
                    }
                }

                if (Logger.HasErrors)
                    return _group;

                _generator.Dispose();
                _generator = null;

                Logger.Progress(30, progressMessage);
            }
            catch (Exception ex)
            {
                Logger.Error(null, "Unexpected error", ex);
            }
            finally
            {
                _generator?.Dispose();
                _generator = null;

                Logger.Message("Parsing headers is finished.");
            }

            if (Logger.HasErrors)
                return _group;

            try
            {
                foreach (var configFile in streamProvider.Streams)
                {
                    ParseRoslyn(configFile.IsTestOutput, configFile.Stream);
                }

                if (Logger.HasErrors)
                    return _group;

                // TODO
                Logger.Progress(30, progressMessage);
            }
            catch (Exception ex)
            {
                Logger.Error(null, "Unexpected error", ex);
            }
            finally
            {
                Logger.Message("Parsing ClangSharp-generated code is finished.");
            }

            try
            {
                streamProvider.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Error(null, "Unexpected error", ex);
            }
            finally
            {
                Logger.Message("Resetting streams finished.");
            }

            try
            {
                foreach (var enumDeclaration in enums)
                    ParseDoTypeDefinitionPass(enumDeclaration);
                foreach (var typeDeclaration in types)
                    ParseDoTypeDefinitionPass(typeDeclaration);
                foreach (var typeDeclaration in typesWithVtbl)
                    ParseDoTypeDefinitionPassWithVtbl(typeDeclaration);
                foreach (var typeDeclaration in typesStatic)
                    ParseDoTypeDefinitionPassForStaticType(typeDeclaration);
                foreach (var cppStruct in _group.Includes.SelectMany(x => x.Items.OfType<CppStruct>()))
                    ParseDoItemDefinitionPass(cppStruct);
                foreach (var cppInterface in _group.Includes.SelectMany(x => x.Items.OfType<CppInterface>()))
                    ParseDoItemDefinitionPass(cppInterface);
                foreach (var cppEnum in _group.Includes.SelectMany(x => x.Items.OfType<CppEnum>()))
                    ParseDoItemDefinitionPass(cppEnum);
            }
            catch (Exception ex)
            {
                Logger.Error(null, "Unexpected error", ex);
            }
            finally
            {
                // TODO
                Logger.Message("TODO");
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

        public Stream GetOutputStream(bool isTestOutput, string name, string extension) =>
            streamProvider.GetOutputStream(isTestOutput, name, extension);

        public bool IsIncludedFileOrLocation(Cursor cursor, CXFile file, CXSourceLocation location)
        {
            var includeId = GetIncludeIdFromFilePath(file.Name.ToString());

            // Process only files listed inside the config files
            if (!_includeToProcess.Contains(includeId))
                return false;

            // Process only files attached (fully or partially) to an assembly/namespace
            if (!_includeIsAttached.TryGetValue(includeId, out var isIncludeFullyAttached))
                return false;

            if (isIncludeFullyAttached)
                return true;

            string qualifiedName;
            string name;

            switch (cursor)
            {
                case NamedDecl namedDecl:
                    // We get the non-remapped name for the purpose of exclusion checks to ensure that users
                    // can remove no-definition declarations in favor of remapped anonymous declarations.

                    qualifiedName = _generator.GetCursorQualifiedName(namedDecl);
                    name = _generator.GetCursorName(namedDecl);
                    break;
                case MacroDefinitionRecord macroDefinitionRecord:
                    qualifiedName = macroDefinitionRecord.Name;
                    name = macroDefinitionRecord.Name;
                    break;
                default:
                    return false;
            }

            // If this include is partially attached and the current type is not attached
            // Than skip it, as we are not mapping it
            var attachedType = _includeAttachedTypes[includeId];
            return attachedType.Contains(qualifiedName) || attachedType.Contains(name);
        }

        private void ParseRoslyn(bool isTestOutput, Stream reader)
        {
            var sourceText = SourceText.From(reader);
            var syntaxTree = CSharpSyntaxTree.ParseText(
                sourceText,
                CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview)
            );
            var syntaxRoot = syntaxTree.GetRoot();
            foreach (var node in syntaxRoot.DescendantNodesAndSelf(x => x is not NamespaceDeclarationSyntax)
                                           .OfType<NamespaceDeclarationSyntax>().SelectMany(x => x.Members))
            {
                switch (node)
                {
                    case EnumDeclarationSyntax enumSyntax when isTestOutput:
                        enumsTest.Add(enumSyntax);
                        break;
                    case EnumDeclarationSyntax enumSyntax:
                        enums.Add(enumSyntax);
                        break;
                    case TypeDeclarationSyntax typeSyntax when isTestOutput:
                        typesTest.Add(typeSyntax);
                        break;
                    case TypeDeclarationSyntax typeSyntax when typeSyntax.Modifiers.Any(SyntaxKind.StaticKeyword):
                        typesStatic.Add(typeSyntax);
                        break;
                    case StructDeclarationSyntax typeSyntax when HasVtbl.IsInAttributeList(typeSyntax):
                        typesWithVtbl.Add(typeSyntax);
                        break;
                    case StructDeclarationSyntax typeSyntax:
                        types.Add(typeSyntax);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(node));
                }
            }
        }

        private void ParseDoTypeDefinitionPass(StructDeclarationSyntax typeSyntax)
        {
            var include = GetInclude(typeSyntax);

            if (include == null)
                return;

            Debug.Assert(!typeSyntax.Modifiers.Any(SyntaxKind.StaticKeyword));
            Debug.Assert(!HasVtbl.IsInAttributeList(typeSyntax));

            CppStruct node = new(typeSyntax.Identifier.ValueText, typeSyntax)
            {
                Base = GetNativeInheritanceFromSyntax(typeSyntax)
            };

            var layoutAttributeList = NativeTypeLayout.GetAttributeFromAttributeList(typeSyntax);
            if (layoutAttributeList is not {} layoutAttribute)
            {
                throw new Exception();
            }

            node.Alignment32 = GetAttributeValueLongLiteral(GetAttributeValueByName(layoutAttribute, nameof(CppStruct.Alignment32))).Value;
            node.Alignment64 = GetAttributeValueLongLiteral(GetAttributeValueByName(layoutAttribute, nameof(CppStruct.Alignment64))).Value;
            node.Size32 = GetAttributeValueLongLiteral(GetAttributeValueByName(layoutAttribute, nameof(CppStruct.Size32))).Value;
            node.Size64 = GetAttributeValueLongLiteral(GetAttributeValueByName(layoutAttribute, nameof(CppStruct.Size64))).Value;
            node.Pack = GetAttributeValueLongLiteral(GetAttributeValueByName(layoutAttribute, nameof(CppStruct.Pack))).Value;

            include.Add(node);
        }

        private void ParseDoTypeDefinitionPassWithVtbl(StructDeclarationSyntax typeSyntax)
        {
            var include = GetInclude(typeSyntax);

            if (include == null)
                return;

            Debug.Assert(!typeSyntax.Modifiers.Any(SyntaxKind.StaticKeyword));
            Debug.Assert(HasVtbl.IsInAttributeList(typeSyntax));

            CppInterface node = new(typeSyntax.Identifier.ValueText, typeSyntax)
            {
                Base = GetNativeInheritanceFromSyntax(typeSyntax)
            };

            include.Add(node);
        }

        private void ParseDoTypeDefinitionPassForStaticType(TypeDeclarationSyntax typeSyntax)
        {
            Debug.Assert(typeSyntax.Modifiers.Any(SyntaxKind.StaticKeyword));
            Debug.Assert(!HasVtbl.IsInAttributeList(typeSyntax));

            foreach (var member in typeSyntax.Members)
            {
                var include = GetInclude(member);

                if (include == null)
                    return;

                switch (member)
                {
                    case MethodDeclarationSyntax methodSyntax when DllImport.GetAttributeFromAttributeList(methodSyntax) is {} dllImport:
                        var entryPoint = GetDllImportEntryPointFromSyntax(methodSyntax, dllImport);
                        CppFunction function = new(entryPoint);

                        if (GetDllImportCallingConventionFromSyntax(dllImport) is {Length: > 0} callConvName)
                            function.CallingConvention = callConvName.ToLowerInvariant() switch
                            {
                                "winapi" => CallingConvention.Winapi,
                                "cdecl" => CallingConvention.Cdecl,
                                "stdcall" => CallingConvention.StdCall,
                                "thiscall" => CallingConvention.ThisCall,
                                "fastcall" => CallingConvention.FastCall,
                            };

                        if (!methodSyntax.ReturnType.IsKind(SyntaxKind.VoidKeyword))
                        {
                            CppReturnValue returnValue = new();

                            ResolveAndFillType(
                                methodSyntax.ReturnType, GetNativeTypeNameFromSyntax(methodSyntax), returnValue
                            );

                            function.ReturnValue = returnValue;
                        }

                        foreach (var parameterSyntax in methodSyntax.ParameterList.Parameters)
                        {
                            CppParameter parameter = new(parameterSyntax.Identifier.ValueText);

                            ResolveAndFillType(
                                parameterSyntax.Type, GetNativeTypeNameFromSyntax(parameterSyntax), parameter
                            );

                            function.Add(parameter);
                        }

                        include.Add(function);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(member));
                }
            }
        }

        private void ParseDoTypeDefinitionPass(EnumDeclarationSyntax enumSyntax)
        {
            var include = GetInclude(enumSyntax);

            if (include == null)
                return;

            var underlyingType = enumSyntax.BaseList?.Types.SingleOrDefault()?.Type.ToString();
            CppEnum node = new(enumSyntax.Identifier.ValueText, enumSyntax);

            if (!string.IsNullOrEmpty(underlyingType))
                node.UnderlyingType = underlyingType;

            include.Add(node);
        }

        private void ParseDoItemDefinitionPass(CppEnum cppEnum)
        {
            foreach (var member in cppEnum.Roslyn.Members)
            {
                cppEnum.AddEnumItem(member.Identifier.ValueText, member);
            }
        }

        private void ParseDoItemDefinitionPass(CppStruct cppStruct)
        {
            foreach (var member in cppStruct.Roslyn.Members)
            {
                switch (member)
                {
                    case BasePropertyDeclarationSyntax propertySyntax:
                        break;
                    case FieldDeclarationSyntax fieldSyntax:
                    {
                        CppField field = new(fieldSyntax.Declaration.Variables.Single().Identifier.ValueText);
                        ResolveAndFillType(
                            fieldSyntax.Declaration.Type, GetNativeTypeNameFromSyntax(fieldSyntax), field
                        );
                        cppStruct.Add(field);
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException(nameof(member));
                }
            }
        }

        private void ParseDoItemDefinitionPass(CppInterface cppInterface)
        {
            foreach (var member in cppInterface.Roslyn.Members)
            {
                switch (member)
                {
                    case MethodDeclarationSyntax methodSyntax when !DllImport.IsInAttributeList(methodSyntax):
                        CppMethod function = new(methodSyntax.Identifier.ValueText);

                        // TODO: CallingConvention

                        if (!methodSyntax.ReturnType.IsKind(SyntaxKind.VoidKeyword))
                        {
                            CppReturnValue returnValue = new();

                            ResolveAndFillType(
                                methodSyntax.ReturnType, GetNativeTypeNameFromSyntax(methodSyntax), returnValue
                            );

                            function.ReturnValue = returnValue;
                        }

                        foreach (var parameterSyntax in methodSyntax.ParameterList.Parameters)
                        {
                            CppParameter parameter = new(parameterSyntax.Identifier.ValueText);

                            ResolveAndFillType(
                                parameterSyntax.Type, GetNativeTypeNameFromSyntax(parameterSyntax), parameter
                            );

                            function.Add(parameter);
                        }

                        cppInterface.Add(function);
                        break;
                    case FieldDeclarationSyntax fieldSyntax when fieldSyntax.Declaration.Type is PointerTypeSyntax
                                                                 {
                                                                     ElementType: PointerTypeSyntax
                                                                     {
                                                                         ElementType: PredefinedTypeSyntax
                                                                             {Keyword: {ValueText: "void"}}
                                                                     }
                                                                 }
                                                              && fieldSyntax.Declaration.Variables is
                                                                     {Count: 1} variables
                                                              && variables[0].Identifier.ValueText == "lpVtbl":
                        
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(member));
                }
            }
        }

        private CppInclude GetInclude(SyntaxNode syntax) => GetIncludeFileFromSyntax(syntax) is { } includeFile
            ? GetInclude(includeFile)
            : null;

        private static string GetIncludeFileFromSyntax(SyntaxNode syntax) =>
            FindAttributeValueFromNode(syntax, SourceLocation, 0);

        private static string GetNativeInheritanceFromSyntax(MemberDeclarationSyntax syntax) =>
            GetAttributeValueFromSyntax(NativeInheritance, 0, syntax);

        private static string GetNativeTypeNameFromSyntax(MemberDeclarationSyntax syntax) =>
            GetAttributeValueFromSyntax(NativeTypeName, 0, syntax);

        private static string GetNativeTypeNameFromSyntax(BaseParameterSyntax syntax) =>
            GetAttributeValueFromSyntax(NativeTypeName, 0, syntax);

        private static string GetDllImportEntryPointFromSyntax(
            MethodDeclarationSyntax syntax, SeparatedSyntaxList<AttributeArgumentSyntax> dllImport
        ) => GetAttributeValueByName(dllImport, nameof(DllImportAttribute.EntryPoint)) switch
        {
            { } entryPoint => GetAttributeValueStringLiteral(entryPoint),
            _ => syntax.Identifier.ValueText
        };

        private static string GetDllImportCallingConventionFromSyntax(
            SeparatedSyntaxList<AttributeArgumentSyntax> dllImport
        ) => GetAttributeValueByName(dllImport, nameof(DllImportAttribute.CallingConvention)) switch
        {
            {
                Expression: MemberAccessExpressionSyntax {Name: {Identifier: {ValueText: {Length: > 0} callingConv}}}
            } => callingConv,
            _ => throw new ArgumentException()
        };

        private static AttributeArgumentSyntax GetAttributeValueByName(
            SeparatedSyntaxList<AttributeArgumentSyntax> arguments, string attributeName) =>
            arguments.FirstOrDefault(x => x.NameEquals?.Name.Identifier.ValueText == attributeName);

        private static string FindAttributeValueFromNode(SyntaxNode syntax, RoslynAttribute attribute,
                                                         int targetAttributeArgumentIndex)
        {
            var ancestor = syntax.FirstAncestorOrSelf<MemberDeclarationSyntax>(FindPredicate);
            return ancestor is { } ancestorSyntax
                ? GetAttributeValueFromSyntax(attribute, targetAttributeArgumentIndex, ancestorSyntax)
                : null;

            bool FindPredicate(MemberDeclarationSyntax arg) =>
                GetAttributeValueFromSyntax(attribute, targetAttributeArgumentIndex, arg) != null;
        }

        private static string GetAttributeValueFromSyntax(RoslynAttribute attribute,
                                                          int targetAttributeArgumentIndex,
                                                          MemberDeclarationSyntax syntax) =>
            attribute.GetAttributeFromAttributeList(syntax) is { } arguments
                ? GetAttributeValueStringLiteral(arguments[targetAttributeArgumentIndex])
                : null;

        private static string GetAttributeValueFromSyntax(RoslynAttribute attribute,
                                                          int targetAttributeArgumentIndex,
                                                          BaseParameterSyntax syntax) =>
            attribute.GetAttributeFromAttributeList(syntax) is { } arguments
                ? GetAttributeValueStringLiteral(arguments[targetAttributeArgumentIndex])
                : null;

        private static string GetAttributeValueStringLiteral(AttributeArgumentSyntax value) => value.Expression switch
        {
            LiteralExpressionSyntax literalExpressionSyntax => literalExpressionSyntax.Token.ValueText,
            _ => null
        };

        private static long? GetAttributeValueLongLiteral(AttributeArgumentSyntax value) => value.Expression switch
        {
            LiteralExpressionSyntax literalExpressionSyntax => long.Parse(literalExpressionSyntax.Token.ValueText),
            _ => null
        };

        private class RoslynAttribute
        {
            public RoslynAttribute(string Name, int? ExpectedAttributeArgumentCount)
            {
                this.Name = Name;
                this.ExpectedAttributeArgumentCount = ExpectedAttributeArgumentCount;
            }

            public SeparatedSyntaxList<AttributeArgumentSyntax>?
                GetAttributeFromAttributeList(MemberDeclarationSyntax syntax) =>
                GetAttributeFromAttributeList(syntax.AttributeLists);

            public SeparatedSyntaxList<AttributeArgumentSyntax>?
                GetAttributeFromAttributeList(BaseParameterSyntax syntax) =>
                GetAttributeFromAttributeList(syntax.AttributeLists);

            public bool IsInAttributeList(MemberDeclarationSyntax syntax) =>
                GetAttributeFromAttributeList(syntax.AttributeLists).HasValue;

            public bool IsInAttributeList(BaseParameterSyntax syntax) =>
                GetAttributeFromAttributeList(syntax.AttributeLists).HasValue;

            public SeparatedSyntaxList<AttributeArgumentSyntax>? FindAttributeFromNode(SyntaxNode syntax)
            {
                var ancestor = syntax.FirstAncestorOrSelf<MemberDeclarationSyntax>(FindPredicate);
                return ancestor is { } ancestorSyntax
                    ? GetAttributeFromAttributeList(ancestorSyntax.AttributeLists)
                    : null;

                bool FindPredicate(MemberDeclarationSyntax arg) =>
                    GetAttributeFromAttributeList(arg.AttributeLists) != null;
            }

            private SeparatedSyntaxList<AttributeArgumentSyntax>?
                GetAttributeFromAttributeList(SyntaxList<AttributeListSyntax> attributeLists)
            {
                foreach (var attributeSyntax in attributeLists.SelectMany(x => x.Attributes))
                {
                    var name = attributeSyntax.Name switch
                    {
                        SimpleNameSyntax simpleNameSyntax => simpleNameSyntax.Identifier.ValueText,
                        _ => null
                    };

                    if (name != Name)
                        continue;

                    var argumentList = attributeSyntax.ArgumentList?.Arguments ?? default;
                    if (ExpectedAttributeArgumentCount is { } expectedAttributeArgumentCount)
                        if (argumentList.Count != expectedAttributeArgumentCount)
                            continue;

                    return argumentList;
                }

                return null;
            }

            public string Name { get; }
            public int? ExpectedAttributeArgumentCount { get; }
        }

        private void ResolveAndFillType(TypeSyntax typeSyntax, string nativeTypeName, CppMarshallable type)
        {
            if (nativeTypeName is {Length: > 0})
            {
                const string constPrefix = "const ";
                type.Pointer = new string('*', nativeTypeName.Count(x => x == '*'));
                type.Const = nativeTypeName.StartsWith(constPrefix);
                var arrayIndex = nativeTypeName.IndexOfAny(new[] {'*', '[', ']', '&'});
                var typeNameStart = type.Const ? constPrefix.Length : 0;
                var typeName = arrayIndex >= 0
                                   ? nativeTypeName.Substring(typeNameStart, arrayIndex - typeNameStart)
                                   : nativeTypeName.Substring(typeNameStart);
                type.TypeName = typeName.Trim();
                if (arrayIndex >= 0)
                {
                    var arrayMatches = ArrayTypeRegex.Matches(nativeTypeName, arrayIndex);
                    type.IsArray = arrayMatches.Count != 0;
                    if (type.IsArray)
                        type.ArrayDimension = string.Join(
                            ",",
                            arrayMatches.OfType<Match>().Select(x => x.Groups[1])
                        );
                }

                return;
            }

            ResolveAndFillType(typeSyntax, type);
        }

        private void ResolveAndFillType(TypeSyntax typeSyntax, CppMarshallable type)
        {
            var isTypeResolved = false;

            while (!isTypeResolved)
            {
                switch (typeSyntax)
                {
                    case ArrayTypeSyntax arrayTypeSyntax:
                        type.IsArray = true;
                        typeSyntax = arrayTypeSyntax.ElementType;
                        type.ArrayDimension = string.Join(
                            ",",
                            arrayTypeSyntax.RankSpecifiers.SelectMany(x => x.Sizes.Select(y => y.ToString()))
                        );
                        break;
                    case NameSyntax simpleNameSyntax:
                        type.TypeName = simpleNameSyntax.ToString();
                        isTypeResolved = true;
                        break;
                    case PointerTypeSyntax pointerTypeSyntax:
                        type.Pointer = (type.Pointer ?? string.Empty) + "*";
                        typeSyntax = pointerTypeSyntax.ElementType;
                        break;
                    case PredefinedTypeSyntax predefinedTypeSyntax:
                        type.TypeName = predefinedTypeSyntax.Keyword.ValueText;
                        isTypeResolved = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(typeSyntax));
                }
            }
        }

        private CppInclude GetInclude(string includeFile)
        {
            var includeId = GetIncludeIdFromFilePath(includeFile);

            // Process only files listed inside the config files
            if (!_includeToProcess.Contains(includeId))
                return null;

            // Process only files attached (fully or partially) to an assembly/namespace
            if (!_includeIsAttached.ContainsKey(includeId))
                return null;

            _currentCppInclude = _group.FindInclude(includeId);

            if (_currentCppInclude == null)
            {
                _currentCppInclude = new CppInclude(includeId);
                _group.Add(_currentCppInclude);
            }

            return _currentCppInclude;
        }

        private static string GetIncludeIdFromFilePath(string filePath)
        {
            try
            {
                return File.Exists(filePath)
                           ? Path.GetFileNameWithoutExtension(filePath)
                           : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}