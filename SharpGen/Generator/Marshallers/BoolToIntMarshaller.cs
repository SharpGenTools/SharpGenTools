using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    class BoolToIntMarshaller : MarshallerBase, IMarshaller
    {
        public BoolToIntMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement.IsBoolToInt && !csElement.IsArray;
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
            if (csElement is CsField)
            {
                return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    GetMarshalStorageLocation(csElement),
                    IdentifierName(csElement.IntermediateMarshalName)));
            }
            else
            {
                return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        GetMarshalStorageLocation(csElement),
                        CastExpression(ParseTypeName(csElement.MarshalType.QualifiedName),
                            ParenthesizedExpression(
                                ConditionalExpression(IdentifierName(csElement.Name),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))
                        ));
            }
        }

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            yield return LocalDeclarationStatement(
                VariableDeclaration(ParseTypeName(csElement.MarshalType.QualifiedName),
                    SingletonSeparatedList(VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)))));
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            if (csElement.PassedByNativeReference)
            {
                return Argument(PrefixUnaryExpression(SyntaxKind.AddressOfExpression, GetMarshalStorageLocation(csElement)));
            }
            return Argument(GetMarshalStorageLocation(csElement));
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            return null;
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            if (csElement is CsField)
            {
                return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(csElement.IntermediateMarshalName),
                    GetMarshalStorageLocation(csElement)));
            }
            else
            {
                return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.Name),
                        BinaryExpression(
                            SyntaxKind.NotEqualsExpression,
                            GetMarshalStorageLocation(csElement),
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(0)))));
            }
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
