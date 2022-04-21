using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers;

internal sealed class EnumParameterWrapperMarshaller : WrapperMarshallerBase
{
    public EnumParameterWrapperMarshaller(Ioc ioc, IMarshaller implementation) : base(ioc, implementation)
    {
    }

    public static bool IsApplicable(CsMarshalBase csElement) => csElement is CsParameter {PublicType: CsEnum};

    public override ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
    {
        var argument = base.GenerateNativeArgument(csElement);
        return csElement.PassedByNativeReference
                   ? argument
                   : Argument(
                       CheckedExpression(
                           SyntaxKind.UncheckedExpression,
                           GeneratorHelpers.CastExpression(
                               ParseTypeName(((CsEnum) csElement.PublicType).UnderlyingType.Name),
                               argument.Expression
                           )
                       )
                   );
    }
}