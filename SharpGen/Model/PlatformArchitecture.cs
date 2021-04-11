using System;

namespace SharpGen.Model
{
    [Flags]
    public enum PlatformArchitecture
    {
        X86 = 1 << 0,
        Amd64 = 1 << 1,
        X64 = Amd64,
        Arm = 1 << 2,
        Arm64 = 1 << 3,
        Wasm = 1 << 4,
        AnyX86 = X86 | X64,
        AnyArm = Arm | Arm64,
        Any = AnyX86 | AnyArm | Wasm
    }
}