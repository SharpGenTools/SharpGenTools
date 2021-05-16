using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;

namespace SharpGen.Generator.Marshallers
{
    internal abstract class WrapperMarshallerBase : MarshallerBase, IMarshaller
    {
        private readonly IMarshaller implementation;

        protected WrapperMarshallerBase(Ioc ioc, IMarshaller implementation) : base(ioc)
        {
            this.implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
        }

        public virtual IEnumerable<StatementSyntax> GenerateManagedToNativeProlog(CsMarshalCallableBase csElement) =>
            implementation.GenerateManagedToNativeProlog(csElement);

        public virtual IEnumerable<StatementSyntax> GenerateNativeToManagedExtendedProlog(CsMarshalCallableBase csElement) =>
            implementation.GenerateNativeToManagedExtendedProlog(csElement);

        public virtual StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame) =>
            implementation.GenerateManagedToNative(csElement, singleStackFrame);

        public virtual StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame) =>
            implementation.GenerateNativeToManaged(csElement, singleStackFrame);

        public virtual ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement) =>
            implementation.GenerateNativeArgument(csElement);

        public virtual ArgumentSyntax GenerateManagedArgument(CsParameter csElement) =>
            implementation.GenerateManagedArgument(csElement);

        public virtual ParameterSyntax GenerateManagedParameter(CsParameter csElement) =>
            implementation.GenerateManagedParameter(csElement);

        public virtual StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame) =>
            implementation.GenerateNativeCleanup(csElement, singleStackFrame);

        public virtual FixedStatementSyntax GeneratePin(CsParameter csElement) => implementation.GeneratePin(csElement);

        public virtual bool CanMarshal(CsMarshalBase csElement) => implementation.CanMarshal(csElement);

        public virtual bool GeneratesMarshalVariable(CsMarshalCallableBase csElement) =>
            implementation.GeneratesMarshalVariable(csElement);

        public virtual TypeSyntax GetMarshalTypeSyntax(CsMarshalBase csElement) =>
            implementation.GetMarshalTypeSyntax(csElement);
    }
}