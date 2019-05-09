using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    class StructWithNativeTypeMarshaller : MarshallerBase, IMarshaller
    {
        public StructWithNativeTypeMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement.HasNativeValueType && !csElement.IsArray;
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
            ExpressionSyntax publicElementExpression = IdentifierName(csElement.Name);

            if (csElement.IsNullableStruct)
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

            if (((CsStruct)csElement.PublicType).HasCustomNew)
            {
                return Block(
                    CreateMarshalCustomNewStatement(csElement, GetMarshalStorageLocation(csElement)),
                    marshalToStatement);
            }
            return marshalToStatement;
        }

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            yield return LocalDeclarationStatement(
                VariableDeclaration(ParseTypeName($"{csElement.PublicType.QualifiedName}.__Native"),
                   SingletonSeparatedList(
                       VariableDeclarator(GetMarshalStorageLocationIdentifier(csElement))
                       .WithInitializer(
                           EqualsValueClause(
                               DefaultExpression(ParseTypeName($"{csElement.PublicType.QualifiedName}.__Native")))))));
            if (csElement.IsOut)
            {
                yield return ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(csElement.Name),
                        DefaultExpression(ParseTypeName(csElement.PublicType.QualifiedName))
                ));
            }
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            if (csElement.PassedByNativeReference)
            {
                return Argument(GenerateNullCheckIfNeeded(
                    csElement,
                    PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                        GetMarshalStorageLocation(csElement)),
                        CastExpression(
                                PointerType(
                                    PredefinedType(
                                        Token(SyntaxKind.VoidKeyword))),
                                LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    Literal(0)))
                        ));
            }
            return Argument(GetMarshalStorageLocation(csElement));
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            ExpressionSyntax publicElementExpression = IdentifierName(csElement.Name);

            if (csElement.IsNullableStruct)
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

            if (csElement.IsNullableStruct)
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
            return ParseTypeName($"{csElement.PublicType.QualifiedName}.__Native");
        }
    }
}
