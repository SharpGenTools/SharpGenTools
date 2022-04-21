using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers;

internal sealed class BoolToIntArrayMarshaller : ArrayMarshallerBase
{
    public override bool CanMarshal(CsMarshalBase csElement) => csElement.IsBoolToInt && csElement.IsArray;

    public override StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
    {
        var marshalStorage = GetMarshalStorageLocation(csElement);

        // TODO: Reverse-callback support?
        StatementSyntax value;
        if (singleStackFrame)
            value = EmitConvertToIntArray(marshalStorage);
        else
        {
            marshalStorage = PrefixUnaryExpression(SyntaxKind.AddressOfExpression, marshalStorage);

            value = FixedStatement(
                VariableDeclaration(
                    GetMarshalTypeSyntax(csElement),
                    SingletonSeparatedList(
                        VariableDeclarator(PtrIdentifier, default, EqualsValueClause(marshalStorage))
                    )
                ),
                EmitConvertToIntArray(PtrIdentifierName)
            );
        }

        return GenerateNullCheckIfNeeded(csElement, value);

        ExpressionStatementSyntax EmitConvertToIntArray(ExpressionSyntax destination) => ExpressionStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    GlobalNamespace.GetTypeNameSyntax(WellKnownName.BooleanHelpers),
                    IdentifierName("ConvertToIntArray")
                ),
                ArgumentList(
                    SeparatedList(
                        new[]
                        {
                            Argument(IdentifierName(csElement.Name)),
                            Argument(destination)
                        }
                    )
                )
            )
        );
    }

    public override StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) => null;

    public override StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
    {
        var marshalStorage = GetMarshalStorageLocation(csElement);

        StatementSyntax value;
        if (singleStackFrame)
        {
            value = EmitConvertToBoolArray(marshalStorage);
        }
        else if (csElement is CsField)
        {
            marshalStorage = PrefixUnaryExpression(SyntaxKind.AddressOfExpression, marshalStorage);

            value = FixedStatement(
                VariableDeclaration(
                    GetMarshalTypeSyntax(csElement),
                    SingletonSeparatedList(
                        VariableDeclarator(PtrIdentifier, default, EqualsValueClause(marshalStorage))
                    )
                ),
                EmitConvertToBoolArray(PtrIdentifierName)
            );
        }
        else // Reverse-callbacks
        {
            value = EmitConvertToBoolArray(marshalStorage);
        }

        return GenerateNullCheckIfNeeded(csElement, value);

        ExpressionStatementSyntax EmitConvertToBoolArray(ExpressionSyntax storage) => ExpressionStatement(
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    GlobalNamespace.GetTypeNameSyntax(WellKnownName.BooleanHelpers),
                    IdentifierName("ConvertToBoolArray")
                ),
                ArgumentList(
                    SeparatedList(
                        new[]
                        {
                            Argument(storage),
                            Argument(IdentifierName(csElement.Name))
                        }
                    )
                )
            )
        );
    }

    protected override TypeSyntax GetMarshalElementTypeSyntax(CsMarshalBase csElement) =>
        ParseTypeName(csElement.MarshalType.QualifiedName);

    public BoolToIntArrayMarshaller(Ioc ioc) : base(ioc)
    {
    }
}