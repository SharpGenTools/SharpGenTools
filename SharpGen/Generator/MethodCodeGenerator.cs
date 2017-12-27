using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace SharpGen.Generator
{
    class MethodCodeGenerator : IMultiCodeGenerator<CsMethod, MemberDeclarationSyntax>
    {
        public MethodCodeGenerator(IGeneratorRegistry generators)
        {
            Generators = generators;
        }

        public IGeneratorRegistry Generators { get; }

        public IEnumerable<MemberDeclarationSyntax> GenerateCode(CsMethod csElement)
        {
            if (csElement.CustomVtbl)
            {
                yield return FieldDeclaration(
                    VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                        SingletonSeparatedList(
                            VariableDeclarator($"{csElement.Name}__vtbl_index")
                                .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(csElement.Offset)))))))
                    .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));
            }

            foreach (var member in Generators.Callable.GenerateCode(csElement))
            {
                yield return member;
            }
        }
    }
}
