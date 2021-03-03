using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal class RemappedTypeMarshaller : MarshallerBase, IMarshaller
    {
        public RemappedTypeMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement) => csElement.MappedToDifferentPublicType;

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) =>
            GenerateManagedValueTypeArgument(csElement);

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement) =>
            GenerateManagedValueTypeParameter(csElement);

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    GetMarshalStorageLocation(csElement),
                    CastExpression(GetMarshalTypeSyntax(csElement), IdentifierName(csElement.Name))
                )
            );

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    GetMarshalTypeSyntax(csElement),
                    SingletonSeparatedList(VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)))
                )
            );
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) => Argument(
            csElement.PassedByNativeReference
                ? PrefixUnaryExpression(SyntaxKind.AddressOfExpression, GetMarshalStorageLocation(csElement))
                : GetMarshalStorageLocation(csElement)
        );

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) => null;

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(csElement.Name),
                    CastExpression(
                        ParseTypeName(csElement.PublicType.QualifiedName), GetMarshalStorageLocation(csElement)
                    )
                )
            );

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
            Enumerable.Empty<StatementSyntax>();

        public FixedStatementSyntax GeneratePin(CsParameter csElement) => null;

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => true;

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) =>
            ParseTypeName(csElement.MarshalType.QualifiedName);
    }
}
