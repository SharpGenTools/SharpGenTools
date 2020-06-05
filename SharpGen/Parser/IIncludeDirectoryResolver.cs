using System.Collections.Generic;
using SharpGen.Config;

#nullable enable

namespace SharpGen.Parser
{
    public interface IIncludeDirectoryResolver
    {
        void Configure(ConfigFile config);
        void AddDirectories(IEnumerable<IncludeDirRule> directories);
        void AddDirectories(params IncludeDirRule[] directories);
        IEnumerable<string> IncludePaths { get; }
    }
}