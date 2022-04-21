using System;
using System.Text;

namespace SharpGen.Logging;

public static class LogUtilities
{
    /// <summary>
    /// Formats the message.
    /// </summary>
    /// <param name="logLevel">The log level.</param>
    /// <param name="logLocation">The log location.</param>
    /// <param name="context">The context.</param>
    /// <param name="message">The message.</param>
    /// <param name="exception">The exception.</param>
    /// <param name="parameters">The parameters.</param>
    /// <returns></returns>
    public static string FormatMessage(LogLevel logLevel, LogLocation logLocation, string context, string message, Exception exception, params object[] parameters)
    {
        var lineMessage = new StringBuilder();

        if (logLocation != null)
            lineMessage.AppendFormat("{0}({1},{2}): ", logLocation.File, logLocation.Line, logLocation.Column);

        // Write log parsable by Visual Studio
        var levelName = Enum.GetName(typeof (LogLevel), logLevel).ToLower();
        lineMessage.AppendFormat("{0}:{1}{2}", levelName , (context != null) ? $" in {context} " : "", message != null ? string.Format(message, parameters) : "");

        return lineMessage.ToString();
    }
}