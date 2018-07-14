using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;

namespace SharpGen.Generator.Marshallers
{
    class ValueTypeArrayFieldMarshaller : MarshallerBase, IMarshaller
    {
        public ValueTypeArrayFieldMarshaller(GlobalNamespaceProvider globalNamespace) : base(globalNamespace)
        {
        }

        public bool CanMarshal(CsMarshalBase csElement)
        {
            return csElement.IsValueType && csElement.IsArray && !csElement.MappedToDifferentPublicType && csElement is CsField;
        }

        public ArgumentSyntax GenerateManagedArgument(CsParameter csElement)
        {
            throw new InvalidOperationException();
        }

        public ParameterSyntax GenerateManagedParameter(CsParameter csElement)
        {
            throw new InvalidOperationException();
        }

        public StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame)
        {
            return GenerateCopyMemory(csElement, copyFromNative: false);
        }

        public IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement)
        {
            throw new InvalidOperationException();
        }

        public ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement)
        {
            throw new InvalidOperationException();
        }

        public StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame)
        {
            throw new NotImplementedException();
        }

        public StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame)
        {
            return GenerateCopyMemory(csElement, copyFromNative: true);
        }

        public IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement)
        {
            throw new InvalidOperationException();
        }

        public FixedStatementSyntax GeneratePin(CsParameter csElement)
        {
            throw new InvalidOperationException();
        }

        public bool GeneratesMarshalVariable(CsMarshalCallableBase csElement)
        {
            throw new InvalidOperationException();
        }

        public TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement)
        {
            throw new InvalidOperationException();
        }
    }
}
