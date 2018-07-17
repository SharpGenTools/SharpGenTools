using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    class ReverseCallablePrologCodeGenerator : IMultiCodeGenerator<CsCallable, StatementSyntax>
    {
        private readonly IGeneratorRegistry generators;
        private readonly GlobalNamespaceProvider globalNamespace;

        public ReverseCallablePrologCodeGenerator(IGeneratorRegistry generators, GlobalNamespaceProvider globalNamespace)
        {
            this.generators = generators;
            this.globalNamespace = globalNamespace;
        }

        public IEnumerable<StatementSyntax> GenerateCode(CsCallable csElement)
        {
            var interopParameters = csElement.Interop.ParameterTypes;
            var realParameterStart = 0;
            if (csElement.IsReturnStructLarge)
            {
                ++realParameterStart;
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
                var prologBuilder = publicParameter.PassedByNativeReference
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
            yield return LocalDeclarationStatement(
                VariableDeclaration(ParseTypeName(publicElement.PublicType.QualifiedName))
                .AddVariables(
                    VariableDeclarator(Identifier(publicElement.Name))
                        .WithInitializer(
                            EqualsValueClause(
                                DefaultExpression(ParseTypeName(publicElement.PublicType.QualifiedName))))));

            if (marshaller.GeneratesMarshalVariable(publicElement))
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(marshaller.GetMarshalTypeSyntax(publicElement))
                    .AddVariables(
                        VariableDeclarator(generators.Marshalling.GetMarshalStorageLocationIdentifier(publicElement))
                        .WithInitializer(
                            nativeParameter != null
                            ? EqualsValueClause(
                                CastExpression(
                                    marshaller.GetMarshalTypeSyntax(publicElement),
                                    nativeParameter))
                            : null)));
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
                    globalNamespace.GetTypeNameSyntax(BuiltinType.Unsafe),
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
