using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator.Marshallers;

internal abstract class ValueTypeMarshallerBase : MarshallerBase, IMarshaller
{
    protected ValueTypeMarshallerBase(Ioc ioc) : base(ioc)
    {
    }

    public abstract IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement);

    public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
        Enumerable.Empty<StatementSyntax>();

    public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) => null;

    public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) => null;

    public abstract ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement);

    public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) =>
        GenerateManagedValueTypeArgument(csElement);

    public ParameterSyntax GenerateManagedParameter(CsParameter csElement) =>
        GenerateManagedValueTypeParameter(csElement);

    public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) => null;

    public abstract FixedStatementSyntax GeneratePin(CsParameter csElement);

    public bool CanMarshal(CsMarshalBase csElement) =>
        csElement is CsMarshalCallableBase {IsArray: false} value && CanMarshal(value);

    protected abstract bool CanMarshal(CsMarshalCallableBase csElement);

    public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => false;

    public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) =>
        SyntaxFactory.ParseTypeName(GetMarshalType(csElement).QualifiedName);

    protected abstract CsTypeBase GetMarshalType(CsMarshalBase csElement);
}