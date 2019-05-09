using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator.Marshallers
{
    public interface IRelationMarshaller
    {
        StatementSyntax GenerateManagedToNative(CsMarshalBase publicElement, CsMarshalBase relatedElement);
        StatementSyntax GenerateNativeToManaged(CsMarshalBase publicElement, CsMarshalBase relatedElement);
    }
}
