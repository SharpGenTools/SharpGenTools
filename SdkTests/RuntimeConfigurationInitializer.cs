using System;
using System.IO;
using System.Runtime.CompilerServices;
using SharpGen.Runtime;

internal static class RuntimeConfigurationInitializer
{
    [ModuleInitializer]
    internal static void Configure()
    {
        Configuration.EnableObjectTracking = true;
    }
}