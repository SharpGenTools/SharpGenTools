using System;
using System.Collections.Generic;
using SharpGen.Logging;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests
{
    class XUnitLogger : LoggerBase
    {
        private readonly ITestOutputHelper output;

        public XUnitLogger(ITestOutputHelper output)
        {
            this.output = output;
        }

        public List<XUnitLogEvent> MessageLog { get; } = new List<XUnitLogEvent>();

        public override void Exit(string reason, int exitCode)
        {
            Assert.False(true, "SharpGen failed to run"); // Fail the test
        }

        public override void Log(LogLevel logLevel, LogLocation logLocation, string context, string code, string message, Exception exception, params object[] parameters)
        {
            var lineMessage = FormatMessage(logLevel, logLocation, context, message, exception, parameters);
            
            MessageLog.Add(new XUnitLogEvent(code, lineMessage, exception, logLevel));

            output.WriteLine(lineMessage);

            if (exception != null)
                output.WriteLine(exception.ToString());
        }
    }
}
