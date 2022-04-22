using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Generator.Marshallers;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator;

internal sealed class ShadowCallbackGenerator : MemberPlatformMultiCodeGeneratorBase<CsCallable>
{
    private static readonly NameSyntax UnmanagedFunctionPointerAttributeName =
        ParseName("System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute");

    private static readonly NameSyntax UnmanagedCallersOnlyAttributeName =
        ParseName("System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute");

    private static readonly NameSyntax CompilerServicesNamespace =
        ParseName("System.Runtime.CompilerServices");

    public override IEnumerable<PlatformDetectionType> GetPlatforms(CsCallable csElement) =>
        csElement.InteropSignatures.Keys;

    public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsCallable csElement,
                                                                      PlatformDetectionType platform)
    {
        var sig = csElement.InteropSignatures[platform];

        var delegateDecl = GenerateDelegateDeclaration(csElement, platform, sig);
        if (csElement is CsMethod { IsFunctionPointerInVtbl: true })
            delegateDecl = delegateDecl.WithLeadingIfDirective(GeneratorHelpers.NotPreprocessorNameSyntax)
                                       .WithTrailingElseDirective();

        yield return delegateDecl;
        yield return GenerateShadowCallback(csElement, platform, sig);
    }

    private static DelegateDeclarationSyntax GenerateDelegateDeclaration(CsCallable csElement,
                                                                         PlatformDetectionType platform,
                                                                         InteropMethodSignature sig) =>
        DelegateDeclaration(sig.ReturnTypeSyntax, VtblGenerator.GetMethodDelegateName(csElement, platform))
           .AddAttributeLists(
                AttributeList(
                    SingletonSeparatedList(
                        Attribute(UnmanagedFunctionPointerAttributeName)
                           .AddArgumentListArguments(
                                AttributeArgument(
                                    ModelUtilities.GetManagedCallingConventionExpression(sig.CallingConvention))))))
           .WithParameterList(GetNativeParameterList(csElement, sig))
           .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));

    private static ParameterListSyntax GetNativeParameterList(CsCallable csElement, InteropMethodSignature sig) =>
        ParameterList(
            (csElement is CsMethod
                 ? SingletonSeparatedList(Parameter(Identifier("thisObject")).WithType(GeneratorHelpers.IntPtrType))
                 : default)
           .AddRange(
                sig.ParameterTypes
                   .Select(type => Parameter(Identifier(type.Name)).WithType(type.InteropTypeSyntax))
            )
        );

    private static CatchClauseSyntax GenerateCatchClause(SyntaxToken exceptionVariableIdentifier, params StatementSyntax[] statements)
    {
        StatementSyntaxList statementList = new()
        {
            ExpressionStatement(
                ConditionalAccessExpression(
                    ParenthesizedExpression(
                        BinaryExpression(
                            SyntaxKind.AsExpression,
                            IdentifierName(@"@this"),
                            IdentifierName("SharpGen.Runtime.IExceptionCallback")
                        )
                    ),
                    InvocationExpression(
                        MemberBindingExpression(IdentifierName("RaiseException")),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(IdentifierName(exceptionVariableIdentifier))
                            )
                        )
                    )
                )
            )
        };

        statementList.AddRange(statements);

        return CatchClause()
              .WithDeclaration(
                   CatchDeclaration(ParseTypeName("System.Exception"), exceptionVariableIdentifier)
               )
              .WithBlock(statementList.ToBlock());
    }

    private StatementSyntax GenerateShadowCallbackStatement(CsCallable csElement) =>
        GeneratorHelpers.VarDeclaration(
            Identifier("@this"),
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    GlobalNamespace.GetTypeNameSyntax(WellKnownName.CppObjectShadow),
                    GenericName(
                        Identifier("ToCallback"),
                        TypeArgumentList(
                            SingletonSeparatedList<TypeSyntax>(IdentifierName(csElement.Parent.Name))
                        )
                    )
                ),
                ArgumentList(SingletonSeparatedList(Argument(IdentifierName("thisObject"))))
            )
        );

    private MethodDeclarationSyntax GenerateShadowCallback(CsCallable csElement, PlatformDetectionType platform, InteropMethodSignature sig)
    {
        var interopReturnType = sig.ReturnTypeSyntax;

        var statements = NewStatementList;

        statements.Add(csElement, platform, Generators.ReverseCallableProlog);

        statements.AddRange(
            csElement.ReturnValue,
            static(marshaller, item) => marshaller.GenerateNativeToManagedExtendedProlog(item)
        );

        statements.AddRange(
            csElement.Parameters,
            static(marshaller, item) => marshaller.GenerateNativeToManagedExtendedProlog(item)
        );

        statements.AddRange(
            csElement.InRefInRefParameters,
            static(marshaller, item) => marshaller.GenerateNativeToManaged(item, false)
        );

        var managedArguments = csElement.PublicParameters
                                        .Select(param => GetMarshaller(param).GenerateManagedArgument(param))
                                        .ToList();

        ExpressionSyntax callableName = csElement is CsFunction
                                            ? IdentifierName(csElement.Name)
                                            : MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("@this"),
                                                IdentifierName(csElement.Name)
                                            );

        var invocation = InvocationExpression(callableName, ArgumentList(SeparatedList(managedArguments)));

        var returnValueNeedsMarshalling = GetMarshaller(csElement.ReturnValue).GeneratesMarshalVariable(csElement.ReturnValue);

        if (!csElement.HasReturnStatement)
        {
            statements.Add(ExpressionStatement(invocation));
        }
        else
        {
            var publicReturnValue = csElement.ActualReturnValue;

            statements.Add(
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(publicReturnValue.Name),
                        invocation
                    )
                )
            );

            if (returnValueNeedsMarshalling && publicReturnValue is CsReturnValue)
                statements.Add(
                    publicReturnValue,
                    static(marshaller, item) => marshaller.GenerateManagedToNative(item, false)
                );
        }

        statements.AddRange(
            csElement.LocalManagedReferenceParameters,
            static(marshaller, item) => marshaller.GenerateManagedToNative(item, false)
        );

        var isForcedReturnBufferSig = sig.ForcedReturnBufferSig;

        var nativeReturnLocation = returnValueNeedsMarshalling
                                       ? MarshallerBase.GetMarshalStorageLocation(csElement.ReturnValue)
                                       : IdentifierName(csElement.ReturnValue.Name);

        if (csElement.HasReturnTypeValue && !csElement.HasReturnTypeParameter && (returnValueNeedsMarshalling || isForcedReturnBufferSig || !ReverseCallablePrologCodeGenerator.GetPublicType(csElement.ReturnValue).IsEquivalentTo(interopReturnType)))
            nativeReturnLocation = GeneratorHelpers.CastExpression(interopReturnType, nativeReturnLocation);

        if (csElement.HasReturnTypeValue)
            statements.Add(
                ReturnStatement(
                    isForcedReturnBufferSig
                        ? IdentifierName("returnSlot")
                        : nativeReturnLocation
                )
            );

        var exceptionVariableIdentifier = Identifier("__exception__");

        StatementSyntax[] catchClauseStatements = null;

        if (csElement.IsReturnTypeResult)
        {
            catchClauseStatements = new StatementSyntax[]
            {
                ReturnStatement(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                GlobalNamespace.GetTypeNameSyntax(WellKnownName.Result),
                                IdentifierName("GetResultFromException")
                            ),
                            ArgumentList(
                                SingletonSeparatedList(Argument(IdentifierName(exceptionVariableIdentifier)))
                            )
                        ),
                        IdentifierName("Code")
                    )
                )
            };

            if (csElement.IsReturnTypeHidden && !csElement.ForceReturnType)
            {
                statements.Add(ReturnStatement(
                                   MemberAccessExpression(
                                       SyntaxKind.SimpleMemberAccessExpression,
                                       MemberAccessExpression(
                                           SyntaxKind.SimpleMemberAccessExpression,
                                           GlobalNamespace.GetTypeNameSyntax(WellKnownName.Result),
                                           IdentifierName("Ok")),
                                       IdentifierName("Code"))));
            }
        }
        else if (csElement.HasReturnType)
        {
            catchClauseStatements = new StatementSyntax[]
            {
                ReturnStatement(
                    isForcedReturnBufferSig ? IdentifierName("returnSlot") : DefaultLiteral
                )
            };
        }

        var fullBody = NewStatementList;

        if (csElement is CsMethod)
            fullBody.Add(GenerateShadowCallbackStatement(csElement));

        fullBody.Add(
            TryStatement()
               .WithBlock(statements.ToBlock())
               .WithCatches(
                    SingletonList(
                        GenerateCatchClause(exceptionVariableIdentifier, catchClauseStatements)
                    )
                )
        );

        return MethodDeclaration(
                   interopReturnType,
                   VtblGenerator.GetMethodImplName(csElement, platform)
               )
              .WithModifiers(
                   TokenList(
                       Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword),
                       Token(SyntaxKind.UnsafeKeyword)
                   )
               )
              .WithParameterList(GetNativeParameterList(csElement, sig))
              .WithBody(fullBody.ToBlock())
              .WithAttributeLists(
                   csElement is CsMethod { IsFunctionPointerInVtbl: true }
                       ? SingletonList(
                           AttributeList(
                               SingletonSeparatedList(
                                   Attribute(
                                       UnmanagedCallersOnlyAttributeName,
                                       AttributeArgumentList(
                                           SingletonSeparatedList(
                                               AttributeArgument(FnPtrCallConvs(sig.CallingConvention))
                                                  .WithNameEquals(NameEquals("CallConvs"))
                                           )
                                       )
                                   )
                               )
                           ).WithTrailingEndIfDirective()
                       )
                       : default
               );

        static ExpressionSyntax FnPtrCallConvs(CallingConvention callingConvention) =>
            ImplicitArrayCreationExpression(
                InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    SingletonSeparatedList<ExpressionSyntax>(
                        TypeOfExpression(
                            QualifiedName(
                                CompilerServicesNamespace,
                                IdentifierName("CallConv" + callingConvention.ToCallConvShortName())
                            )
                        )
                    )
                )
            );
    }

    public ShadowCallbackGenerator(Ioc ioc) : base(ioc)
    {
    }
}