using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    class NullableInstanceMarshaller : MarshallerBase, IMarshaller
    {
        public NullableInstanceMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement.PassedByNullableInstance && !csElement.HasNativeValueType;
        }

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement)
        {
            return GenerateManagedValueTypeArgument(csElement);
        }

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement)
        {
            return GenerateManagedValueTypeParameter(csElement);
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
        {
            return GenerateNullCheckIfNeeded(csElement,
                ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    GetMarshalStorageLocation(csElement),
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(csElement.Name),
                        IdentifierName("Value")))));
        }

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            yield return LocalDeclarationStatement(
               VariableDeclaration(ParseTypeName(csElement.PublicType.QualifiedName),
                   SingletonSeparatedList(
                       VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)))));
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            return Argument(GenerateNullCheckIfNeeded(csElement,
                PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                    GetMarshalStorageLocation(csElement)),
                CastExpression(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))));
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            return null;
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(csElement.Name),
                GetMarshalStorageLocation(csElement)));
        }

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
        {
            return Enumerable.Empty<StatementSyntax>();
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
            return ParseTypeName(csElement.MarshalType.QualifiedName);
        }
    }
}
