// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using SharpGenTools.Sdk.Internal;
using SharpGenTools.Sdk.Internal.Roslyn;

namespace SharpGenTools.Sdk.Extensibility
{
    internal abstract class ExtensibilityAssemblyLoader
    {
        private readonly object _guard = new();

        private static readonly ImmutableHashSet<string> WellKnownAssemblyNames =
            new[]
            {
                "SharpGen",
                "SharpGen.Platform",
                "SharpGen.Runtime",
                "SharpGenTools.Sdk"
            }.ToImmutableHashSet();

        // lock _guard to read/write
        private readonly Dictionary<string, AssemblyIdentity> _loadedAssemblyIdentitiesByPath = new();
        private readonly Dictionary<AssemblyIdentity, Assembly> _loadedAssembliesByIdentity = new();

        private readonly HashSet<string> _dependencyLocations = new(PathUtilities.Comparer); 

        protected abstract Assembly LoadFromPathImpl(string fullPath);

        #region Public API

        public void AddDependencyLocation(string fullPath)
        {
            Utilities.RequireAbsolutePath(fullPath, nameof(fullPath));

            _dependencyLocations.Add(Directory.GetParent(fullPath).FullName);
        }

        public Assembly LoadFromPath(string fullPath)
        {
            Utilities.RequireAbsolutePath(fullPath, nameof(fullPath));
            return LoadFromPathUnchecked(fullPath);
        }

        #endregion

        private Assembly LoadFromPathUnchecked(string fullPath)
        {
            return LoadFromPathUncheckedCore(fullPath);
        }

        private Assembly LoadFromPathUncheckedCore(string fullPath, AssemblyIdentity identity = null)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            // Check if we have already loaded an assembly with the same identity or from the given path.
            Assembly loadedAssembly = null;
            lock (_guard)
            {
                identity ??= GetOrAddAssemblyIdentity(fullPath);
                if (identity != null && _loadedAssembliesByIdentity.TryGetValue(identity, out var existingAssembly))
                {
                    loadedAssembly = existingAssembly;
                }
            }

            // Otherwise, load the assembly.
            if (loadedAssembly == null)
            {
                loadedAssembly = LoadFromPathImpl(fullPath);
            }

            // Add the loaded assembly to both path and identity cache.
            return AddToCache(loadedAssembly, fullPath, identity);
        }

        private Assembly AddToCache(Assembly assembly, string fullPath, AssemblyIdentity identity)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            Debug.Assert(assembly != null);

            identity = AddToCache(fullPath, identity ?? AssemblyIdentity.FromAssemblyDefinition(assembly));
            Debug.Assert(identity != null);

            lock (_guard)
            {
                // The same assembly may be loaded from two different full paths (e.g. when loaded from GAC, etc.),
                // or another thread might have loaded the assembly after we checked above.
                if (_loadedAssembliesByIdentity.TryGetValue(identity, out var existingAssembly))
                {
                    assembly = existingAssembly;
                }
                else
                {
                    _loadedAssembliesByIdentity.Add(identity, assembly);
                }

                return assembly;
            }
        }

        private AssemblyIdentity GetOrAddAssemblyIdentity(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            lock (_guard)
            {
                if (_loadedAssemblyIdentitiesByPath.TryGetValue(fullPath, out var existingIdentity))
                {
                    return existingIdentity;
                }
            }

            var identity = AssemblyIdentityUtils.TryGetAssemblyIdentity(fullPath);
            return AddToCache(fullPath, identity);
        }

        private AssemblyIdentity AddToCache(string fullPath, AssemblyIdentity identity)
        {
            lock (_guard)
            {
                if (_loadedAssemblyIdentitiesByPath.TryGetValue(fullPath, out var existingIdentity) && existingIdentity != null)
                {
                    identity = existingIdentity;
                }
                else
                {
                    _loadedAssemblyIdentitiesByPath[fullPath] = identity;
                }
            }

            return identity;
        }

        public Assembly Load(string displayName)
        {
            if (!AssemblyIdentity.TryParseDisplayName(displayName, out var requestedIdentity))
            {
                return null;
            }

            if (WellKnownAssemblyNames.Contains(requestedIdentity.Name))
            {
                // Force our assemblies to be loaded in the default ALC
                // and unify to the current version.
                return null;
            }

            lock (_guard)
            {
                // First, check if this loader already loaded the requested assembly:
                if (_loadedAssembliesByIdentity.TryGetValue(requestedIdentity, out var existingAssembly))
                {
                    return existingAssembly;
                }
            }

            IEnumerable<string> CulturePathSelector(string cultureSubfolder)
            {
                string LocationSelector(string directory) =>
                    Path.Combine(directory, cultureSubfolder, $"{requestedIdentity.Name}.dll");

                return _dependencyLocations.Select(LocationSelector).Where(File.Exists);
            }

            var culturePathVariants = string.IsNullOrEmpty(requestedIdentity.CultureName)
                                          // If no culture is specified, attempt to load directly from
                                          // the known dependency paths.
                                          ? new[] {string.Empty}
                                          // Search for satellite assemblies in culture subdirectories
                                          // of the assembly search directories, but fall back to the
                                          // bare search directory if that fails.
                                          : new[] {requestedIdentity.CultureName, string.Empty};

            var candidatePaths = culturePathVariants.SelectMany(CulturePathSelector);

            // Multiple assemblies of the same simple name but different identities might have been registered.
            // Load the one that matches the requested identity (if any).
            foreach (var candidatePath in candidatePaths)
            {
                var candidateIdentity = GetOrAddAssemblyIdentity(candidatePath);

                if (requestedIdentity.Equals(candidateIdentity))
                {
                    return LoadFromPathUncheckedCore(candidatePath, candidateIdentity);
                }
            }

            return null;
        }
    }
}
