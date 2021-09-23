using Microsoft.Build.Framework;

namespace SharpGenTools.Sdk.Tasks
{
    public abstract class SharpGenTaskBase : SharpTaskBase
    {
        // ReSharper disable MemberCanBeProtected.Global, UnusedAutoPropertyAccessor.Global
        [Required] public string[] CastXmlArguments { get; set; }
        [Required] public ITaskItem CastXmlExecutable { get; set; }
        [Required] public ITaskItem[] ConfigFiles { get; set; }
        [Required] public string ConsumerBindMappingConfigId { get; set; }
        [Required] public ITaskItem DocumentationCache { get; set; }
        [Required] public bool DocumentationFailuresAsErrors { get; set; }
        [Required] public ITaskItem[] ExtensionAssemblies { get; set; }
        [Required] public ITaskItem[] ExternalDocumentation { get; set; }
        [Required] public string GeneratedCodeFolder { get; set; }
        [Required] public ITaskItem[] GlobalNamespaceOverrides { get; set; }
        [Required] public ITaskItem InputsCache { get; set; }
        [Required] public string[] Macros { get; set; }
        [Required] public string OutputPath { get; set; }
        [Required] public ITaskItem[] Platforms { get; set; }
        [Required] public ITaskItem[] SilenceMissingDocumentationErrorIdentifierPatterns { get; set; }
        [Required] public bool UseFunctionPointersInVtbl { get; set; }
        public ITaskItem ConsumerBindMappingConfig { get; set; }
        // ReSharper restore UnusedAutoPropertyAccessor.Global, MemberCanBeProtected.Global
    }
}