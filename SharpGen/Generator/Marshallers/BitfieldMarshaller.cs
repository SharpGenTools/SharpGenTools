using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed class BitfieldMarshaller : FieldMarshallerBase
    {
        protected override bool AllowNativeCleanup => true;

        protected override bool CanMarshal(CsField csField) => csField.IsBitField;

        protected override StatementSyntax GenerateManagedToNative(CsField csField, bool singleStackFrame) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.OrAssignmentExpression,
                    GetMarshalStorageLocation(csField),
                    GeneratorHelpers.CastExpression(
                        ParseTypeName(csField.MarshalType.QualifiedName),
                        BinaryExpression(
                            SyntaxKind.BitwiseAndExpression,
                            IdentifierName(csField.IntermediateMarshalName),
                            LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                Literal(csField.BitMask << csField.BitOffset)
                            )
                        )
                    )
                )
            );

        protected override StatementSyntax GenerateNativeToManaged(CsField csField, bool singleStackFrame) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(csField.IntermediateMarshalName),
                    GetMarshalStorageLocation(csField)
                )
            );

        public BitfieldMarshaller(Ioc ioc) : base(ioc)
        {
        }
    }
}
