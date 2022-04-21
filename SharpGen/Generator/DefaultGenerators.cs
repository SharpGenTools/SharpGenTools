using SharpGen.Model;

namespace SharpGen.Generator;

public sealed class DefaultGenerators : IGeneratorRegistry
{
    public DefaultGenerators(Ioc ioc)
    {
        ExpressionConstant = new ExpressionConstantCodeGenerator(ioc);
        GuidConstant = new GuidConstantCodeGenerator(ioc);
        ResultConstant = new ResultConstantCodeGenerator(ioc);
        ResultRegistration = new ResultRegistrationCodeGenerator(ioc);
        Property = new PropertyCodeGenerator(ioc);
        Enum = new EnumCodeGenerator(ioc);
        ExplicitOffsetField = new FieldCodeGenerator(ioc, true);
        AutoLayoutField = new FieldCodeGenerator(ioc, false);
        Struct = new StructCodeGenerator(ioc);
        NativeStruct = new NativeStructCodeGenerator(ioc);
        NativeInvocation = new NativeInvocationCodeGenerator(ioc);
        Callable = new CallableCodeGenerator(ioc);
        Method = new MethodCodeGenerator(ioc);
        Function = new FunctionCodeGenerator(ioc);
        FunctionImport = new FunctionImportCodeGenerator(ioc);
        Interface = new InterfaceCodeGenerator(ioc);
        Group = new GroupCodeGenerator(ioc);
        ShadowCallable = new ShadowCallbackGenerator(ioc);
        ReverseCallableProlog = new ReverseCallablePrologCodeGenerator(ioc);
        Vtbl = new VtblGenerator(ioc);
        Shadow = new ShadowGenerator(ioc);
        Marshalling = new MarshallingRegistry(ioc);
        Config = ioc.GeneratorConfig;
    }

    public IMemberCodeGenerator<CsExpressionConstant> ExpressionConstant { get; }
    public IMemberCodeGenerator<CsGuidConstant> GuidConstant { get; }
    public IMemberCodeGenerator<CsResultConstant> ResultConstant { get; }
    public IStatementCodeGenerator<CsResultConstant> ResultRegistration { get; }
    public IMemberCodeGenerator<CsProperty> Property { get; }
    public IMemberCodeGenerator<CsEnum> Enum { get; }
    public IMemberCodeGenerator<CsStruct> NativeStruct { get; }
    public IMemberCodeGenerator<CsField> ExplicitOffsetField { get; }
    public IMemberCodeGenerator<CsField> AutoLayoutField { get; }
    public IMemberCodeGenerator<CsStruct> Struct { get; }
    public IStatementCodeGenerator<CsCallable> NativeInvocation { get; }
    public IMemberCodeGenerator<CsCallable> Callable { get; }
    public IMemberCodeGenerator<CsMethod> Method { get; }
    public IMemberCodeGenerator<CsFunction> Function { get; }
    public IMemberCodeGenerator<CsFunction> FunctionImport { get; }
    public IMemberCodeGenerator<CsInterface> Interface { get; }
    public IMemberCodeGenerator<CsInterface> Shadow { get; }
    public IMemberCodeGenerator<CsInterface> Vtbl { get; }
    public IMemberCodeGenerator<CsCallable> ShadowCallable { get; }
    public IStatementCodeGenerator<CsCallable> ReverseCallableProlog { get; }
    public IMemberCodeGenerator<CsGroup> Group { get; }

    public MarshallingRegistry Marshalling { get; }
    public GeneratorConfig Config { get; }
}