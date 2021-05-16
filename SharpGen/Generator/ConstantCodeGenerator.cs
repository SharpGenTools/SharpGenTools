using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator
{
    internal sealed class ConstantCodeGenerator : MemberCodeGeneratorBase<CsVariable>
    {
        public override IEnumerable<MemberDeclarationSyntax> GenerateCode(CsVariable var)
        {
            yield return AddDocumentationTrivia(
                FieldDeclaration(
                        VariableDeclaration(
                            IdentifierName(var.TypeName),
                            SingletonSeparatedList(
                                VariableDeclarator(Identifier(var.Name))
                                   .WithInitializer(EqualsValueClause(ParseExpression(var.Value)))
                            )))
                   .WithModifiers(var.VisibilityTokenList),
                var
            );
        }

        public ConstantCodeGenerator(Ioc ioc) : base(ioc)
        {
        }
    }
}
