using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator
{
    static class Generators
    {
        public static readonly IMultiCodeGenerator<CsVariable, MemberDeclarationSyntax> Constant = new ConstantCodeGenerator();

        public static readonly IMultiCodeGenerator<CsProperty, MemberDeclarationSyntax> Property = new PropertyCodeGenerator();

        public static readonly IMultiCodeGenerator<CsEnum, MemberDeclarationSyntax> Enum = new EnumCodeGenerator();

        public static readonly IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax> NativeStruct = new NativeStructCodeGenerator();

        public static readonly IMultiCodeGenerator<CsField, MemberDeclarationSyntax> ExplicitOffsetField = new FieldCodeGenerator(true);

        public static readonly IMultiCodeGenerator<CsField, MemberDeclarationSyntax> AutoLayoutField = new FieldCodeGenerator(false);

        public static readonly IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax> Struct = new StructCodeGenerator();

        public static readonly ICodeGenerator<CsMethod, ExpressionSyntax> NativeInvocation = new NativeInvocationCodeGenerator();

        public static readonly IMultiCodeGenerator<CsParameter, StatementSyntax> ParameterProlog = new ParameterPrologCodeGenerator();

        public static readonly IMultiCodeGenerator<CsParameter, StatementSyntax> ParameterEpilog = new ParameterEpilogCodeGenerator();

        public static readonly IMultiCodeGenerator<CsMethod, MemberDeclarationSyntax> Callable = new CallableCodeGenerator();

        public static readonly IMultiCodeGenerator<CsMethod, MemberDeclarationSyntax> Method = new MethodCodeGenerator();

        public static readonly IMultiCodeGenerator<CsFunction, MemberDeclarationSyntax> Function = new FunctionCodeGenerator();

        public static readonly IMultiCodeGenerator<CsInterface, MemberDeclarationSyntax> Interface = new InterfaceCodeGenerator();
    }
}
