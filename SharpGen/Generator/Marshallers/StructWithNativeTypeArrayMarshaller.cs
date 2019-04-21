using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    class StructWithNativeTypeArrayMarshaller : MarshallerBase, IMarshaller
    {
        public StructWithNativeTypeArrayMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement.HasNativeValueType && csElement.IsArray;
        }

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement)
        {
            return Argument(IdentifierName(csElement.Name));
        }

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement)
        {
            return GenerateManagedArrayParameter(csElement);
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
        {
            return LoopThroughArrayParameter(
                csElement,
                (publicElement, marshalElement) =>
                {
                    var marshalTo = CreateMarshalStructStatement(
                        csElement,
                        StructMarshalMethod.To,
                        publicElement,
                        marshalElement);
                    if (((CsStruct)csElement.PublicType).HasCustomNew)
                    {
                        return Block(
                            CreateMarshalCustomNewStatement(csElement, marshalElement),
                            marshalTo
                            );
                    }
                    return marshalTo;
                });
        }

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    ArrayType(ParseTypeName($"{csElement.PublicType.QualifiedName}.__Native"), SingletonList(ArrayRankSpecifier())),
                    SingletonSeparatedList(
                        VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement))
                            .WithInitializer(EqualsValueClause(
                                GenerateNullCheckIfNeeded(csElement,
                                    ObjectCreationExpression(
                                        ArrayType(ParseTypeName($"{csElement.PublicType.QualifiedName}.__Native"),
                                        SingletonList(ArrayRankSpecifier(
                                            SingletonSeparatedList<ExpressionSyntax>(
                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName(csElement.Name),
                                                    IdentifierName("Length"))))))),
                                    LiteralExpression(SyntaxKind.NullLiteralExpression)))))));
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            return Argument(IdentifierName(csElement.IntermediateMarshalName));
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            return LoopThroughArrayParameter(csElement,
               (publicElement, marshalElement) =>
                   CreateMarshalStructStatement(csElement, StructMarshalMethod.Free, publicElement, marshalElement));
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            return LoopThroughArrayParameter(csElement,
               (publicElement, marshalElement) =>
                   CreateMarshalStructStatement(csElement, StructMarshalMethod.From, publicElement, marshalElement));
        }

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
        {
            yield return GenerateArrayNativeToManagedExtendedProlog(csElement);
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            return FixedStatement(VariableDeclaration(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
               SingletonSeparatedList(
                   VariableDeclarator(Identifier(csElement.IntermediateMarshalName)).WithInitializer(EqualsValueClause(
                       GetMarshalStorageLocation(csElement)
                       )))), EmptyStatement());
        }

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement)
        {
            return true;
        }

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement)
        {
            return PointerType(ParseTypeName($"{csElement.PublicType.QualifiedName}.__Native"));
        }
    }
}
