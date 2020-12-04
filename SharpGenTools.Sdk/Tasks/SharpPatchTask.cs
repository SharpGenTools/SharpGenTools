using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpPatch;

namespace SharpGenTools.Sdk.Tasks
{
    public sealed class SharpPatchTask : Task
    {
        [Required] public string AssemblyToPatch { get; set; }

        [Required] public ITaskItem[] References { get; set; }

        public override bool Execute()
        {
            BindingRedirectResolution.Enable();
            try
            {
                var patchApp = new InteropApp
                {
                    AssemblyResolver = new MSBuildAssemblyResolver(References),
                    Logger = new MSBuildSharpPatchLogger(Log),
                };
                patchApp.PatchFile(AssemblyToPatch);
                return true;
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, true, true, null);
                return false;
            }
        }
    }
}