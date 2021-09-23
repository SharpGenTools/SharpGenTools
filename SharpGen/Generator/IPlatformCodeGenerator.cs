using System.Collections.Generic;
using SharpGen.Model;

namespace SharpGen.Generator
{
    public interface IPlatformCodeGenerator<in TCsElement> where TCsElement : CsBase
    {
        IEnumerable<PlatformDetectionType> GetPlatforms(TCsElement csElement);
    }
}