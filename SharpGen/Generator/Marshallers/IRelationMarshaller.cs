using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator.Marshallers;

public interface IRelationMarshaller
{
    StatementSyntax GenerateManagedToNative(CsMarshalBase publicElement, CsMarshalBase relatedElement);
}