using System;
using System.Linq;
using SharpGen.Logging;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests
{
    public class TestBase
    {
        private readonly XUnitLogger loggerImpl;

        protected TestBase(ITestOutputHelper outputHelper)
        {
            loggerImpl = new XUnitLogger(outputHelper);
            Logger = new Logger(loggerImpl);
        }

        internal IDisposable LoggerEnvironment(LoggerAssertHandler handler)
        {
            return new LoggerTestEnvironment(loggerImpl, handler);
        }

        public IDisposable LoggerCodeRequiredEnvironment(string code)
        {
            void AssertHandler(XUnitLogEvent[] events)
            {
                Assert.Contains(code, events.Select(x => x.Code));
            }

            return LoggerEnvironment(AssertHandler);
        }

        public IDisposable LoggerMessageCountEnvironment(int expectedMessageCount, params LogLevel[] levels)
        {
            if (expectedMessageCount == 0 && levels.Length == 0)
                throw new InvalidOperationException($"Use {nameof(LoggerEmptyEnvironment)} instead");
            
            var takeLevels = levels.Where(x => x >= 0).ToArray();
            var skipLevels = levels.Where(x => x < 0).Select(x => ~x).ToArray();

            if (takeLevels.Concat(skipLevels).Any(x => !Enum.IsDefined(typeof(LogLevel), x)))
                throw new ArgumentOutOfRangeException(nameof(levels));
            
            if (takeLevels.Length == 0)
                takeLevels = new[] {LogLevel.Info, LogLevel.Warning, LogLevel.Error, LogLevel.Fatal};

            void AssertHandler(XUnitLogEvent[] events)
            {
                bool Predicate(XUnitLogEvent x) => takeLevels.Contains(x.Level) && !skipLevels.Contains(x.Level);

                Assert.Equal(expectedMessageCount, events.Count(Predicate));
            }

            return LoggerEnvironment(AssertHandler);
        }

        public IDisposable LoggerEmptyEnvironment()
        {
            return LoggerEnvironment(LoggerEmptyHandler);
        }

        private static void LoggerEmptyHandler(XUnitLogEvent[] events)
        {
            Assert.Empty(events);
        }

        protected Logger Logger { get; }

        private sealed class LoggerTestEnvironment : IDisposable
        {
            private readonly LoggerAssertHandler handler;
            private readonly XUnitLogger logger;
            private readonly int startIndex;

            public LoggerTestEnvironment(XUnitLogger logger, LoggerAssertHandler handler)
            {
                this.logger = logger;
                this.handler = handler;
                startIndex = logger.MessageLog.Count;
            }

            public void Dispose()
            {
                XUnitLogEvent[] events;
                var length = logger.MessageLog.Count - startIndex;

                if (length != 0)
                {
                    events = new XUnitLogEvent[length];
                    logger.MessageLog.CopyTo(startIndex, events, 0, length);
                }
                else
                {
                    events = Array.Empty<XUnitLogEvent>();
                }

                handler(events);
            }
        }
    }
}