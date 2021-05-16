using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator
{
    public sealed class DefaultGenerators : IGeneratorRegistry
    {
        public DefaultGenerators(GeneratorConfig config, Ioc ioc)
        {
            Constant = new ConstantCodeGenerator(ioc);
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
            Interface = new InterfaceCodeGenerator(ioc);
            Group = new GroupCodeGenerator(ioc);
            ShadowCallable = new ShadowCallbackGenerator(ioc);
            ReverseCallableProlog = new ReverseCallablePrologCodeGenerator(ioc);
            Vtbl = new VtblGenerator(ioc);
            Shadow = new ShadowGenerator(ioc);
            Marshalling = new MarshallingRegistry(ioc);
            Config = config;
        }

        public IMultiCodeGenerator<CsVariable, MemberDeclarationSyntax> Constant { get; }
        public IMultiCodeGenerator<CsProperty, MemberDeclarationSyntax> Property { get; }
        public IMultiCodeGenerator<CsEnum, MemberDeclarationSyntax> Enum { get; }
        public IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax> NativeStruct { get; }
        public IMultiCodeGenerator<CsField, MemberDeclarationSyntax> ExplicitOffsetField { get; }
        public IMultiCodeGenerator<CsField, MemberDeclarationSyntax> AutoLayoutField { get; }
        public IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax> Struct { get; }
        public INativeCallCodeGenerator NativeInvocation { get; }
        public IMultiCodeGenerator<CsCallable, MemberDeclarationSyntax> Callable { get; }
        public IMultiCodeGenerator<CsMethod, MemberDeclarationSyntax> Method { get; }
        public IMultiCodeGenerator<CsFunction, MemberDeclarationSyntax> Function { get; }
        public IMultiCodeGenerator<CsInterface, MemberDeclarationSyntax> Interface { get; }
        public IMultiCodeGenerator<CsGroup, MemberDeclarationSyntax> Group { get; }
        public ICodeGenerator<CsInterface, MemberDeclarationSyntax> Shadow { get; }
        public ICodeGenerator<CsInterface, MemberDeclarationSyntax> Vtbl { get; }
        public IMultiCodeGenerator<CsCallable, MemberDeclarationSyntax> ShadowCallable { get; }
        public IMultiCodeGenerator<(CsCallable, InteropMethodSignature), StatementSyntax> ReverseCallableProlog { get; }

        public MarshallingRegistry Marshalling { get; }

        public GeneratorConfig Config { get; }
    }
}
