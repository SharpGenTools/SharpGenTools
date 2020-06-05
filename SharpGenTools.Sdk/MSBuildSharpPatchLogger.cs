using Microsoft.Build.Utilities;
using SharpPatch;

namespace SharpGenTools.Sdk
{
    internal sealed class MSBuildSharpPatchLogger : ILogger
    {
        private readonly TaskLoggingHelper log;

        public MSBuildSharpPatchLogger(TaskLoggingHelper log)
        {
            this.log = log;
        }

        public void Log(string message, params object[] parameters)
        {
            log.LogMessage(message, parameters);
        }

        public void LogError(string message, params object[] parameters)
        {
            log.LogError(message, parameters);
        }
    }
}
