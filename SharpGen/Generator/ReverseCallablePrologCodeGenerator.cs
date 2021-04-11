using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

            var defaultLiteral = LiteralExpression(SyntaxKind.DefaultLiteralExpression, Token(SyntaxKind.DefaultKeyword));

            ExpressionSyntax publicTypeVariableValue = nativeParameter != null
                ? CastExpression(publicType, nativeParameter)
                : defaultLiteral;

            yield return LocalDeclarationStatement(
                VariableDeclaration(publicType)
                .AddVariables(
                    VariableDeclarator(Identifier(publicElement.Name))
                        .WithInitializer(
                            EqualsValueClause(publicTypeVariableValue))));

            if (marshaller.GeneratesMarshalVariable(publicElement))
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
            var localByRef = publicElement.IsRef || publicElement.IsOut;
            ExpressionSyntax refToNativeExpression = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    GlobalNamespaceProvider.GetTypeNameSyntax(BuiltinType.Unsafe),
                    GenericName(Identifier(nameof(Unsafe.AsRef)))
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SingletonSeparatedList(
                                marshaller.GetMarshalTypeSyntax(publicElement))))))
            .WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList(
                        Argument(
                            nativeParameter))));

            var publicType = ParseTypeName(publicElement.PublicType.QualifiedName);

            if (localByRef)
            {
                if (!marshaller.GeneratesMarshalVariable(publicElement))
                {
                    publicType = RefType(publicType);
                }

                refToNativeExpression = RefExpression(refToNativeExpression);
            }

            if (marshaller.GeneratesMarshalVariable(publicElement))
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        RefType(marshaller.GetMarshalTypeSyntax(publicElement)))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                MarshallerBase.GetMarshalStorageLocationIdentifier(publicElement))
                            .WithInitializer(
                                EqualsValueClause(
                                    refToNativeExpression)))));

                yield return LocalDeclarationStatement(
                    VariableDeclaration(publicType)
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier(publicElement.Name)))));
            }
            else
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(publicType)
                    .AddVariables(
                        VariableDeclarator(Identifier(publicElement.Name))
                        .WithInitializer(EqualsValueClause(refToNativeExpression))));
            }
        }
    }
}
