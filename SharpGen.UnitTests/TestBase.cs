using SharpGen.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace SharpGen.UnitTests
{
    public class TestBase
    {
        private readonly ITestOutputHelper outputHelper;
        private readonly XUnitLogger loggerImpl;

        protected TestBase(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
            loggerImpl = new XUnitLogger(outputHelper);
            Logger = new Logger(loggerImpl);
        }

        public void AssertLoggingCodeLogged(string code)
        {
            Assert.Contains(code, loggerImpl.LoggerCodesEncountered);
        }

        protected Logger Logger { get; }
    }
}
