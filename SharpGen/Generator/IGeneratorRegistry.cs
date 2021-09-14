using SharpGen.Model;

namespace SharpGen.Generator
{
    public interface IGeneratorRegistry
    {
        IMemberCodeGenerator<CsExpressionConstant> ExpressionConstant { get; }
        IMemberCodeGenerator<CsGuidConstant> GuidConstant { get; }
        IMemberCodeGenerator<CsResultConstant> ResultConstant { get; }
        IStatementCodeGenerator<CsResultConstant> ResultRegistration { get; }
        IMemberCodeGenerator<CsProperty> Property { get; }
        IMemberCodeGenerator<CsEnum> Enum { get; }
        IMemberCodeGenerator<CsStruct> NativeStruct { get; }
        IMemberCodeGenerator<CsField> ExplicitOffsetField { get; }
        IMemberCodeGenerator<CsField> AutoLayoutField { get; }
        IMemberCodeGenerator<CsStruct> Struct { get; }
        IStatementCodeGenerator<CsCallable> NativeInvocation { get; }
        IMemberCodeGenerator<CsCallable> Callable { get; }
        IMemberCodeGenerator<CsMethod> Method { get; }
        IMemberCodeGenerator<CsFunction> Function { get; }
        IMemberCodeGenerator<CsFunction> FunctionImport { get; }
        IMemberCodeGenerator<CsInterface> Interface { get; }
        IMemberCodeGenerator<CsInterface> Shadow { get; }
        IMemberCodeGenerator<CsInterface> Vtbl { get; }
        IMemberCodeGenerator<CsCallable> ShadowCallable { get; }
        IStatementCodeGenerator<CsCallable> ReverseCallableProlog { get; }
        IMemberCodeGenerator<CsGroup> Group { get; }

        MarshallingRegistry Marshalling { get; }
        GeneratorConfig Config { get; }
    }
}