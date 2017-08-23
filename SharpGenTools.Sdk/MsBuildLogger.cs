using System;
using Microsoft.Build.Utilities;
using SharpGen.Logging;

namespace SharpGenTools.Sdk
{
    internal class MsBuildLogger : LoggerBase
    {
        private TaskLoggingHelper log;

        public MsBuildLogger(TaskLoggingHelper log)
        {
            this.log = log;
        }

        public override void Exit(string reason, int exitCode)
        {
            log.LogError(reason ?? "");
            throw new CodeGenFailedException(reason ?? "");
        }

        public override void Log(LogLevel logLevel, LogLocation logLocation, string context, string message, Exception exception, params object[] parameters)
        {
            if (message == null)
            {
                return;
            }

            switch (logLevel)
            {
                case LogLevel.Info:
                    log.LogMessage(context, null, null, logLocation.File, logLocation.Line, logLocation.Column, 0, 0, Microsoft.Build.Framework.MessageImportance.Normal, message, parameters);
                    break;
                case LogLevel.Warning:
                    log.LogWarning(context, null, null, logLocation.File, logLocation.Line, logLocation.Column, 0, 0, message, parameters);
                    if (exception != null)
                    {
                        log.LogWarningFromException(exception);
                    }
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    log.LogError(context, null, null, logLocation.File, logLocation.Line, logLocation.Column, 0, 0, message, parameters);
                    if (exception != null)
                    {
                        log.LogErrorFromException(exception);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}