using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System.Collections.Generic;

namespace SharpGen.Generator.Marshallers
{
    public interface IMarshaller
    {
        IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement);

        IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement);

        StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame);

        StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame);

        ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement);

        ArgumentSyntax GenerateManagedArgument(CsParameter csElement);

        ParameterSyntax GenerateManagedParameter(CsParameter csElement);

        StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame);

        FixedStatementSyntax GeneratePin(CsParameter csElement);

        bool CanMarshal(CsMarshalBase csElement);

        bool GeneratesMarshalVariable(CsMarshalCallableBase csElement);

        TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement);
    }
}
