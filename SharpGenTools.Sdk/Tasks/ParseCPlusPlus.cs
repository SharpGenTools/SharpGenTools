using System;
using Microsoft.Build.Framework;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Parser;
using SharpGen.Platform;

namespace SharpGenTools.Sdk.Tasks
{
    public class ParseCPlusPlus : SharpGenCppTaskBase
    {
        [Required]
        public string CastXmlExecutablePath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public ITaskItem PartialCppModuleCache { get; set; }

        [Required]
        public ITaskItem ParsedCppModule { get; set; }

        [Required]
        public string[] CastXmlArguments { get; set; }

        protected override bool Execute(ConfigFile config)
        {
            var resolver = new IncludeDirectoryResolver(SharpGenLogger);
            resolver.Configure(config);

            var castXml = new CastXmlRunner(SharpGenLogger, resolver, CastXmlExecutablePath, CastXmlArguments)
            {
                OutputPath = OutputPath
            };

            // Run the parser
            var parser = new CppParser(SharpGenLogger, config)
            {
                OutputPath = OutputPath
            };

            if (SharpGenLogger.HasErrors)
                return false;

            var module = CppModule.Read(PartialCppModuleCache.ItemSpec);

            // Run the C++ parser
            CppModule group;

            using (var xmlReader = castXml.Process(parser.RootConfigHeaderFileName))
            {
                group = parser.Run(module, xmlReader);
            }

            if (SharpGenLogger.HasErrors)
                return false;

            group.Write(ParsedCppModule.ItemSpec);

            return true;
        }
    }
}
