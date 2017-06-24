using Microsoft.Extensions.DependencyModel;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpPatch
{
    class AssemblyResolver : IAssemblyResolver // Code adapted from https://github.com/jbevain/cecil/issues/306
    {
        Dictionary<string, Lazy<AssemblyDefinition>> _libraries;
        public AssemblyResolver(DependencyContext context)
        {
            _libraries = new Dictionary<string, Lazy<AssemblyDefinition>>();

            var compileLibraries = context.CompileLibraries;
            foreach (var library in compileLibraries)
            {
                var path = library.Assemblies.FirstOrDefault();
                if (string.IsNullOrEmpty(path))
                    continue;
                if (path.StartsWith("lib") && path.StartsWith("ref"))
                    _libraries.Add(library.Name.ToLower(), ResolveNugetAssemblyPath(library.Path, path));
                else
                    _libraries.Add(library.Name.ToLower(), ResolveFrameworkAssemblyPath(path));
            }
        }
        /// <summary>
        /// Get 32-bit Program Files folder
        /// </summary>
        /// <returns></returns>
        static string ProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432")))
                || Environment.GetEnvironmentVariable("ProgramFiles(x86)") != null)
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        private Lazy<AssemblyDefinition> ResolveFrameworkAssemblyPath(string path)
        {
            var assemblyPath = Path.Combine(ProgramFilesx86(), @"Reference Assemblies\Microsoft\Framework", path);

            return new Lazy<AssemblyDefinition>(() =>
            AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters() { AssemblyResolver = this }));
        }

        private Lazy<string> homeDir = new Lazy<string>(GetHomeDir);

        private static string GetHomeDir()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.GetEnvironmentVariable("HOMEDRIVE") + Environment.GetEnvironmentVariable("HOMEPATH");
            }
            else // Assuming it's Unix-style until otherwise disproven
            {
                return Environment.GetEnvironmentVariable("HOME");
            }
        }

        private Lazy<AssemblyDefinition> ResolveNugetAssemblyPath(string package, string path)
        {
            var assemblyPath = Path.Combine(homeDir.Value, ".nuget", "packages", package, path);

            return new Lazy<AssemblyDefinition>(() =>
                AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters() { AssemblyResolver = this }));
        }

        public virtual AssemblyDefinition Resolve(string fullName)
        {
            return Resolve(fullName, new ReaderParameters());
        }

        public virtual AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
        {
            if (fullName == null)
                throw new ArgumentNullException(nameof(fullName));

            return Resolve(AssemblyNameReference.Parse(fullName), parameters);
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return Resolve(name, new ReaderParameters());
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (_libraries.TryGetValue(name.Name.ToLower(), out Lazy<AssemblyDefinition> asm))
                return asm.Value;

            throw new AssemblyResolutionException(name);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            foreach (var lazy in _libraries.Values)
            {
                if (!lazy.IsValueCreated)
                    continue;

                lazy.Value.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }
}
