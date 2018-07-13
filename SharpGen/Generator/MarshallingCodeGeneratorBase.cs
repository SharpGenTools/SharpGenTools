using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    class MarshallingCodeGeneratorBase
    {
        private readonly GlobalNamespaceProvider globalNamespace;

        protected MarshallingCodeGeneratorBase(GlobalNamespaceProvider globalNamespace)
        {
            this.globalNamespace = globalNamespace;
        }

        protected SyntaxToken GetMarshalStorageLocationIdentifier(CsMarshalBase marshallable)
        {
            switch (marshallable)
            {
                case CsParameter _:
                    return Identifier($"{marshallable.Name}_");
                case CsField _:
                    throw new ArgumentException("Marshal storage location for a field cannot be represented by a token.", nameof(marshallable));
                case CsReturnValue returnValue:
                    return Identifier(returnValue.MarshalStorageLocation);
                default:
                    throw new ArgumentException(nameof(marshallable));
            }
        }

        protected ExpressionSyntax GetMarshalStorageLocation(CsMarshalBase marshallable)
        {
            switch (marshallable)
            {
                case CsParameter _:
                case CsReturnValue _:
                    return IdentifierName(GetMarshalStorageLocationIdentifier(marshallable));
                case CsField _:
                    return MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("@ref"),
                        IdentifierName(marshallable.Name));
                default:
                    throw new ArgumentException(nameof(marshallable));
            }
        }

        protected StatementSyntax NotImplemented(string message)
        {
            return ThrowStatement(
                ObjectCreationExpression(
                    ParseTypeName("System.NotImplementedException"))
                .WithArgumentList(
                    ArgumentList(
                        message == null ? default
                        : SingletonSeparatedList(
                            Argument(
                                LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(message)))))));
        }

        protected bool NeedsMarshalling(CsMarshalBase value)
        {
            return value.HasNativeValueType
                || value.IsBoolToInt
                || value.IsInterface
                || value.IsArray
                || value.IsString
                || value.MappedToDifferentPublicType
                && (value.PublicType.QualifiedName != globalNamespace.GetTypeName(WellKnownName.PointerSize));
        }

        protected TypeSyntax GetMarshalTypeSyntax(CsMarshalBase value)
        {
            if (value.HasNativeValueType)
            {
                return ParseTypeName($"{value.MarshalType.QualifiedName}.__Native");
            }
            else if (value.IsInterface)
            {
                return ParseTypeName("System.IntPtr");
            }
            return ParseTypeName(value.MarshalType.QualifiedName);
        }
    }
}
