using SharpGen.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit.Abstractions;

namespace SharpGen.E2ETests
{
    public class TestBase
    {
        private readonly ITestOutputHelper outputHelper;

        protected TestBase(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
            Logger = new Logger(new XUnitLogger(outputHelper));
        }

        protected Logger Logger { get; }
    }
}
