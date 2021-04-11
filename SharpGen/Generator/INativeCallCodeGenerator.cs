using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator
{
    public interface INativeCallCodeGenerator
    {
        ExpressionSyntax GenerateCall(CsCallable callable, PlatformAbi platform,
                                      InteropMethodSignature interopSig);
    }
}