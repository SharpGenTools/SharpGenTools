using System;
using Microsoft.Build.Framework;
using SharpGenTools.Sdk.Patch;

namespace SharpGenTools.Sdk.Tasks
{
    public sealed class SharpPatchTask : SharpTaskBase
    {
        // ReSharper disable MemberCanBePrivate.Global, UnusedAutoPropertyAccessor.Global
        [Required] public string AssemblyToPatch { get; set; }

        [Required] public ITaskItem[] References { get; set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Global, MemberCanBePrivate.Global

        public override bool Execute()
        {
            PrepareExecute();

            try
            {
                AssemblyPatcher patchApp = new(new MSBuildAssemblyResolver(References), SharpGenLogger);
                return patchApp.PatchFile(AssemblyToPatch);
            }
            catch (Exception ex)
            {
                Log.LogErrorFromException(ex, true, true, null);
                return false;
            }
        }
    }
}