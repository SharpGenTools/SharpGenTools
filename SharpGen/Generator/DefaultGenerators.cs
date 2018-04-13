using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            ExternalDocCommentsReader docReader)
        {
            Constant = new ConstantCodeGenerator();
            Property = new PropertyCodeGenerator(this, documentation, docReader);
            Enum = new EnumCodeGenerator(documentation, docReader);
            ExplicitOffsetField = new FieldCodeGenerator(documentation, docReader, true);
            AutoLayoutField = new FieldCodeGenerator(documentation, docReader, false);
            Struct = new StructCodeGenerator(this, documentation, docReader);
            NativeStruct = new NativeStructCodeGenerator(this, globalNamespace);
            NativeInvocation = new NativeInvocationCodeGenerator(this, globalNamespace);
            Callable = new CallableCodeGenerator(this, documentation, docReader, globalNamespace);
            Pinning = new PinningCodeGenerator(globalNamespace);
            Method = new MethodCodeGenerator(this);
            Function = new FunctionCodeGenerator(this);
            Interface = new InterfaceCodeGenerator(this, documentation, docReader);
            Parameter = new ParameterCodeGenerator();
            Argument = new ArgumentGenerator(globalNamespace);
            Group = new GroupCodeGenerator(this, documentation, docReader);
            LocalInterop = new LocalInteropCodeGenerator(this);
            InteropMethod = new InteropMethodCodeGenerator();
            CallableMarshallingProlog = new CallableMarshallingPrologCodeGenerator(globalNamespace);
            MarshalToNativeSingleFrame = new MarshalToNativeCodeGenerator(true, globalNamespace);
            MarshalToNative = new MarshalToNativeCodeGenerator(false, globalNamespace);
            MarshalFromNativeSingleFrame = new MarshalFromNativeCodeGenerator(true, globalNamespace);
            MarshalFromNative = new MarshalFromNativeCodeGenerator(false, globalNamespace);
            MarshalCleanupSingleFrame = new MarshalCleanupCodeGenerator(true, globalNamespace);
            MarshalCleanup = new MarshalCleanupCodeGenerator(false, globalNamespace);
        }

        public IMultiCodeGenerator<CsVariable, MemberDeclarationSyntax> Constant { get; }
        public IMultiCodeGenerator<CsProperty, MemberDeclarationSyntax> Property { get; }
        public IMultiCodeGenerator<CsEnum, MemberDeclarationSyntax> Enum { get; }
        public IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax> NativeStruct { get; }
        public IMultiCodeGenerator<CsField, MemberDeclarationSyntax> ExplicitOffsetField { get; }
        public IMultiCodeGenerator<CsField, MemberDeclarationSyntax> AutoLayoutField { get; }
        public IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax> Struct { get; }
        public ICodeGenerator<CsCallable, ExpressionSyntax> NativeInvocation { get; }
        public ICodeGenerator<CsParameter, FixedStatementSyntax> Pinning { get; }
        public IMultiCodeGenerator<CsCallable, MemberDeclarationSyntax> Callable { get; }
        public IMultiCodeGenerator<CsMethod, MemberDeclarationSyntax> Method { get; }
        public IMultiCodeGenerator<CsFunction, MemberDeclarationSyntax> Function { get; }
        public IMultiCodeGenerator<CsInterface, MemberDeclarationSyntax> Interface { get; }
        public ICodeGenerator<CsParameter, ParameterSyntax> Parameter { get; }
        public ICodeGenerator<CsMarshalCallableBase, ArgumentSyntax> Argument { get; }
        public IMultiCodeGenerator<CsGroup, MemberDeclarationSyntax> Group { get; }

        public ICodeGenerator<CsAssembly, NamespaceDeclarationSyntax> LocalInterop { get; }

        public IMultiCodeGenerator<InteropMethodSignature, MemberDeclarationSyntax> InteropMethod { get; }

        public IMultiCodeGenerator<CsMarshalCallableBase, StatementSyntax> CallableMarshallingProlog { get; }

        public ICodeGenerator<CsMarshalBase, StatementSyntax> MarshalToNativeSingleFrame { get; }

        public ICodeGenerator<CsMarshalBase, StatementSyntax> MarshalFromNativeSingleFrame { get; }

        public ICodeGenerator<CsMarshalBase, StatementSyntax> MarshalCleanupSingleFrame { get; }

        public ICodeGenerator<CsMarshalBase, StatementSyntax> MarshalToNative { get; }

        public ICodeGenerator<CsMarshalBase, StatementSyntax> MarshalFromNative { get; }

        public ICodeGenerator<CsMarshalBase, StatementSyntax> MarshalCleanup { get; }
    }
}
