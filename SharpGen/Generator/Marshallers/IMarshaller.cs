﻿using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpGen.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGen.Generator.Marshallers
{
    public interface IMarshaller
    {

        StatementSyntax GenerateManagedToNative(CsMarshalBase csElement, bool singleStackFrame);

        StatementSyntax GenerateNativeToManaged(CsMarshalBase csElement, bool singleStackFrame);

        ArgumentSyntax GenerateNativeArgument(CsMarshalCallableBase csElement);

        ArgumentSyntax GenerateManagedArgument(CsParameter csElement);

        ParameterSyntax GenerateManagedParameter(CsParameter csElement);

        StatementSyntax GenerateNativeCleanup(CsMarshalBase csElement, bool singleStackFrame);

        FixedStatementSyntax GeneratePin(CsParameter csElement);

        bool CanMarshal(CsMarshalBase csElement);
    }
}