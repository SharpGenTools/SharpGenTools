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

        public override bool Execute()
        {
            var patchApp = new InteropApp();
            return patchApp.PatchFile(AssemblyToPatch);
        }
    }
}
