using SharpGen.Logging;
using System;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests
{
    class XUnitLogger : LoggerBase
    {
        private ITestOutputHelper output;
        private bool failTestOnFatalError;

        public XUnitLogger(ITestOutputHelper output, bool failTestOnFatalError = true)
        {
            this.output = output;
            this.failTestOnFatalError = failTestOnFatalError;
        }

        public bool Success { get; private set; } = true;
        public string ExitReason { get; private set; }

        public override void Exit(string reason, int exitCode)
        {
            Success = exitCode == 0;
            ExitReason = reason;
            Assert.False(failTestOnFatalError, "SharpGen failed to run"); // Fail the test
        }

        public override void Log(LogLevel logLevel, LogLocation logLocation, string context, string code, string message, Exception exception, params object[] parameters)
        {
            string lineMessage = FormatMessage(logLevel, logLocation, context, message, exception, parameters);

            output.WriteLine(lineMessage);

            if (exception != null)
                LogException(logLocation, exception);
        }
    }
}
