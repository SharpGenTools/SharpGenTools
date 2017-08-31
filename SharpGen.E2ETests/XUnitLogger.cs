using SharpGen.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.E2ETests
{
    class XUnitLogger : LoggerBase
    {
        private ITestOutputHelper output;
        private bool failTestOnFatalError;

        public XUnitLogger(ITestOutputHelper output, bool failTestOnFatalError)
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

        public override void Log(LogLevel logLevel, LogLocation logLocation, string context, string message, Exception exception, params object[] parameters)
        {
            string lineMessage = FormatMessage(logLevel, logLocation, context, message, exception, parameters);

            output.WriteLine(lineMessage);

            if (exception != null)
                LogException(logLocation, exception);
        }
    }
}
