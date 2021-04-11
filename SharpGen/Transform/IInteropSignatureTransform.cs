#nullable enable

using System.Collections.Generic;
using SharpGen.Model;

namespace SharpGen.Transform
{
    public interface IInteropSignatureTransform
    {
        IDictionary<PlatformAbi, InteropMethodSignature> GetInteropSignatures(CsCallable callable);
    }
}