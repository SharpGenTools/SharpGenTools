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
            NativeStruct = new NativeStructCodeGenerator(globalNamespace);
            NativeInvocation = new NativeInvocationCodeGenerator(this, globalNamespace);
            ParameterProlog = new ParameterPrologCodeGenerator(globalNamespace);
            ParameterEpilog = new ParameterEpilogCodeGenerator();
            Callable = new CallableCodeGenerator(this, documentation, docReader, globalNamespace);
            Method = new MethodCodeGenerator(this);
            Function = new FunctionCodeGenerator(this);
            Interface = new InterfaceCodeGenerator(this, documentation, docReader);
            Parameter = new ParameterCodeGenerator();
            Argument = new ArgumentGenerator(globalNamespace);
            Group = new GroupCodeGenerator(this, documentation);
            LocalInterop = new LocalInteropCodeGenerator(this);
            InteropMethod = new InteropMethodCodeGenerator();
        }

        public IMultiCodeGenerator<CsVariable, MemberDeclarationSyntax> Constant { get; }
        public IMultiCodeGenerator<CsProperty, MemberDeclarationSyntax> Property { get; }
        public IMultiCodeGenerator<CsEnum, MemberDeclarationSyntax> Enum { get; }
        public IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax> NativeStruct { get; }
        public IMultiCodeGenerator<CsField, MemberDeclarationSyntax> ExplicitOffsetField { get; }
        public IMultiCodeGenerator<CsField, MemberDeclarationSyntax> AutoLayoutField { get; }
        public IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax> Struct { get; }
        public ICodeGenerator<CsCallable, ExpressionSyntax> NativeInvocation { get; }
        public IMultiCodeGenerator<CsParameter, StatementSyntax> ParameterProlog { get; }
        public IMultiCodeGenerator<CsParameter, StatementSyntax> ParameterEpilog { get; }
        public IMultiCodeGenerator<CsCallable, MemberDeclarationSyntax> Callable { get; }
        public IMultiCodeGenerator<CsMethod, MemberDeclarationSyntax> Method { get; }
        public IMultiCodeGenerator<CsFunction, MemberDeclarationSyntax> Function { get; }
        public IMultiCodeGenerator<CsInterface, MemberDeclarationSyntax> Interface { get; }
        public ICodeGenerator<CsParameter, ParameterSyntax> Parameter { get; }
        public ICodeGenerator<CsParameter, ArgumentSyntax> Argument { get; }
        public IMultiCodeGenerator<CsGroup, MemberDeclarationSyntax> Group { get; }

        public ICodeGenerator<CsAssembly, NamespaceDeclarationSyntax> LocalInterop { get; }

        public IMultiCodeGenerator<InteropMethodSignature, MemberDeclarationSyntax> InteropMethod { get; }
    }
}
