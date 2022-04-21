// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using SharpGen.Doc;

namespace SharpGenTools.Sdk.Extensibility;

/// <summary>
/// Represents an extension reference that can't be resolved.
/// </summary>
/// <remarks>
/// For error reporting only, can't be used to reference an extension assembly.
/// </remarks>
public sealed class UnresolvedExtensionReference : ExtensionReference
{
    public UnresolvedExtensionReference(string unresolvedPath)
    {
        FullPath = unresolvedPath ?? throw new ArgumentNullException(nameof(unresolvedPath));
    }

    public override string Display => "Unresolved: " + FullPath;

    public override string FullPath { get; }

    public override object Id => FullPath;

    public override ImmutableArray<IDocProvider> GetDocumentationProviders() => ImmutableArray<IDocProvider>.Empty;
}