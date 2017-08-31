using Microsoft.Build.Framework;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SharpGenTools.Sdk
{
    class MSBuildAssemblyResolver : IAssemblyResolver
    {
        private Dictionary<string, Lazy<AssemblyDefinition>> references;

        public MSBuildAssemblyResolver(ITaskItem[] references)
        {
            this.references = references
                .Select(item => (Path.GetFileNameWithoutExtension(item.ItemSpec), new Lazy<AssemblyDefinition>(() => AssemblyDefinition.ReadAssembly(item.ItemSpec, new ReaderParameters { AssemblyResolver = this }))))
                .ToDictionary((element) => element.Item1, (element) => element.Item2);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            return references[name.Name].Value;
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            return references[name.Name].Value;
        }
    }
}
