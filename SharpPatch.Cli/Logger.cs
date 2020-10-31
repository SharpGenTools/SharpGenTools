using System;

namespace SharpPatch.Cli
{
    class Logger : ILogger
    {
        public void Log(string message, params object[] parameters)
        {
            Console.WriteLine(message, parameters);
        }

        public void LogError(string message, params object[] parameters)
        {
            Console.WriteLine(message, parameters);
        }
    }
}
