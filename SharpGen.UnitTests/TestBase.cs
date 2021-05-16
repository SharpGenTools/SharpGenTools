using System;
using System.Linq;
using SharpGen.Logging;
using SharpGen.Transform;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests
{
    public abstract class TestBase
    {
        private readonly XUnitLogger loggerImpl;
        private readonly IocServiceContainer serviceContainer = new();

        protected TestBase(ITestOutputHelper outputHelper)
        {
            loggerImpl = new XUnitLogger(outputHelper);
            serviceContainer.AddService(new Logger(loggerImpl));
            serviceContainer.AddService<IDocumentationLinker, DocumentationLinker>();
            serviceContainer.AddService<GlobalNamespaceProvider>();
            serviceContainer.AddService(new TypeRegistry(Ioc));
            Ioc.ConfigureServices(serviceContainer);
        }

        protected IDisposable LoggerEnvironment(LoggerAssertHandler handler)
        {
            return new LoggerTestEnvironment(loggerImpl, handler);
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Global
        protected IDisposable LoggerCodeRequiredEnvironment(string code)
        {
            void AssertHandler(XUnitLogEvent[] events)
            {
                Assert.Contains(code, events.Select(x => x.Code));
            }

            return LoggerEnvironment(AssertHandler);
        }

        protected IDisposable LoggerMessageCountEnvironment(int expectedMessageCount, params LogLevel[] levels)
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

        protected IDisposable LoggerEmptyEnvironment()
        {
            return LoggerEnvironment(LoggerEmptyHandler);
        }

        private static void LoggerEmptyHandler(XUnitLogEvent[] events)
        {
            Assert.Empty(events);
        }

        protected Ioc Ioc { get; } = new();
        protected Logger Logger => Ioc.Logger;
        protected TypeRegistry TypeRegistry => Ioc.TypeRegistry;

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