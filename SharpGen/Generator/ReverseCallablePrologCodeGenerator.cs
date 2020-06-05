using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    class ReverseCallablePrologCodeGenerator : IMultiCodeGenerator<(CsCallable, InteropMethodSignature), StatementSyntax>
    {
        private readonly IGeneratorRegistry generators;
        private readonly GlobalNamespaceProvider globalNamespace;

        public ReverseCallablePrologCodeGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            this.generators = generators;
            this.globalNamespace = globalNamespace;
        }

        public IEnumerable<StatementSyntax> GenerateCode((CsCallable, InteropMethodSignature) callableSig)
        {
            var (csElement, interopSig) = callableSig;
            var interopParameters = interopSig.ParameterTypes;
            if ((interopSig.Flags & InteropMethodSignatureFlags.ForcedReturnBufferSig) != 0)
            {
                foreach (var statement in GenerateNativeByRefProlog(csElement.ReturnValue, IdentifierName("returnSlot")))
                {
                    yield return statement;
                }
            }
            else if (csElement.HasReturnType && (!csElement.HideReturnType || csElement.ForceReturnType))
            {
                foreach (var statement in GenerateProlog(csElement.ReturnValue, null))
                {
                    yield return statement;
                }
            }

            for (int i = 0; i < csElement.Parameters.Count; i++)
            {
                var publicParameter = csElement.Parameters[i];
                var nativeParameter = IdentifierName($"param{i}");
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
            yield return LocalDeclarationStatement(
                VariableDeclaration(publicType)
                .AddVariables(
                    VariableDeclarator(Identifier(publicElement.Name))
                        .WithInitializer(
                            EqualsValueClause(
                                DefaultExpression(publicType)))));

            if (marshaller.GeneratesMarshalVariable(publicElement))
            {
                var marshalTypeSyntax = marshaller.GetMarshalTypeSyntax(publicElement);
                yield return LocalDeclarationStatement(
                    VariableDeclaration(marshalTypeSyntax)
                    .AddVariables(
                        VariableDeclarator(generators.Marshalling.GetMarshalStorageLocationIdentifier(publicElement))
                        .WithInitializer(
                            EqualsValueClause(
                                nativeParameter != null
                                ? (ExpressionSyntax)CastExpression(
                                    marshalTypeSyntax,
                                    nativeParameter)
                                : DefaultExpression(marshalTypeSyntax)))));
            }
            else
            {
                if (nativeParameter != null)
                {
                    yield return ExpressionStatement(
                       AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                           IdentifierName(publicElement.Name),
                           CastExpression(
                                ParseTypeName(publicElement.PublicType.QualifiedName),
                                nativeParameter)
                   )); 
                }
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
                    GenericName(Identifier("AsRef"))
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
                                generators.Marshalling.GetMarshalStorageLocationIdentifier(publicElement))
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
