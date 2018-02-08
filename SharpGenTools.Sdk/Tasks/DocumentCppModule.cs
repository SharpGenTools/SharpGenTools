using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen.CppModel;
using SharpGen.Doc;
using System;
using System.Collections.Generic;
using System.Reflection;
#if NETSTANDARD1_5
using System.Runtime.Loader;
#endif
using System.Text;
using Logger = SharpGen.Logging.Logger;
using System.IO;
using SharpGen;

namespace SharpGenTools.Sdk.Tasks
{
    public class DocumentCppModule : Task
    {
        public string DocProviderAssemblyPath { get; set; }

        [Required]
        public ITaskItem DocumentationCache { get; set; }

        [Required]
        public ITaskItem ParsedCppModule { get; set; }

        [Required]
        public ITaskItem DocumentedCppModule { get; set; }


        public override bool Execute()
        {
            BindingRedirectResolution.Enable();
            var logger = new Logger(new MsBuildSharpGenLogger(Log), null);

            // Use default MSDN doc provider
            IDocProvider docProvider = new MsdnProvider(logger);

            // Try to load doc provider from an external assembly
            if (!string.IsNullOrEmpty(DocProviderAssemblyPath))
            {
                try
                {
#if NETSTANDARD1_5
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(DocProviderAssemblyPath);
#else
                    var assembly = Assembly.LoadFrom(DocProviderAssemblyPath);
#endif

                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(IDocProvider).GetTypeInfo().IsAssignableFrom(type))
                        {
                            docProvider = (IDocProvider)Activator.CreateInstance(type);
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    Log.LogWarning("Warning, Unable to locate/load DocProvider Assembly.");
                    Log.LogWarning("Warning, DocProvider was not found from assembly [{0}]", DocProviderAssemblyPath);
                    Log.LogWarning("Falling back to MSDN documentation provider");
                }
            }

            var module = CppModule.Read(ParsedCppModule.ItemSpec);

            var cache = new DocItemCache();

            if (File.Exists(DocumentationCache.ItemSpec))
            {
                cache = DocItemCache.Read(DocumentationCache.ItemSpec);
            }

            docProvider.ApplyDocumentation(cache, module).Result.Write(DocumentedCppModule.ItemSpec);

            cache.Write(DocumentationCache.ItemSpec);

            return true;
        }
    }
}
