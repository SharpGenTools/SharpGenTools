using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Logging;
using SharpGen.Model;
using SharpGen.Transform;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator
{
    class DefaultGenerators : IGeneratorRegistry
    {
        public DefaultGenerators(
            GlobalNamespaceProvider globalNamespace,
            IDocumentationLinker documentation,
            ExternalDocCommentsReader docReader,
            GeneratorConfig config,
            Logger logger)
        {
            Constant = new ConstantCodeGenerator();
            Property = new PropertyCodeGenerator(this, documentation, docReader);
            Enum = new EnumCodeGenerator(documentation, docReader);
            ExplicitOffsetField = new FieldCodeGenerator(documentation, docReader, true);
            AutoLayoutField = new FieldCodeGenerator(documentation, docReader, false);
            Struct = new StructCodeGenerator(this, documentation, docReader);
            NativeStruct = new NativeStructCodeGenerator(this, globalNamespace);
            NativeInvocation = new NativeInvocationCodeGenerator(this, globalNamespace);
            Callable = new CallableCodeGenerator(this, documentation, docReader, globalNamespace, logger);
            Method = new MethodCodeGenerator(this);
            Function = new FunctionCodeGenerator(this);
            Interface = new InterfaceCodeGenerator(this, documentation, docReader, globalNamespace);
            Group = new GroupCodeGenerator(this, documentation, docReader);
            LocalInterop = new LocalInteropCodeGenerator(this);
            InteropMethod = new InteropMethodCodeGenerator();
            ShadowCallable = new ShadowCallbackGenerator(this, globalNamespace);
            ReverseCallableProlog = new ReverseCallablePrologCodeGenerator(this, globalNamespace);
            Vtbl = new VtblGenerator(this, globalNamespace);
            Shadow = new ShadowGenerator(this, globalNamespace);
            Marshalling = new MarshallingRegistry(globalNamespace, logger);
            Config = config;
        }

        public IMultiCodeGenerator<CsVariable, MemberDeclarationSyntax> Constant { get; }
        public IMultiCodeGenerator<CsProperty, MemberDeclarationSyntax> Property { get; }
        public IMultiCodeGenerator<CsEnum, MemberDeclarationSyntax> Enum { get; }
        public IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax> NativeStruct { get; }
        public IMultiCodeGenerator<CsField, MemberDeclarationSyntax> ExplicitOffsetField { get; }
        public IMultiCodeGenerator<CsField, MemberDeclarationSyntax> AutoLayoutField { get; }
        public IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax> Struct { get; }
        public ICodeGenerator<(CsCallable, PlatformDetectionType, InteropMethodSignature), ExpressionSyntax> NativeInvocation { get; }
        public IMultiCodeGenerator<CsCallable, MemberDeclarationSyntax> Callable { get; }
        public IMultiCodeGenerator<CsMethod, MemberDeclarationSyntax> Method { get; }
        public IMultiCodeGenerator<CsFunction, MemberDeclarationSyntax> Function { get; }
        public IMultiCodeGenerator<CsInterface, MemberDeclarationSyntax> Interface { get; }
        public IMultiCodeGenerator<CsGroup, MemberDeclarationSyntax> Group { get; }

        public ICodeGenerator<CsAssembly, ClassDeclarationSyntax> LocalInterop { get; }

        public IMultiCodeGenerator<InteropMethodSignature, MemberDeclarationSyntax> InteropMethod { get; }

        public ICodeGenerator<CsInterface, MemberDeclarationSyntax> Shadow { get; }

        public ICodeGenerator<CsInterface, MemberDeclarationSyntax> Vtbl { get; }

        public IMultiCodeGenerator<CsCallable, MemberDeclarationSyntax> ShadowCallable { get; }

        public IMultiCodeGenerator<(CsCallable, InteropMethodSignature), StatementSyntax> ReverseCallableProlog { get; }

        public MarshallingRegistry Marshalling { get; }

        public GeneratorConfig Config { get; }
    }
}
