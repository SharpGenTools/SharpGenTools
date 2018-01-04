using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Framework;
using SharpGen.Config;
using SharpGen.CppModel;
using SharpGen.Parser;

namespace SharpGenTools.Sdk.Tasks
{
    class ParseCPlusPlus : SharpGenCppTaskBase
    {
        [Required]
        public string CastXmlExecutablePath { get; set; }

        [Required]
        public string OutputPath { get; set; }

        [Required]
        public ITaskItem PartialCppModuleCache { get; set; }

        [Required]
        public ITaskItem ParsedCppModule { get; set; }

        protected override bool Execute(ConfigFile config)
        {
            var castXml = new CastXml(SharpGenLogger, CastXmlExecutablePath)
            {
                OutputPath = OutputPath
            };

            castXml.Configure(config);

            // Run the parser
            var parser = new CppParser(SharpGenLogger, castXml)
            {
                OutputPath = OutputPath
            };
            parser.Initialize(config);

            if (SharpGenLogger.HasErrors)
                return false;

            var module = CppModule.Read(PartialCppModuleCache.ItemSpec);

            // Run the parser
            var group = parser.Run(module);

            if (SharpGenLogger.HasErrors)
                return false;

            group.Write(ParsedCppModule.ItemSpec);

            return true;
        }
    }
}
