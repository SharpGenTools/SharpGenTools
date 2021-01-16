// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using SharpGen.Doc;

namespace SharpGenTools.Sdk.Extensibility
{
    /// <summary>
    /// Represents an extension assembly reference that contains documentation providers.
    /// </summary>
    /// <remarks>
    /// Represents a logical location of the extension reference, not the content of the reference.
    /// The content might change in time. A snapshot is taken when the SDK queries the reference for its extensibility points.
    /// </remarks>
    public abstract class ExtensionReference
    {
        protected ExtensionReference()
        {
        }

        /// <summary>
        /// Full path describing the location of the extension reference, or null if the reference has no location.
        /// </summary>
        public abstract string? FullPath { get; }

        /// <summary>
        /// Path or name used in error messages to identity the reference.
        /// </summary>
        /// <remarks>
        /// Should not be null.
        /// </remarks>
        public virtual string Display => string.Empty;

        /// <summary>
        /// A unique identifier for this extension reference.
        /// </summary>
        /// <remarks>
        /// Should not be null.
        /// Note that this and <see cref="FullPath"/> serve different purposes. An extension reference may not
        /// have a path, but it always has an ID. Further, two extension references with different paths may
        /// represent two copies of the same analyzer, in which case the IDs should also be the same.
        /// </remarks>
        public abstract object Id { get; }

        /// <summary>
        /// Gets all the documentation providers defined in this assembly reference.
        /// </summary>
        public abstract ImmutableArray<IDocProvider> GetDocumentationProviders();
    }
}
