using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator.Marshallers;

internal abstract class FieldMarshallerBase : MarshallerBase, IMarshaller
{
    public bool CanMarshal(CsMarshalBase csElement) => csElement is CsField field && CanMarshal(field);

    protected abstract bool CanMarshal(CsField csField);

    public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => throw new NotSupportedException();

    public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) => throw new NotSupportedException();

    public FixedStatementSyntax GeneratePin(CsParameter csElement) => throw new NotSupportedException();

    public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement) =>
        throw new NotSupportedException();

    public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
        throw new NotSupportedException();

    public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
        GenerateManagedToNative((CsField) csElement, singleStackFrame);

    public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
        GenerateNativeToManaged((CsField) csElement, singleStackFrame);

    protected abstract StatementSyntax GenerateManagedToNative(CsField csField, bool singleStackFrame);
    protected abstract StatementSyntax GenerateNativeToManaged(CsField csField, bool singleStackFrame);

    public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) =>
        throw new NotSupportedException();

    public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) => throw new NotSupportedException();

    public ParameterSyntax GenerateManagedParameter(CsParameter csElement) => throw new NotSupportedException();

    public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) =>
        AllowNativeCleanup ? null : throw new NotSupportedException();

    protected abstract bool AllowNativeCleanup { get; }

    protected FieldMarshallerBase(Ioc ioc) : base(ioc)
    {
    }
}