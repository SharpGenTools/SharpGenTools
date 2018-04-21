using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator
{
    class ReverseCallablePrologCodeGenerator : MarshallingCodeGeneratorBase, IMultiCodeGenerator<CsCallable, StatementSyntax>
    {
        private readonly GlobalNamespaceProvider globalNamespace;

        public ReverseCallablePrologCodeGenerator(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
            this.globalNamespace = globalNamespace;
        }

        public IEnumerable<StatementSyntax> GenerateCode(CsCallable csElement)
        {
            var interopParameters = csElement.Interop.ParameterTypes;
            var realParameterStart = 0;
            if (csElement.IsReturnStructLarge)
            {
                ++realParameterStart;
                foreach (var statement in GenerateProlog(csElement.ReturnValue, Identifier("returnSlot")))
                {
                    yield return statement;
                }
            }

            for (int i = 0; i < csElement.Parameters.Count; i++)
            {
                var publicParameter = csElement.Parameters[i];
                foreach (var statement in GenerateProlog(publicParameter, Identifier($"param{i}")))
                {
                    yield return statement;
                }
            }
        }

        private IEnumerable<StatementSyntax> GenerateProlog(
            CsMarshalCallableBase publicElement,
            SyntaxToken nativeParameter)
        {
            if (!NeedsMarshalling(publicElement) && (publicElement.IsRef || publicElement.IsOut))
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(RefType(ParseTypeName(publicElement.PublicType.QualifiedName)))
                    .AddVariables(
                        VariableDeclarator(Identifier(publicElement.Name))
                        .WithInitializer(EqualsValueClause(
                            RefExpression(
                                PrefixUnaryExpression(SyntaxKind.PointerIndirectionExpression,
                                    ParenthesizedExpression(
                                        CastExpression(
                                            PointerType(GetMarshalTypeSyntax(publicElement)),
                                            IdentifierName(nativeParameter)))))))));
            }
            else
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(ParseTypeName(publicElement.PublicType.QualifiedName))
                    .AddVariables(
                        VariableDeclarator(Identifier(publicElement.Name)))); 
            }

            if (publicElement.IsValueType && !publicElement.IsPrimitive && !publicElement.IsArray)
            {
                yield return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(publicElement.Name),
                        DefaultExpression(ParseTypeName(publicElement.PublicType.QualifiedName))
                ));
            }

            if (publicElement.IsInterfaceArray)
            {
                yield return NotImplemented("Arrays of interfaces");
            }
            else if (publicElement.IsArray)
            {
                yield return NotImplemented("Arrays");
            }
            else if (publicElement.IsInterface && publicElement.IsIn)
            {
                yield return LocalDeclarationStatement(
                    VariableDeclaration(
                        QualifiedName(
                            IdentifierName("System"),
                            IdentifierName("IntPtr")))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                GetMarshalStorageLocationIdentifier(publicElement))
                            .WithInitializer(
                                EqualsValueClause(
                                    CastExpression(
                                        QualifiedName(
                                            IdentifierName("System"),
                                            IdentifierName("IntPtr")),
                                        IdentifierName(nativeParameter)))))));
            }
            else if (publicElement.IsRefIn || publicElement.IsRef || publicElement.IsOut)
            {
                var refToNativeExpression = RefExpression(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            globalNamespace.GetTypeNameSyntax(BuiltinType.Unsafe),
                            GenericName(Identifier("AsRef"))
                            .WithTypeArgumentList(
                                TypeArgumentList(
                                    SingletonSeparatedList(
                                        GetMarshalTypeSyntax(publicElement))))))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    IdentifierName(nativeParameter))))));

                if (NeedsMarshalling(publicElement))
                {
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(
                            RefType(GetMarshalTypeSyntax(publicElement)))
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    GetMarshalStorageLocationIdentifier(publicElement))
                                .WithInitializer(
                                    EqualsValueClause(
                                        refToNativeExpression))))); 
                }
                else
                {
                    yield return ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(publicElement.Name),
                            refToNativeExpression));
                }
            }
            else
            {
                if (NeedsMarshalling(publicElement))
                {
                    yield return LocalDeclarationStatement(
                        VariableDeclaration(GetMarshalTypeSyntax(publicElement))
                        .AddVariables(
                            VariableDeclarator(GetMarshalStorageLocationIdentifier(publicElement))
                            .WithInitializer(
                                EqualsValueClause(
                                    IdentifierName(nativeParameter)))));
                }
                else
                {
                    yield return ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(publicElement.Name),
                            IdentifierName(nativeParameter)));
                }
            }
        }
    }
}
