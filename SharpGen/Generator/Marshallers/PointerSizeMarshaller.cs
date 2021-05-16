using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed class PointerSizeMarshaller : ValueTypeMarshallerBase
    {
        protected override bool CanMarshal(CsMarshalCallableBase csElement) =>
            (csElement.PublicType.IsWellKnownType(GlobalNamespace, WellKnownName.PointerSize)
          || csElement.PublicType is CsFundamentalType {IsPointer: true})
         && csElement is CsParameter {IsIn: true} or CsReturnValue;

        public override IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement) =>
            Enumerable.Empty<StatementSyntax>();

        public override ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) => Argument(
            CastExpression(VoidPtrType, IdentifierName(csElement.Name))
        );

        public override FixedStatementSyntax GeneratePin(CsParameter csElement) => null;

        protected override CsTypeBase GetMarshalType(CsMarshalBase csElement) => csElement.MarshalType;

        public PointerSizeMarshaller(Ioc ioc) : base(ioc)
        {
        }
    }
}
