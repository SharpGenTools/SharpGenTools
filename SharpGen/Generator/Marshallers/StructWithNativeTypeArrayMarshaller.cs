using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SharpGen.Generator.Marshallers
{
    internal sealed class StructWithNativeTypeArrayMarshaller : ArrayMarshallerBase
    {
        public override bool CanMarshal(CsMarshalBase csElement) => csElement.HasNativeValueType && csElement.IsArray;

        public override StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
            LoopThroughArrayParameter(
                csElement,
                (publicElement, marshalElement) =>
                    GenerateMarshalStructManagedToNative(csElement, publicElement, marshalElement)
            );

        public override StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) =>
            LoopThroughArrayParameter(
                csElement,
                (publicElement, marshalElement) =>
                    CreateMarshalStructStatement(csElement, StructMarshalMethod.Free, publicElement, marshalElement)
            );

        public override StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
            LoopThroughArrayParameter(
                csElement,
                (publicElement, marshalElement) =>
                    CreateMarshalStructStatement(csElement, StructMarshalMethod.From, publicElement, marshalElement)
            );

        protected override TypeSyntax GetMarshalElementTypeSyntax(CsMarshalBase csElement) =>
            ParseTypeName($"{csElement.PublicType.QualifiedName}.__Native");

        public StructWithNativeTypeArrayMarshaller(Ioc ioc) : base(ioc)
        {
        }
    }
}
