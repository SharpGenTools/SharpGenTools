using SharpGen.Model;

namespace SharpGen.Generator
{
    public class GeneratorConfig
    {
        public PlatformAbi Platforms { get; set; } = PlatformAbi.Any;
    }
}