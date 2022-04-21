using System;

namespace SharpGen.VisualStudioSetup;

[Flags]
public enum InstanceState : uint
{
    None = 0,
    Local = 1,
    Registered = 2,
    NoRebootRequired = 4,
    NoErrors = 8,
    Complete = unchecked((uint)-1),
}