#nullable enable

using System.Collections.Generic;
using SharpGen.Model;

namespace SharpGen.Transform;

public interface IInteropSignatureTransform
{
    IDictionary<PlatformDetectionType, InteropMethodSignature> GetInteropSignatures(CsCallable callable);
}