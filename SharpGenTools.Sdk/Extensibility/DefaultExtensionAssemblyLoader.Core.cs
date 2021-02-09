// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace SharpGenTools.Sdk.Extensibility
{
    internal class DefaultExtensionAssemblyLoader : ExtensibilityAssemblyLoader
    {
        private readonly AssemblyLoadContext _loadContext;

        public DefaultExtensionAssemblyLoader()
        {
            _loadContext =
                AssemblyLoadContext.GetLoadContext(typeof(DefaultExtensionAssemblyLoader).GetTypeInfo().Assembly);

            _loadContext.Resolving += (context, name) =>
            {
                Debug.Assert(ReferenceEquals(context, _loadContext));
                return Load(name.FullName);
            };
        }

        protected override Assembly LoadFromPathImpl(string fullPath) => _loadContext.LoadFromAssemblyPath(fullPath);
    }
}

#endif
