using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator.Marshallers
{
    internal class ValueTypeArrayFieldMarshaller : MarshallerBase, IMarshaller
    {
        public ValueTypeArrayFieldMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement) => csElement.IsValueType && csElement.IsArray &&
                                                           !csElement.MappedToDifferentPublicType &&
                                                           csElement is CsField;

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
            GenerateCopyMemory(csElement, false);

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
            GenerateCopyMemory(csElement, true);

        #region Non-supported operations

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) => throw new NotSupportedException();

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) => throw new NotSupportedException();

        public FixedStatementSyntax GeneratePin(CsParameter csElement) => throw new NotSupportedException();

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement) =>
            throw new NotSupportedException();

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
            throw new NotSupportedException();

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) =>
            throw new NotSupportedException();

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement) => throw new NotSupportedException();

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement) => throw new NotSupportedException();

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) =>
            throw new NotSupportedException();

        #endregion
    }
}
