﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator.Marshallers
{
    class NullableInstanceMarshaller : MarshallerBase, IMarshaller
    {
        public NullableInstanceMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement.PassedByNullableInstance;
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
            return GenerateNullCheckIfNeeded(csElement,
                ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                    GetMarshalStorageLocation(csElement),
                    MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName(csElement.Name),
                        IdentifierName("Value")))));
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            return Argument(GenerateNullCheckIfNeeded(csElement,
                PrefixUnaryExpression(SyntaxKind.AddressOfExpression,
                    GetMarshalStorageLocation(csElement)),
                CastExpression(
                    PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword))),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))));
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            return null;
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                IdentifierName(csElement.Name),
                GetMarshalStorageLocation(csElement)));
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            return null;
        }
    }
}