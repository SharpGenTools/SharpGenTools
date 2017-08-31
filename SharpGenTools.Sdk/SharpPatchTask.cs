using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpPatch;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGenTools.Sdk
{
    public class SharpPatchTask : Task
    {
        [Required]
        public string AssemblyToPatch { get; set; }

        [Required]
        public ITaskItem[] References { get; set; }

        public override bool Execute()
        {
            BindingRedirectResolution.Enable();
            try
            {
                var patchApp = new InteropApp();
                patchApp.AssemblyResolver = new MSBuildAssemblyResolver(References);
                patchApp.PatchFile(AssemblyToPatch);
                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex);
                return false;
            }
        }
    }
}
