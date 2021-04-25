using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal class StructWithNativeTypeMarshaller : MarshallerBase, IMarshaller
    {
        public StructWithNativeTypeMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement) => csElement.HasNativeValueType && !csElement.IsArray;

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) =>
            GenerateManagedValueTypeArgument(csElement);

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement) =>
            GenerateManagedValueTypeParameter(csElement);

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
        {
            ExpressionSyntax publicElementExpression = IdentifierName(csElement.Name);

            if (csElement is CsParameter {IsNullableStruct: true})
            {
                publicElementExpression = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    publicElementExpression,
                    IdentifierName("Value"));
            }
            var marshalToStatement = CreateMarshalStructStatement(
                csElement,
                StructMarshalMethod.To,
                publicElementExpression,
                GetMarshalStorageLocation(csElement));

            return csElement.PublicType switch
            {
                CsStruct {HasCustomNew: true} => Block(
                    CreateMarshalCustomNewStatement(csElement, GetMarshalStorageLocation(csElement)),
                    marshalToStatement
                ),
                _ => marshalToStatement
            };
        }

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            var defaultSyntax = LiteralExpression(SyntaxKind.DefaultLiteralExpression, Token(SyntaxKind.DefaultKeyword));

            yield return LocalDeclarationStatement(
                VariableDeclaration(
                    GetMarshalTypeSyntax(csElement),
                    SingletonSeparatedList(
                        VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement))
                           .WithInitializer(
                                EqualsValueClause(defaultSyntax))
                    )
                )
            );
            if (csElement.IsOut)
            {
                yield return ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.Name),
                        defaultSyntax
                    )
                );
            }
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) => Argument(
            csElement.PassedByNativeReference
                ? GenerateNullCheckIfNeeded(
                    csElement,
                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression, GetMarshalStorageLocation(csElement)),
                    CastExpression(
                        VoidPtrType,
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))
                    )
                )
                : GetMarshalStorageLocation(csElement)
        );

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            ExpressionSyntax publicElementExpression = IdentifierName(csElement.Name);

            if (csElement is CsParameter {IsNullableStruct: true})
            {
                publicElementExpression = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    publicElementExpression,
                    IdentifierName("Value"));
            }

            return CreateMarshalStructStatement(
                    csElement,
                    StructMarshalMethod.Free,
                    publicElementExpression,
                    GetMarshalStorageLocation(csElement)
            );
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            ExpressionSyntax publicElementExpression = IdentifierName(csElement.Name);

            if (csElement is CsParameter {IsNullableStruct: true})
            {
                publicElementExpression = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    publicElementExpression,
                    IdentifierName("Value"));
            }

            return CreateMarshalStructStatement(
                    csElement,
                    StructMarshalMethod.From,
                    publicElementExpression,
                    GetMarshalStorageLocation(csElement)
            );
        }

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
            Enumerable.Empty<StatementSyntax>();

        public FixedStatementSyntax GeneratePin(CsParameter csElement) => null;

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => true;

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) =>
            ParseTypeName($"{csElement.PublicType.QualifiedName}.__Native");
    }
}
