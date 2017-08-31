using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
#if !NET46
using System.Runtime.Loader;
#endif

namespace SharpGenTools.Sdk
{
    static class BindingRedirectResolution
    {
        public static void Enable()
        {
        }

        static BindingRedirectResolution()
        {
#if NET46
            AppDomain.CurrentDomain.AssemblyResolve += (s, args) =>
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var referenceName = new AssemblyName(AppDomain.CurrentDomain.ApplyPolicy(args.Name));
                var fileName = referenceName.Name + ".dll";

                if (!String.IsNullOrEmpty(assemblyPath))
                {
                    var probingPath = Path.Combine(Path.GetDirectoryName(assemblyPath), fileName);
                    if (File.Exists(probingPath))
                    {
                        var name = AssemblyName.GetAssemblyName(probingPath);

                        if (name.Version >= referenceName.Version)
                        {
                            return Assembly.Load(name);
                        }
                    }
                }

                return null;
            };
#else
            // Isn't getting called???
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                var assemblyPath = typeof(BindingRedirectResolution).GetTypeInfo().Assembly.Location;
                var fileName = name.Name + ".dll";
                if (!String.IsNullOrEmpty(assemblyPath))
                {
                    var probingPath = Path.Combine(Path.GetDirectoryName(assemblyPath), fileName);
                    if (File.Exists(probingPath))
                    {
                        return context.LoadFromAssemblyPath(probingPath);
                    }
                }
                return null;
            };
#endif
        }
    }
}
