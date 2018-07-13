using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using Microsoft.CodeAnalysis.CSharp;

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
                "__MarshalTo",
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
                    "__MarshalFree",
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
                    "__MarshalFrom",
                    publicElementExpression,
                    GetMarshalStorageLocation(csElement)
            );
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            return null;
        }
    }
}
