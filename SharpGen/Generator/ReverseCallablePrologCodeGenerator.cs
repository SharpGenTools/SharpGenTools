using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Generator.Marshallers;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class ReverseCallablePrologCodeGenerator : IMultiCodeGenerator<(CsCallable, InteropMethodSignature), StatementSyntax>
    {
        private readonly IGeneratorRegistry generators;
        private readonly GlobalNamespaceProvider globalNamespace;

        public ReverseCallablePrologCodeGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            this.generators = generators ?? throw new ArgumentNullException(nameof(generators));
            this.globalNamespace = globalNamespace ?? throw new ArgumentNullException(nameof(globalNamespace));
        }

        public IEnumerable<StatementSyntax> GenerateCode((CsCallable, InteropMethodSignature) callableSig)
        {
            var (csElement, interopSig) = callableSig;

            if (!interopSig.ForcedReturnBufferSig && csElement.HasReturnTypeValue(globalNamespace))
            {
                foreach (var statement in GenerateProlog(csElement.ReturnValue, null))
                {
                    yield return statement;
                }
            }

            foreach (var signatureParameter in interopSig.ParameterTypes)
            {
                var publicParameter = signatureParameter.Item;
                var nativeParameter = IdentifierName(signatureParameter.Name);
                var prologBuilder = publicParameter.PassedByNativeReference && !publicParameter.IsArray
                                        ? (Func<CsMarshalCallableBase, ExpressionSyntax, IEnumerable<StatementSyntax>>)GenerateNativeByRefProlog
                                        : GenerateProlog;
                foreach (var statement in prologBuilder(publicParameter, nativeParameter))
                {
                    yield return statement;
                }
            }
        }

        private IEnumerable<StatementSyntax> GenerateProlog(
            CsMarshalCallableBase publicElement,
            ExpressionSyntax nativeParameter)
        {
            var marshaller = generators.Marshalling.GetMarshaller(publicElement);
            var publicType = ParseTypeName(publicElement.PublicType.QualifiedName);
            if (publicElement.IsArray)
            {
                publicType = ArrayType(publicType, SingletonList(ArrayRankSpecifier()));
            }

            var generatesMarshalVariable = marshaller.GeneratesMarshalVariable(publicElement);
            var defaultLiteral = LiteralExpression(SyntaxKind.DefaultLiteralExpression, Token(SyntaxKind.DefaultKeyword));

            ExpressionSyntax publicTypeVariableValue = nativeParameter != null && !generatesMarshalVariable
                ? CastExpression(publicType, nativeParameter)
                : defaultLiteral;

            yield return LocalDeclarationStatement(
                VariableDeclaration(publicType)
                .AddVariables(
                    VariableDeclarator(Identifier(publicElement.Name))
                        .WithInitializer(
                            EqualsValueClause(publicTypeVariableValue))));

            if (generatesMarshalVariable)
            {
                var marshalTypeSyntax = marshaller.GetMarshalTypeSyntax(publicElement);

                ExpressionSyntax initializerExpression = nativeParameter != null
                    ? CastExpression(marshalTypeSyntax, nativeParameter)
                    : defaultLiteral;

                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        marshalTypeSyntax,
                        SingletonSeparatedList(
                            VariableDeclarator(
                                MarshallerBase.GetMarshalStorageLocationIdentifier(publicElement),
                                null,
                                EqualsValueClause(initializerExpression)
                            )
                        )
                    )
                );
            }
        }

        private IEnumerable<StatementSyntax> GenerateNativeByRefProlog(CsMarshalCallableBase publicElement, ExpressionSyntax nativeParameter)
        {
            var marshaller = generators.Marshalling.GetMarshaller(publicElement);
            ExpressionSyntax refToNativeExpression = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Unsafe),
                    GenericName(
                        Identifier(nameof(Unsafe.AsRef)),
                        TypeArgumentList(SingletonSeparatedList(marshaller.GetMarshalTypeSyntax(publicElement)))
                    )
                ),
                ArgumentList(SingletonSeparatedList(Argument(nativeParameter)))
            );

            var publicType = ParseTypeName(publicElement.PublicType.QualifiedName);
            var generatesMarshalVariable = marshaller.GeneratesMarshalVariable(publicElement);

            if (publicElement.IsLocalByRef)
            {
                Debug.Assert(marshaller is RefWrapperMarshaller);

                refToNativeExpression = RefExpression(refToNativeExpression);
            }
            else
            {
                Debug.Assert(publicElement is CsParameter {IsRefIn: true});
                Debug.Assert(marshaller is not RefWrapperMarshaller);

                if (publicElement is CsParameter {IsOptional: true})
                {
                    var defaultLiteral = LiteralExpression(
                        SyntaxKind.DefaultLiteralExpression,
                        Token(SyntaxKind.DefaultKeyword)
                    );

                    refToNativeExpression = ConditionalExpression(
                        BinaryExpression(SyntaxKind.NotEqualsExpression, nativeParameter, defaultLiteral),
                        refToNativeExpression,
                        defaultLiteral
                    );
                }
            }

            var refToNativeClause = EqualsValueClause(refToNativeExpression);
            EqualsValueClauseSyntax publicParamInitializer = default;

            if (publicElement is CsParameter {IsOptional: true, IsLocalByRef: true} parameter)
            {
                var refVariableDeclaration = LocalDeclarationStatement(
                    VariableDeclaration(
                        RefType(marshaller.GetMarshalTypeSyntax(publicElement)),
                        SingletonSeparatedList(
                            VariableDeclarator(MarshallerBase.GetRefLocationIdentifier(publicElement))
                               .WithInitializer(refToNativeClause)
                        )
                    )
                );

                if (generatesMarshalVariable && parameter is {IsRef: true})
                {
                    refVariableDeclaration = refVariableDeclaration.WithLeadingTrivia(
                        Comment("Optional ref parameter that requires generating marshal variable is unsupported.")
                    );
                }
                else
                {
                    refToNativeClause = default;
                }

                yield return refVariableDeclaration;
            }

            if (generatesMarshalVariable)
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        RefType(marshaller.GetMarshalTypeSyntax(publicElement)),
                        SingletonSeparatedList(
                            VariableDeclarator(MarshallerBase.GetMarshalStorageLocationIdentifier(publicElement))
                               .WithInitializer(refToNativeClause)
                        )
                    )
                );
            }
            else
            {
                publicParamInitializer = refToNativeClause;

                if (publicElement is CsParameter {IsOptional: false, IsLocalByRef: true} or CsReturnValue)
                    publicType = RefType(publicType);
            }

            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    publicType,
                    SingletonSeparatedList(
                        VariableDeclarator(Identifier(publicElement.Name), default, publicParamInitializer)
                    )
                )
            );
        }
    }
}
