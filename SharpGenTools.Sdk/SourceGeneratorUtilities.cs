using System;
using System.Diagnostics;

// TODO: Check the existence of ModuleInitializerAttribute via semantic model in source generator
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    [Conditional("SHARPGEN_ATTRIBUTE_DEBUG")]
    internal sealed class ModuleInitializerAttribute : Attribute
    {
        public ModuleInitializerAttribute()
        {
        }
    }
}
#endif
