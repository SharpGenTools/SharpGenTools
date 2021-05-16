using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed class ValueTypeMarshaller : ValueTypeMarshallerBase
    {
        protected override bool CanMarshal(CsMarshalCallableBase csElement) =>
            csElement.IsValueType && !csElement.MappedToDifferentPublicType;

        public override IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            if (csElement.IsOut && !csElement.IsPrimitive && !csElement.UsedAsReturn)
            {
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression, IdentifierName(csElement.Name), DefaultLiteral
                    )
                );
            }
        }

        public override ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) => Argument(
            csElement.PassedByNativeReference
                ? csElement.IsFixed && !csElement.UsedAsReturn
                      ? GetMarshalStorageLocation(csElement)
                      : PrefixUnaryExpression(SyntaxKind.AddressOfExpression, IdentifierName(csElement.Name))
                : IdentifierName(csElement.Name)
        );

        public override FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            if (csElement.IsFixed && !csElement.UsedAsReturn)
            {
                return FixedStatement(
                    VariableDeclaration(
                        VoidPtrType,
                        SingletonSeparatedList(
                            VariableDeclarator(
                                GetMarshalStorageLocationIdentifier(csElement),
                                null,
                                EqualsValueClause(
                                    PrefixUnaryExpression(
                                        SyntaxKind.AddressOfExpression, IdentifierName(csElement.Name)
                                    )
                                )
                            )
                        )
                    ),
                    EmptyStatement()
                );
            }
            return null;
        }

        protected override CsTypeBase GetMarshalType(CsMarshalBase csElement) => csElement.PublicType;

        public ValueTypeMarshaller(Ioc ioc) : base(ioc)
        {
        }
    }
}
