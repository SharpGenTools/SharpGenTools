using System;

namespace SharpGen.Logging
{
    public abstract class LoggerBase
    {
        /// <summary>
        ///   Gets or sets the logger output.
        /// </summary>
        /// <value>The logger output.</value>
        public abstract ILogger LoggerOutput { get; }

        /// <summary>
        ///   Gets a value indicating whether this instance has errors.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has errors; otherwise, <c>false</c>.
        /// </value>
        public abstract bool HasErrors { get; }

        /// <summary>
        ///   Gets or sets the progress report.
        /// </summary>
        /// <value>The progress report.</value>
        public abstract IProgressReport ProgressReport { get; }

        /// <summary>
        ///   Runs a delegate in the specified log context.
        /// </summary>
        /// <param name = "context">The context.</param>
        /// <param name = "method">The method.</param>
        public void RunInContext(string context, Action method)
        {
            try
            {
                PushContext(context);
                method();
            }
            finally
            {
                PopContext();
            }
        }

        /// <summary>
        ///   Pushes a context string.
        /// </summary>
        /// <param name = "context">The context.</param>
        public abstract void PushContext(string context);

        /// <summary>
        ///   Pushes a context location.
        /// </summary>
        /// <param name = "fileName">Name of the file.</param>
        /// <param name = "line">The line.</param>
        /// <param name = "column">The column.</param>
        public abstract void PushLocation(string fileName, int line = 1, int column = 1);

        /// <summary>
        ///   Pops the context location.
        /// </summary>
        public abstract void PopLocation();

        /// <summary>
        ///   Pushes a context formatted string.
        /// </summary>
        /// <param name = "context">The context.</param>
        /// <param name = "parameters">The parameters.</param>
        public abstract void PushContext(string context, params object[] parameters);

        /// <summary>
        ///   Pops the context.
        /// </summary>
        public abstract void PopContext();

        /// <summary>
        ///   Logs the specified message.
        /// </summary>
        /// <param name = "message">The message.</param>
        public void Message(string message)
        {
            Message("{0}", message);
        }

        /// <summary>
        ///   Logs the specified message.
        /// </summary>
        /// <param name = "message">The message.</param>
        /// <param name = "parameters">The parameters.</param>
        public void Message(string message, params object[] parameters)
        {
            LogRawMessage(LogLevel.Info, null, message, null, parameters);
        }

        /// <summary>
        ///   Logs the specified progress level and message.
        /// </summary>
        /// <param name = "level">The level.</param>
        /// <param name = "message">The message.</param>
        /// <param name = "parameters">The parameters.</param>
        public abstract void Progress(int level, string message, params object[] parameters);

        /// <summary>
        ///   Logs the specified warning.
        /// </summary>
        /// <param name = "message">The message.</param>
        public void Warning(string code, string message)
        {
            Warning(code, "{0}", message);
        }

        /// <summary>
        ///   Logs the specified warning.
        /// </summary>
        public void Warning(string code, string message, params object[] parameters)
        {
            LogRawMessage(LogLevel.Warning, code, message, null, parameters);
        }

        /// <summary>
        ///   Logs the specified error.
        /// </summary>
        public void Error(string code, string message, Exception ex, params object[] parameters)
        {
            LogRawMessage(LogLevel.Error, code, message, ex, parameters);
        }

        /// <summary>
        ///   Logs the specified error.
        /// </summary>
        public void Error(string code, string message)
        {
            Error(code, "{0}", message);
        }

        /// <summary>
        ///   Logs the specified error.
        /// </summary>
        public void Error(string code, string message, params object[] parameters)
        {
            Error(code, message, null, parameters);
        }

        /// <summary>
        ///   Logs the specified fatal error.
        /// </summary>
        public void Fatal(string message, Exception ex, params object[] parameters)
        {
            LogRawMessage(LogLevel.Fatal, null, message, ex, parameters);
            Exit("A fatal error occured");
        }

        /// <summary>
        ///   Logs the specified fatal error.
        /// </summary>
        public void Fatal(string message)
        {
            Fatal("{0}", message);
        }

        /// <summary>
        ///   Logs the specified fatal error.
        /// </summary>
        public void Fatal(string message, params object[] parameters)
        {
            Fatal(message, null, parameters);
        }

        /// <summary>
        /// Exits the process.
        /// </summary>
        public abstract void Exit(string reason, params object[] parameters);

        /// <summary>
        ///   Logs the raw message to the LoggerOutput.
        /// </summary>
        public abstract void LogRawMessage(LogLevel type, string code, string message, Exception exception, params object[] parameters);
    }
}