using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    class ArrayOfInterfaceMarshaller : MarshallerBase, IMarshaller
    {
        public ArrayOfInterfaceMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement.IsArray && csElement.IsInterface;
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
                   MarshalInterfaceInstanceToNative(csElement, publicElement, marshalElement));
        }

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            yield return LocalDeclarationStatement(
               VariableDeclaration(
                   PointerType(
                       QualifiedName(
                           IdentifierName("System"),
                           IdentifierName("IntPtr"))),
                   SingletonSeparatedList(
                       VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)))));
                    yield return ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(GetMarshalStorageLocationIdentifier(csElement)),
                            CastExpression(
                                PointerType(
                                    QualifiedName(
                                        IdentifierName("System"),
                                        IdentifierName("IntPtr"))),
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(0)))));
            yield return GenerateNullCheckIfNeeded(csElement,
                Block(
                    LocalDeclarationStatement(
                        VariableDeclaration(
                            PointerType(
                                QualifiedName(
                                    IdentifierName("System"),
                                    IdentifierName("IntPtr"))),
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier(csElement.IntermediateMarshalName))
                                .WithInitializer(
                                    EqualsValueClause(
                                        StackAllocArrayCreationExpression(
                                            ArrayType(
                                                QualifiedName(
                                                    IdentifierName("System"),
                                                    IdentifierName("IntPtr")),
                                                SingletonList(
                                                        ArrayRankSpecifier(
                                                            SingletonSeparatedList<ExpressionSyntax>(
                                                                MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    IdentifierName(csElement.Name),
                                                                    IdentifierName("Length")))))))))))),
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(GetMarshalStorageLocationIdentifier(csElement)),
                            IdentifierName(csElement.IntermediateMarshalName)))));
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            return Argument(CastExpression(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))), GetMarshalStorageLocation(csElement)));
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            return GenerateGCKeepAlive(csElement);
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            return LoopThroughArrayParameter(
               csElement,
               (publicElement, marshalElement) =>
                   MarshalInterfaceInstanceFromNative(csElement, publicElement, marshalElement));
        }

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
        {
            yield return GenerateArrayNativeToManagedExtendedProlog(csElement);
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            return null;
        }

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement)
        {
            return true;
        }

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement)
        {
            return PointerType(IntPtrType);
        }
    }
}
