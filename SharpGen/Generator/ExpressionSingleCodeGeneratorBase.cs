using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator;

internal abstract class ExpressionSingleCodeGeneratorBase<T> : StatementCodeGeneratorBase<T>,
                                                               ISingleCodeGenerator<T, StatementSyntax>,
                                                               ISingleCodeGenerator<T, ExpressionSyntax>
    where T : CsBase
{
    protected ExpressionSingleCodeGeneratorBase(Ioc ioc) : base(ioc)
    {
    }

    StatementSyntax ISingleCodeGenerator<T, StatementSyntax>.GenerateCode(T csElement) =>
        Generate(csElement) is { } value ? SyntaxFactory.ExpressionStatement(value) : null;

    ExpressionSyntax ISingleCodeGenerator<T, ExpressionSyntax>.GenerateCode(T csElement) => Generate(csElement);

    protected abstract ExpressionSyntax Generate(T csElement);
}