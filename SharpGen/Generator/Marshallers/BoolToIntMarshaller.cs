using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed class BoolToIntMarshaller : MarshallerBase, IMarshaller
    {
        public bool CanMarshal(CsMarshalBase csElement) => csElement.IsBoolToInt && !csElement.IsArray;

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) =>
            GenerateManagedValueTypeArgument(csElement);

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement) =>
            GenerateManagedValueTypeParameter(csElement);

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    GetMarshalStorageLocation(csElement),
                    csElement is CsField
                        ? IdentifierName(csElement.IntermediateMarshalName)
                        : GeneratorHelpers.CastExpression(
                            GetMarshalTypeSyntax(csElement),
                            GeneratorHelpers.GenerateBoolToIntConversion(IdentifierName(csElement.Name))
                        )
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
                csElement is CsField
                    ? AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.IntermediateMarshalName),
                        GetMarshalStorageLocation(csElement)
                    )
                    : AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.Name),
                        GeneratorHelpers.GenerateIntToBoolConversion(GetMarshalStorageLocation(csElement))
                    )
            );

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
            Enumerable.Empty<StatementSyntax>();

        public FixedStatementSyntax GeneratePin(CsParameter csElement) => null;

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => true;

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) =>
            ParseTypeName(csElement.MarshalType.QualifiedName);

        public BoolToIntMarshaller(Ioc ioc) : base(ioc)
        {
        }
    }
}
