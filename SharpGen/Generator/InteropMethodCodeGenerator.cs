using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SharpGen.Generator
{
    class InteropMethodCodeGenerator : IMultiCodeGenerator<InteropMethodSignature, MethodDeclarationSyntax>
    {
        public IEnumerable<MethodDeclarationSyntax> GenerateCode(InteropMethodSignature csElement)
        {
            var method = MethodDeclaration(ParseTypeName(csElement.ReturnType.TypeName), csElement.Name)
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.UnsafeKeyword)));
            if (!csElement.IsFunction)
            {
                method = method.AddParameterListParameters(
                    Parameter(Identifier("thisObject"))
                        .WithType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))));
            }

            method = method.AddParameterListParameters(
                csElement.ParameterTypes.Select(param =>
                    Parameter(Identifier(param.Name))
                        .WithType(param.InteropTypeSyntax)).ToArray());

            method = method.AddParameterListParameters(
                Parameter(Identifier(csElement.IsFunction ? "funcPtr" : "methodPtr"))
                    .WithType(PointerType(PredefinedType(Token(SyntaxKind.VoidKeyword)))));

            yield return method.WithBody(Block(ThrowStatement(LiteralExpression(SyntaxKind.NullLiteralExpression))));
        }
    }
}
