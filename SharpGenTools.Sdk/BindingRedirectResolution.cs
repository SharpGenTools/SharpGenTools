using System;
using System.IO;
using System.Reflection;

namespace SharpGenTools.Sdk;

internal static class BindingRedirectResolution
{
    public static void Enable()
    {
    }

    static BindingRedirectResolution()
    {
#if NETFRAMEWORK
            AppDomain.CurrentDomain.AssemblyResolve += (s, args) =>
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var referenceName = new AssemblyName(AppDomain.CurrentDomain.ApplyPolicy(args.Name));
                var fileName = referenceName.Name + ".dll";

                if (!string.IsNullOrEmpty(assemblyPath))
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
#endif
    }
}