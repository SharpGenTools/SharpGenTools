using System;
using SharpGen.Logging;

namespace SharpGenTools.Sdk.Documentation
{
    internal sealed class DocumentationLogger : LoggerBase
    {
        private readonly LoggerBase loggerBaseImplementation;

        public DocumentationLogger(LoggerBase loggerBaseImplementation)
        {
            this.loggerBaseImplementation = loggerBaseImplementation;
        }

        public LogLevel MaxLevel { get; set; } = LogLevel.Fatal;

        public override ILogger LoggerOutput => loggerBaseImplementation.LoggerOutput;

        public override bool HasErrors => loggerBaseImplementation.HasErrors;

        public override IProgressReport ProgressReport => loggerBaseImplementation.ProgressReport;

        public override void PushContext(string context) => loggerBaseImplementation.PushContext(context);

        public override void PushLocation(string fileName, int line = 1, int column = 1) =>
            loggerBaseImplementation.PushLocation(fileName, line, column);

        public override void PopLocation() => loggerBaseImplementation.PopLocation();

        public override void PushContext(string context, params object[] parameters) =>
            loggerBaseImplementation.PushContext(context, parameters);

        public override void PopContext() => loggerBaseImplementation.PopContext();

        public override void Progress(int level, string message, params object[] parameters) =>
            loggerBaseImplementation.Progress(level, message, parameters);

        public override void Exit(string reason, params object[] parameters) =>
            throw new DocumentationProviderFailedException(reason ?? string.Empty);

        public override void LogRawMessage(LogLevel type, string code, string message, Exception exception,
                                           params object[] parameters)
        {
            if (type > MaxLevel)
                type = MaxLevel;

            loggerBaseImplementation.LogRawMessage(type, code, message, exception, parameters);
        }
    }
}