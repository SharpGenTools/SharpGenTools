using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class MethodCodeGenerator : IMultiCodeGenerator<CsMethod, MemberDeclarationSyntax>
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
                var defaultOffset = csElement.Offset;

                if ((Generators.Config.Platforms & PlatformDetectionType.IsWindows) != 0)
                {
                    // Use the Windows offset for the default offset in the custom vtable when the Windows platform is requested for compat reasons.
                    defaultOffset = csElement.WindowsOffset;
                }

                yield return FieldDeclaration(
                    default,
                    TokenList(Token(SyntaxKind.PrivateKeyword)),
                    VariableDeclaration(
                        PredefinedType(Token(SyntaxKind.IntKeyword)),
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier($"{csElement.Name}__vtbl_index"),
                                null,
                                EqualsValueClause(
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(defaultOffset))
                                )
                            )
                        )
                    )
                );
            }

            // If not hidden, generate body
            if (csElement.Hidden.HasValue && csElement.Hidden.Value)
                yield break;

            foreach (var member in Generators.Callable.GenerateCode(csElement))
                yield return member;
        }
    }
}
