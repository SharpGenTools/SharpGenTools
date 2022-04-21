using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers;

internal sealed class NullableInstanceMarshaller : MarshallerBase, IMarshaller
{
    public bool CanMarshal(CsMarshalBase csElement) =>
        csElement is CsParameter {PassedByNullableInstance: true, HasNativeValueType: false};

    public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) =>
        GenerateManagedValueTypeArgument(csElement);

    public ParameterSyntax GenerateManagedParameter(CsParameter csElement) =>
        GenerateManagedValueTypeParameter(csElement);

    public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
        GenerateNullCheckIfNeeded(
            csElement,
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    GetMarshalStorageLocation(csElement),
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(csElement.Name), IdentifierName("Value")
                    )
                )
            )
        );

    public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
    {
        yield return LocalDeclarationStatement(
            VariableDeclaration(
                ParseTypeName(csElement.PublicType.QualifiedName),
                SingletonSeparatedList(VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement)))
            )
        );
    }

    public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) => Argument(
        GenerateNullCheckIfNeeded(
            csElement,
            PrefixUnaryExpression(SyntaxKind.AddressOfExpression, GetMarshalStorageLocation(csElement)),
            CastExpression(VoidPtrType, ZeroLiteral)
        )
    );

    public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) => null;

    public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
        ExpressionStatement(
            AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(csElement.Name), GetMarshalStorageLocation(csElement)
            )
        );

    public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
        Enumerable.Empty<StatementSyntax>();

    public FixedStatementSyntax GeneratePin(CsParameter csElement) => null;

    public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => true;

    public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) =>
        ParseTypeName(csElement.MarshalType.QualifiedName);

    public NullableInstanceMarshaller(Ioc ioc) : base(ioc)
    {
    }
}