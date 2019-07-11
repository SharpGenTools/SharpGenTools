using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

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
            int defaultOffset = csElement.Offset;
            if ((Generators.Config.Platforms & PlatformDetectionType.IsWindows) != 0)
            {
                // Use the Windows offset for the default offset in the custom vtable when the Windows platform is requested for compat reasons.
                defaultOffset = csElement.WindowsOffset;
            }
            if (csElement.CustomVtbl)
            {
                yield return FieldDeclaration(
                    VariableDeclaration(PredefinedType(Token(SyntaxKind.IntKeyword)),
                        SingletonSeparatedList(
                            VariableDeclarator($"{csElement.Name}__vtbl_index")
                                .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(defaultOffset))))))) 
                    .WithModifiers(TokenList(Token(SyntaxKind.PrivateKeyword)));
            }

            // If not hidden, generate body
            if (csElement.Hidden)
            {
                yield break;
            }

            foreach (var member in Generators.Callable.GenerateCode(csElement))
            {
                yield return member;
            }
        }
    }
}
