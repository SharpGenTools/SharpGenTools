using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Config;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

internal sealed class VtblGenerator : MemberSingleCodeGeneratorBase<CsInterface>
{
    private static readonly SyntaxToken DelegateCacheGlobalIdentifier = Identifier("DelegateCache");
    private static readonly SyntaxToken VtblIdentifier = Identifier("Vtbl");

    public override MemberDeclarationSyntax GenerateCode(CsInterface csElement)
    {
        var vtblClassName = csElement.VtblName.Split('.').Last();

        // Default: at least protected to enable inheritance.
        var vtblVisibility = csElement.VtblVisibility ?? Visibility.Internal;

        bool AnyOffsetDiffersPredicate()
        {
            bool Predicate(CsMethod x) => x.WindowsOffset != x.Offset ||
                                          x.InteropSignatures is not { Count: 1 } v ||
                                          !v.ContainsKey(PlatformDetectionType.Any);

            return csElement.Methods.Any(Predicate);
        }

        ExpressionSyntax MethodArrayBuilder(bool withFunctionPointers)
        {
            InitializerExpressionSyntax Generate(PlatformDetectionType platform) =>
                InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    SeparatedList(
                        GetOrderedMethods(csElement.Methods, platform)
                           .Select(x => MethodBuilder(x, platform, withFunctionPointers))
                    )
                );

            return GeneratorHelpers.PlatformSpecificExpression(
                GlobalNamespace, Generators.Config.Platforms,
                AnyOffsetDiffersPredicate,
                () => Generate(PlatformDetectionType.Windows),
                () => Generate(PlatformDetectionType.ItaniumSystemV),
                ImplicitArrayCreationExpression
            );
        }

        var members = NewMemberList;

        List<CsMethod> legacyMethods = new();

        foreach (var method in csElement.Methods)
        {
            if (method.IsFunctionPointerInVtbl)
            {
                legacyMethods.Add(method);
                continue;
            }

            members.AddRange(method.InteropSignatures.Keys, platform => DelegateCacheDecl(method, platform));
        }

        var conditionalStart = members.Count;

        members.Add(VtblDecl(MethodArrayBuilder(true)).WithTrailingElseDirective());

        foreach (var method in legacyMethods)
            members.AddRange(method.InteropSignatures.Keys, platform => DelegateCacheDecl(method, platform));

        members.Add(VtblDecl(MethodArrayBuilder(false)).WithTrailingEndIfDirective());
        members[conditionalStart] = members[conditionalStart].WithLeadingIfDirective(GeneratorHelpers.PreprocessorNameSyntax);

#if false
            ImplicitArrayCreationExpressionSyntax GenerateDelegateCacheFill(PlatformDetectionType platform) =>
                ImplicitArrayCreationExpression(
                    InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SeparatedList(
                            GetOrderedMethods(csElement.Methods, platform)
                               .Select(x => DelegateCacheBuilder(x, platform))
                        )
                    )
                );

            members.Add(
                ConstructorDeclaration(vtblClassName)
                   .WithModifiers(TokenList(Token(SyntaxKind.StaticKeyword)))
                   .WithBody(
                        Block(
                            ExpressionStatement(
                                AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    IdentifierName(DelegateCacheGlobalIdentifier),
                                    GeneratorHelpers.PlatformSpecificExpression(
                                        GlobalNamespace, Generators.Config.Platforms,
                                        AnyOffsetDiffersPredicate,
                                        () => GenerateDelegateCacheFill(PlatformDetectionType.Windows),
                                        () => GenerateDelegateCacheFill(PlatformDetectionType.ItaniumSystemV)
                                    )
                                )
                            )
                        )
                    )
                   .WithTrailingEndIfDirective()
            );
#endif

        members.AddRange(csElement.Methods, Generators.ShadowCallable);

        return ClassDeclaration(vtblClassName)
              .WithModifiers(
                   ModelUtilities.VisibilityToTokenList(
                       vtblVisibility,
                       SyntaxKind.StaticKeyword, SyntaxKind.UnsafeKeyword, SyntaxKind.PartialKeyword
                   )
               )
              .WithMembers(List(members));
    }

    private ExpressionSyntax GetMarshalFunctionPointerForDelegate(CsMethod method,
                                                                  PlatformDetectionType platform) =>
        InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                GlobalNamespace.GetTypeNameSyntax(BuiltinType.Marshal),
                IdentifierName("GetFunctionPointerForDelegate")
            ),
            ArgumentList(SingletonSeparatedList(Argument(IdentifierName(GetMethodCacheName(method, platform)))))
        );

    private static void CoercePlatform(CsMethod csMethod, ref PlatformDetectionType platform)
    {
        var interopSignatures = csMethod.InteropSignatures;
        if (interopSignatures.ContainsKey(platform))
            return;

        platform = PlatformDetectionType.Any;
        Debug.Assert(interopSignatures.ContainsKey(platform));
    }

    private FieldDeclarationSyntax DelegateCacheDecl(CsMethod method, PlatformDetectionType platform) =>
        FieldDeclaration(
            default, TokenList(
                Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword)
            ),
            VariableDeclaration(
                IdentifierName(GetMethodDelegateName(method, platform)),
                SingletonSeparatedList(
                    VariableDeclarator(
                        GetMethodCacheName(method, platform), default,
                        EqualsValueClause(
                            platform != PlatformDetectionType.Any
                                ? ConditionalExpression(
                                    GeneratorHelpers.PlatformCondition(GlobalNamespace, platform),
                                    IdentifierName(GetMethodImplName(method, platform)), NullLiteral
                                )
                                : IdentifierName(GetMethodImplName(method, platform))
                        )
                    )
                )
            )
        );

    private FieldDeclarationSyntax VtblDecl(ExpressionSyntax value) => FieldDeclaration(
        default, TokenList(
            Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword)
        ),
        VariableDeclaration(
            ArrayType(
                GeneratorHelpers.IntPtrType,
                SingletonList(
                    ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(OmittedArraySizeExpression()))
                )
            ),
            SingletonSeparatedList(VariableDeclarator(VtblIdentifier, default, EqualsValueClause(value)))
        )
    );

    private ExpressionSyntax MethodBuilder(CsMethod method, PlatformDetectionType platform, bool withFunctionPointers)
    {
        CoercePlatform(method, ref platform);

        var vtblMethodName = IdentifierName(GetMethodImplName(method, platform));

        FunctionPointerTypeSyntax FnPtrType()
        {
            var sig = method.InteropSignatures[platform];

            var fnptrParameters = sig.ParameterTypes
                                     .Select(x => x.InteropTypeSyntax)
                                     .Prepend(GeneratorHelpers.IntPtrType)
                                     .Append(sig.ReturnTypeSyntax)
                                     .Select(FunctionPointerParameter);

            return FunctionPointerType(
                FunctionPointerCallingConvention(
                    Token(SyntaxKind.UnmanagedKeyword),
                    FunctionPointerUnmanagedCallingConventionList(
                        SingletonSeparatedList(
                            FunctionPointerUnmanagedCallingConvention(
                                Identifier(method.CppCallingConvention.ToCallConvShortName())
                            )
                        )
                    )
                ),
                FunctionPointerParameterList(SeparatedList(fnptrParameters))
            );
        }

        return method.IsFunctionPointerInVtbl && withFunctionPointers
                   ? GeneratorHelpers.CastExpression(
                       GeneratorHelpers.IntPtrType,
                       GeneratorHelpers.CastExpression(
                           FnPtrType(), PrefixUnaryExpression(SyntaxKind.AddressOfExpression, vtblMethodName)
                       )
                   )
                   : GetMarshalFunctionPointerForDelegate(method, platform);
    }

    private CsMethod[] GetOrderedMethods(IEnumerable<CsMethod> methods, PlatformDetectionType platform)
    {
        var getter = GeneratorHelpers.GetOffsetGetter(platform);

        var items = methods.OrderBy(getter).ToArray();
        var count = items.Length;
        if (count == 0)
            return items;

        var offsets = items.Select(getter).ToArray();
        var minOffset = offsets[0];
        var maxOffset = offsets.Last();
        Debug.Assert(maxOffset - minOffset + 1 == count);

        for (var i = 0; i < count; i++)
            Debug.Assert(offsets[i] == minOffset + i);

        return items;
    }

    internal static SyntaxToken GetMethodDelegateName(CsCallable csElement, PlatformDetectionType platform) =>
        Identifier(csElement.Name + "Delegate" + GeneratorHelpers.GetPlatformSpecificSuffix(platform));

    internal static SyntaxToken GetMethodCacheName(CsCallable csElement, PlatformDetectionType platform) =>
        Identifier(csElement.Name + "DelegateCache" + GeneratorHelpers.GetPlatformSpecificSuffix(platform));

    internal static SyntaxToken GetMethodImplName(CsCallable csElement, PlatformDetectionType platform) =>
        Identifier(csElement.Name + "Impl" + GeneratorHelpers.GetPlatformSpecificSuffix(platform));

    public VtblGenerator(Ioc ioc) : base(ioc)
    {
    }
}