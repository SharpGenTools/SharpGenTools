using SharpGen.Model;

namespace SharpGen.Generator;

public sealed class GeneratorConfig
{
    public PlatformDetectionType Platforms { get; set; } = PlatformDetectionType.Any;
}