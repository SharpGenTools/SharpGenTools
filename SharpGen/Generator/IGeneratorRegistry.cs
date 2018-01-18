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
        ICodeGenerator<CsCallable, ExpressionSyntax> NativeInvocation { get; }
        IMultiCodeGenerator<CsParameter, StatementSyntax> ParameterProlog { get; }
        IMultiCodeGenerator<CsParameter, StatementSyntax> ParameterEpilog { get; }
        IMultiCodeGenerator<CsCallable, MemberDeclarationSyntax> Callable { get; }
        IMultiCodeGenerator<CsMethod, MemberDeclarationSyntax> Method { get; }
        IMultiCodeGenerator<CsFunction, MemberDeclarationSyntax> Function { get; }
        IMultiCodeGenerator<CsInterface, MemberDeclarationSyntax> Interface { get; }
        ICodeGenerator<CsParameter, ParameterSyntax> Parameter { get; }
        ICodeGenerator<CsParameter, ArgumentSyntax> Argument { get; }
        IMultiCodeGenerator<CsGroup, MemberDeclarationSyntax> Group { get; }
        ICodeGenerator<CsAssembly, NamespaceDeclarationSyntax> LocalInterop { get; }

        IMultiCodeGenerator<InteropMethodSignature, MemberDeclarationSyntax> InteropMethod { get; }
    }
}
