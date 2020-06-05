using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SharpGen.Logging;

namespace SharpGenTools.Sdk
{
    internal sealed class MSBuildSharpGenLogger : LoggerBase
    {
        private readonly TaskLoggingHelper log;

        public MSBuildSharpGenLogger(TaskLoggingHelper log)
        {
            this.log = log;
        }

        public override void Exit(string reason, int exitCode)
        {
            log.LogError(reason ?? "");
            throw new CodeGenFailedException(reason ?? "");
        }

        public override void Log(LogLevel logLevel, LogLocation logLocation, string context, string code, string message, Exception exception, params object[] parameters)
        {
            if (message == null)
            {
                return;
            }

            switch (logLevel)
            {
                case LogLevel.Info:
                    log.LogMessage(context, code, null, logLocation?.File, logLocation?.Line ?? 0, logLocation?.Column ?? 0, 0, 0, MessageImportance.Normal, message, parameters);
                    break;
                case LogLevel.Warning:
                    log.LogWarning(context, code, null, logLocation?.File, logLocation?.Line ?? 0, logLocation?.Column ?? 0, 0, 0, message, parameters);
                    if (exception != null)
                    {
                        log.LogWarningFromException(exception);
                    }
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    log.LogError(context, code, null, logLocation?.File, logLocation?.Line ?? 0, logLocation?.Column ?? 0, 0, 0, message, parameters);
                    if (exception != null)
                    {
                        log.LogErrorFromException(exception, true, true, null);
                    }
                    break;
                default:
                    break;
            }
        }
    }
}