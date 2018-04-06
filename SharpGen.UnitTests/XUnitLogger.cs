using SharpGen.Logging;
using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests
{
    class XUnitLogger : LoggerBase
    {
        private ITestOutputHelper output;

        public XUnitLogger(ITestOutputHelper output)
        {
            this.output = output;
        }

        public HashSet<string> LoggerCodesEncountered { get; } = new HashSet<string>();

        public override void Exit(string reason, int exitCode)
        {
            Assert.False(true, "SharpGen failed to run"); // Fail the test
        }

        public override void Log(LogLevel logLevel, LogLocation logLocation, string context, string code, string message, Exception exception, params object[] parameters)
        {
            LoggerCodesEncountered.Add(code);
            string lineMessage = FormatMessage(logLevel, logLocation, context, message, exception, parameters);

            output.WriteLine(lineMessage);

            if (exception != null)
                output.WriteLine(exception.ToString());
        }
    }
}
