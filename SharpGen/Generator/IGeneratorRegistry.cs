using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator
{
    public interface IGeneratorRegistry
    {
        IMultiCodeGenerator<CsVariable, MemberDeclarationSyntax> Constant { get; }
        IMultiCodeGenerator<CsProperty, MemberDeclarationSyntax> Property { get; }
        IMultiCodeGenerator<CsEnum, MemberDeclarationSyntax> Enum { get; }
        IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax> NativeStruct { get; }
        IMultiCodeGenerator<CsField, MemberDeclarationSyntax> ExplicitOffsetField { get; }
        IMultiCodeGenerator<CsField, MemberDeclarationSyntax> AutoLayoutField { get; }
        IMultiCodeGenerator<CsStruct, MemberDeclarationSyntax> Struct { get; }
        ICodeGenerator<(CsCallable, PlatformDetectionType, InteropMethodSignature), ExpressionSyntax> NativeInvocation { get; }
        IMultiCodeGenerator<CsCallable, MemberDeclarationSyntax> Callable { get; }
        IMultiCodeGenerator<CsMethod, MemberDeclarationSyntax> Method { get; }
        IMultiCodeGenerator<CsFunction, MemberDeclarationSyntax> Function { get; }
        IMultiCodeGenerator<CsInterface, MemberDeclarationSyntax> Interface { get; }
        ICodeGenerator<CsInterface, MemberDeclarationSyntax> Shadow { get; }
        ICodeGenerator<CsInterface, MemberDeclarationSyntax> Vtbl { get; }
        IMultiCodeGenerator<CsCallable, MemberDeclarationSyntax> ShadowCallable { get; }
        IMultiCodeGenerator<(CsCallable, InteropMethodSignature), StatementSyntax> ReverseCallableProlog { get; }
        IMultiCodeGenerator<CsGroup, MemberDeclarationSyntax> Group { get; }
        ICodeGenerator<CsAssembly, ClassDeclarationSyntax> LocalInterop { get; }

        IMultiCodeGenerator<InteropMethodSignature, MemberDeclarationSyntax> InteropMethod { get; }
        MarshallingRegistry Marshalling { get; }
        GeneratorConfig Config { get; }
    }
}
