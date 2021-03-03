using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal class StructWithNativeTypeArrayMarshaller : MarshallerBase, IMarshaller
    {
        public StructWithNativeTypeArrayMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement) => csElement.HasNativeValueType && csElement.IsArray;

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) =>
            Argument(IdentifierName(csElement.Name));

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement) =>
            GenerateManagedArrayParameter(csElement);

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
            LoopThroughArrayParameter(
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

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            var elementType = ParseTypeName($"{csElement.PublicType.QualifiedName}.__Native");
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    ArrayType(elementType, SingletonList(ArrayRankSpecifier())),
                    SingletonSeparatedList(
                        VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement))
                            .WithInitializer(EqualsValueClause(
                                GenerateNullCheckIfNeeded(csElement,
                                                          ObjectCreationExpression(
                                                              ArrayType(elementType,
                                                                        SingletonList(ArrayRankSpecifier(
                                                                            SingletonSeparatedList<ExpressionSyntax>(
                                                                                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                                                    IdentifierName(csElement.Name),
                                                                                    IdentifierName("Length"))))))),
                                                          LiteralExpression(SyntaxKind.NullLiteralExpression)))))));
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) =>
            Argument(IdentifierName(csElement.IntermediateMarshalName));

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) =>
            LoopThroughArrayParameter(
                csElement,
                (publicElement, marshalElement) =>
                    CreateMarshalStructStatement(csElement, StructMarshalMethod.Free, publicElement, marshalElement)
            );

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
            LoopThroughArrayParameter(
                csElement,
                (publicElement, marshalElement) =>
                    CreateMarshalStructStatement(csElement, StructMarshalMethod.From, publicElement, marshalElement)
            );

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
        {
            yield return GenerateArrayNativeToManagedExtendedProlog(csElement);
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement) => FixedStatement(
            VariableDeclaration(
                VoidPtrType,
                SingletonSeparatedList(
                    VariableDeclarator(
                        Identifier(csElement.IntermediateMarshalName),
                        null,
                        EqualsValueClause(GetMarshalStorageLocation(csElement))
                    )
                )
            ),
            EmptyStatement()
        );

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => true;

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) =>
            PointerType(ParseTypeName($"{csElement.PublicType.QualifiedName}.__Native"));
    }
}
