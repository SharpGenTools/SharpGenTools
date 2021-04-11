using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal class ValueTypeMarshaller : MarshallerBase, IMarshaller
    {
        public ValueTypeMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement) => csElement.IsValueType && !csElement.IsArray &&
                                                           !csElement.MappedToDifferentPublicType &&
                                                           csElement is not CsField;

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) =>
            GenerateManagedValueTypeArgument(csElement);

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement) =>
            GenerateManagedValueTypeParameter(csElement);

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) => null;

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            if (csElement.IsOut && !csElement.IsPrimitive && !csElement.UsedAsReturn)
            {
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.Name),
                        LiteralExpression(SyntaxKind.DefaultLiteralExpression, Token(SyntaxKind.DefaultKeyword))
                    )
                );
            }
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) => Argument(
            csElement.PassedByNativeReference
                ? csElement.IsFixed && !csElement.UsedAsReturn
                      ? GetMarshalStorageLocation(csElement)
                      : PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(csElement.Name))
                : IdentifierName(csElement.Name)
        );

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) => null;

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) => null;

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
            Enumerable.Empty<StatementSyntax>();

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            if (csElement.IsFixed && !csElement.IsUsedAsReturnType)
            {
                return FixedStatement(VariableDeclaration(VoidPtrType,
                    SingletonSeparatedList(
                        VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)).WithInitializer(EqualsValueClause(
                            PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                                IdentifierName(csElement.Name))
                            )))), EmptyStatement());
            }
            return null;
        }

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => false;

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) =>
            ParseTypeName(csElement.PublicType.QualifiedName);
    }
}
