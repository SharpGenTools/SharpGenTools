using System;
using System.Collections.Generic;
using System.Text;

namespace SharpPatch
{
    public interface ILogger
    {
        void Log(string message, params object[] parameters);

        void LogError(string message, params object[] parameters);
    }
}
