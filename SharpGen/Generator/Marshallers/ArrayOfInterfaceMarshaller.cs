using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal class ArrayOfInterfaceMarshaller : MarshallerBase, IMarshaller
    {
        public ArrayOfInterfaceMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement) => csElement.IsArray && csElement.IsInterface;

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) =>
            Argument(IdentifierName(csElement.Name));

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement) =>
            GenerateManagedArrayParameter(csElement);

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
            LoopThroughArrayParameter(
                csElement,
                (publicElement, marshalElement) =>
                    MarshalInterfaceInstanceToNative(csElement, publicElement, marshalElement));

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    PointerType(IntPtrType),
                    SingletonSeparatedList(VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)))
                )
            );
            yield return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(GetMarshalStorageLocationIdentifier(csElement)),
                    CastExpression(
                        PointerType(IntPtrType),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))
                    )
                )
            );
            yield return GenerateNullCheckIfNeeded(
                csElement,
                Block(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            PointerType(IntPtrType),
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier(csElement.IntermediateMarshalName),
                                    null,
                                    EqualsValueClause(
                                        StackAllocArrayCreationExpression(
                                            ArrayType(
                                                IntPtrType,
                                                SingletonList(
                                                    ArrayRankSpecifier(
                                                        SingletonSeparatedList<ExpressionSyntax>(
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName(csElement.Name),
                                                                IdentifierName("Length")
                                                            )
                                                        )
                                                    )
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    ),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(GetMarshalStorageLocationIdentifier(csElement)),
                            IdentifierName(csElement.IntermediateMarshalName)
                        )
                    )
                )
            );
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) =>
            Argument(CastExpression(VoidPtrType, GetMarshalStorageLocation(csElement)));

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) =>
            GenerateGCKeepAlive(csElement);

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
            LoopThroughArrayParameter(
                csElement,
                (publicElement, marshalElement) =>
                    MarshalInterfaceInstanceFromNative(csElement, publicElement, marshalElement)
            );

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
        {
            yield return GenerateArrayNativeToManagedExtendedProlog(csElement);
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement) => null;

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => true;

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) => PointerType(IntPtrType);
    }
}
