using System;
using FakeItEasy;
using SharpGen.Logging;
using Xunit;

namespace SharpGen.UnitTests
{
    public class LoggerTests
    {
        [Fact]
        public void LoggerFatalCallsLoggerOutputExit()
        {
            var output = A.Fake<ILogger>();

            var logger = new Logger(output);

            logger.Fatal("Fatal error");

            A.CallTo(() => output.Log(A<LogLevel>.Ignored, A<LogLocation>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<Exception>.Ignored, A<object[]>.Ignored)).MustHaveHappened();
            A.CallTo(() => output.Exit(A<string>.Ignored, A<int>.Ignored)).MustHaveHappened();
        }
    }
}