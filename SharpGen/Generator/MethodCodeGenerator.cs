using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class MethodCodeGenerator : MemberMultiCodeGeneratorBase<CsMethod>
    {
        public MethodCodeGenerator(Ioc ioc) : base(ioc)
        {
        }

        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsMethod csElement)
        {
            var list = NewMemberList;
            if (csElement.CustomVtbl)
                list.Add(
                    FieldDeclaration(
                        default,
                        TokenList(Token(SyntaxKind.PrivateKeyword)),
                        VariableDeclaration(
                            PredefinedType(Token(SyntaxKind.UIntKeyword)),
                            SingletonSeparatedList(
                                VariableDeclarator(
                                    Identifier($"{csElement.Name}__vtbl_index"),
                                    null,
                                    EqualsValueClause(csElement.VTableOffsetExpression(Generators.Config.Platforms))
                                )
                            )
                        )
                    )
                );

            // If not hidden, generate body
            if (!csElement.Hidden)
                list.Add(csElement, Generators.Callable);

            return list;
        }
    }
}