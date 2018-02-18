using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen.CppModel;
using SharpGenTools.Sdk;

namespace SharpGen.Doc.Msdn.Tasks
{
    public class MsdnDocTask : Task
    {
        [Required]
        public ITaskItem DocumentationCache { get; set; }

        [Required]
        public ITaskItem CppModule { get; set; }
        

        public bool ShadowCopy { get; set; }

        public override bool Execute()
        {
            BindingRedirectResolution.Enable();
            var docProvider = new MsdnProvider(message => Log.LogMessage(message));

            var module = CppModel.CppModule.Read(CppModule.ItemSpec);

            var cache = new DocItemCache();

            var cachePath = ShadowCopy ? Path.GetTempFileName() : DocumentationCache.ItemSpec;

            if (File.Exists(DocumentationCache.ItemSpec))
            {
                if (ShadowCopy)
                {
                    File.Copy(DocumentationCache.ItemSpec, cachePath, true);
                }
                cache = DocItemCache.Read(cachePath);
            }
            
            var documented = docProvider.ApplyDocumentation(cache, module).Result;
            
            cache.Write(cachePath);

            if (ShadowCopy)
            {
                File.Copy(cachePath, DocumentationCache.ItemSpec, true);
                File.Delete(cachePath);
            }

            documented.Write(CppModule.ItemSpec);

            return true;
        }
    }
}
