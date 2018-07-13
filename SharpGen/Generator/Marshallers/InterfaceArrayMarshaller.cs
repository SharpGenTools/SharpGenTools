using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator.Marshallers
{
    class InterfaceArrayMarshaller : IMarshaller
    {
        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement.IsInterfaceArray;
        }

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement)
        {
            return Argument(IdentifierName(csElement.Name));
        }

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement)
        {
            return Parameter(Identifier(csElement.Name)).WithType(ParseTypeName(csElement.PublicType.QualifiedName));
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
        {
            return null;
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            return Argument(CastExpression(
               PointerType(
                   PredefinedType(
                       Token(SyntaxKind.VoidKeyword))),
               ParenthesizedExpression(
                   BinaryExpression(
                       SyntaxKind.CoalesceExpression,
                       ConditionalAccessExpression(
                           IdentifierName(csElement.Name),
                           MemberBindingExpression(
                               IdentifierName("NativePointer"))),
                       MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                           MemberAccessExpression(
                               SyntaxKind.SimpleMemberAccessExpression,
                               IdentifierName("System"),
                               IdentifierName("IntPtr")),
                           IdentifierName("Zero"))))));
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            return null;
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            return null;
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            return null;
        }
    }
}
