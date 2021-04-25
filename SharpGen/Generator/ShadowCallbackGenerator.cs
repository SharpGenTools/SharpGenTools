using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Generator.Marshallers;
using SharpGen.Model;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class ShadowCallbackGenerator : IMultiCodeGenerator<CsCallable, MemberDeclarationSyntax>
    {
        private readonly IGeneratorRegistry generators;
        private readonly GlobalNamespaceProvider globalNamespace;

        public ShadowCallbackGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            this.generators = generators ?? throw new ArgumentNullException(nameof(generators));
            this.globalNamespace = globalNamespace ?? throw new ArgumentNullException(nameof(globalNamespace));
        }

        public IEnumerable<MemberDeclarationSyntax> GenerateCode(CsCallable csElement)
        {
            foreach (var sig in csElement.InteropSignatures.Where(sig => (sig.Key & generators.Config.Platforms) != 0))
            {
                yield return GenerateDelegateDeclaration(csElement, sig.Key, sig.Value);
                yield return GenerateShadowCallback(csElement, sig.Key, sig.Value);
            }
        }

        private DelegateDeclarationSyntax GenerateDelegateDeclaration(CsCallable csElement, PlatformDetectionType platform, InteropMethodSignature sig)
        {
            return DelegateDeclaration(ParseTypeName(sig.ReturnType.TypeName), VtblGenerator.GetMethodDelegateName(csElement, platform))
                .AddAttributeLists(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(
                                ParseName("System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute"))
                            .AddArgumentListArguments(
                                AttributeArgument(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("System"),
                                                    IdentifierName("Runtime")),
                                                IdentifierName("InteropServices")),
                                            IdentifierName("CallingConvention")),
                                        IdentifierName(sig.CallingConvention.ToManagedCallingConventionName())))))))
            .WithParameterList(
                ParameterList(
                    (csElement is CsMethod ?
                        SingletonSeparatedList(
                            Parameter(Identifier("thisObject"))
                            .WithType(ParseTypeName("System.IntPtr")))
                        : default)
                    .AddRange(
                        sig.ParameterTypes
                            .Select(type =>
                                Parameter(Identifier(type.Name))
                                .WithType(type.InteropTypeSyntax))
                        )))
            .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));
        }

        private static CatchClauseSyntax GenerateCatchClause(CsCallable csElement, SyntaxToken exceptionVariableIdentifier, params StatementSyntax[] statements)
        {
            StatementSyntaxList statementList = new()
            {
                GenerateShadowCallbackStatement(csElement),
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
                  .WithBlock(Block())
                  .WithDeclaration(
                       CatchDeclaration(ParseTypeName("System.Exception"), exceptionVariableIdentifier)
                   )
                  .WithBlock(statementList.ToBlock());
        }

        private static LocalDeclarationStatementSyntax GenerateShadowCallbackStatement(CsCallable csElement)
        {
            var parentName = IdentifierName(csElement.Parent.Name);

            var callbackValue = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                InvocationExpression(
                    GenericName(Identifier("ToShadow"))
                       .WithTypeArgumentList(
                            TypeArgumentList(
                                SingletonSeparatedList<TypeSyntax>(
                                    IdentifierName(csElement.GetParent<CsInterface>().ShadowName)
                                )
                            )
                        ),
                    ArgumentList(SingletonSeparatedList(Argument(IdentifierName("thisObject"))))
                ),
                IdentifierName("Callback")
            );

            return LocalDeclarationStatement(
                VariableDeclaration(
                    parentName,
                    SingletonSeparatedList(
                        VariableDeclarator(Identifier("@this"))
                           .WithInitializer(EqualsValueClause(CastExpression(parentName, callbackValue)))
                    )
                )
            );
        }

        private MethodDeclarationSyntax GenerateShadowCallback(CsCallable csElement, PlatformDetectionType platform, InteropMethodSignature sig)
        {
            var interopReturnType = ParseTypeName(sig.ReturnType.TypeName);

            var methodDecl = MethodDeclaration(
                interopReturnType,
                csElement.Name + GeneratorHelpers.GetPlatformSpecificSuffix(platform))
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PrivateKeyword),
                        Token(SyntaxKind.StaticKeyword),
                        Token(SyntaxKind.UnsafeKeyword)));

            if (csElement is CsMethod)
            {
                methodDecl = methodDecl
                    .AddParameterListParameters(
                        Parameter(Identifier("thisObject"))
                        .WithType(ParseTypeName("System.IntPtr")));
            }

            methodDecl = methodDecl
                .AddParameterListParameters(
                    sig.ParameterTypes
                       .Select(type =>
                                   Parameter(Identifier(type.Name))
                                      .WithType(type.InteropTypeSyntax))
                       .ToArray());

            StatementSyntaxList statements = new(generators.Marshalling);

            statements.AddRange(generators.ReverseCallableProlog.GenerateCode((csElement, sig)));

            statements.AddRange(
                csElement.ReturnValue,
                static(marshaller, item) => marshaller.GenerateNativeToManagedExtendedProlog(item)
            );

            statements.AddRange(
                csElement.Parameters,
                static(marshaller, item) => marshaller.GenerateNativeToManagedExtendedProlog(item)
            );

            if (csElement is CsMethod)
            {
                statements.Add(GenerateShadowCallbackStatement(csElement));
            }

            statements.AddRange(
                csElement.InRefInRefParameters,
                static(marshaller, item) => marshaller.GenerateNativeToManaged(item, false)
            );

            var managedArguments = csElement.PublicParameters
                .Select(param => generators.Marshalling.GetMarshaller(param).GenerateManagedArgument(param))
                .ToList();

            ExpressionSyntax callableName = csElement is CsFunction
                                                ? IdentifierName(csElement.Name)
                                                : MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("@this"),
                                                    IdentifierName(csElement.Name)
                                                );

            var invocation = InvocationExpression(callableName, ArgumentList(SeparatedList(managedArguments)));

            var returnValueMarshaller = generators.Marshalling.GetMarshaller(csElement.ReturnValue);

            var returnValueNeedsMarshalling = returnValueMarshaller.GeneratesMarshalVariable(csElement.ReturnValue);

            if (!csElement.HasReturnStatement(globalNamespace))
            {
                statements.Add(ExpressionStatement(invocation));
            }
            else
            {
                var publicReturnValue = csElement.ActualReturnValue;

                statements.Add(ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(publicReturnValue.Name),
                        invocation)));

                if (returnValueNeedsMarshalling && publicReturnValue is CsReturnValue)
                    statements.Add(
                        publicReturnValue,
                        static(marshaller, item) => marshaller.GenerateManagedToNative(item, false)
                    );
            }

            statements.AddRange(
                csElement.RefOutParameters,
                static(marshaller, item) => marshaller.GenerateManagedToNative(item, false)
            );

            var isForcedReturnBufferSig = sig.ForcedReturnBufferSig;

            var nativeReturnLocation = returnValueNeedsMarshalling
                                           ? MarshallerBase.GetMarshalStorageLocation(csElement.ReturnValue)
                                           : IdentifierName(csElement.ReturnValue.Name);

            var doReturnResult = csElement.IsReturnTypeResult(globalNamespace);

            if (csElement.HasReturnTypeValue(globalNamespace) && !csElement.HasReturnTypeParameter)
            {
                nativeReturnLocation = CastExpression(interopReturnType, nativeReturnLocation);
            }

            if (csElement.HasReturnTypeValue(globalNamespace))
            {
                statements.Add(
                    ReturnStatement(
                        isForcedReturnBufferSig
                            ? IdentifierName("returnSlot")
                            : nativeReturnLocation
                    )
                );
            }

            var exceptionVariableIdentifier = Identifier("__exception__");

            CatchClauseSyntax catchClause;

            if (doReturnResult)
            {
                catchClause = GenerateCatchClause(
                    csElement, exceptionVariableIdentifier,
                    ReturnStatement(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    globalNamespace.GetTypeNameSyntax(WellKnownName.Result),
                                    IdentifierName("GetResultFromException")
                                ),
                                ArgumentList(
                                    SingletonSeparatedList(Argument(IdentifierName(exceptionVariableIdentifier)))
                                )
                            ),
                            IdentifierName("Code")
                        )
                    )
                );

                if (csElement.IsReturnTypeHidden(globalNamespace) && !csElement.ForceReturnType)
                {
                    statements.Add(ReturnStatement(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    globalNamespace.GetTypeNameSyntax(WellKnownName.Result),
                                    IdentifierName("Ok")),
                                IdentifierName("Code"))));
                }
            }
            else if (csElement.HasReturnType)
            {
                catchClause = GenerateCatchClause(
                    csElement, exceptionVariableIdentifier,
                    ReturnStatement(
                        isForcedReturnBufferSig
                            ? IdentifierName("returnSlot")
                            : LiteralExpression(SyntaxKind.DefaultLiteralExpression, Token(SyntaxKind.DefaultKeyword))
                    )
                );
            }
            else
            {
                catchClause = GenerateCatchClause(csElement, exceptionVariableIdentifier);
            }

            return methodDecl.WithBody(
                Block(
                    TryStatement()
                    .WithBlock(statements.ToBlock())
                    .WithCatches(SingletonList(catchClause))));
        }
    }
}
