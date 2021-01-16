// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SharpGen.Logging
{
    /// <summary>
    /// Root Logger class.
    /// </summary>
    public sealed class Logger : LoggerBase
    {
        private int _errorCount;
        private readonly List<string> ContextStack = new List<string>();
        private readonly Stack<LogLocation> FileLocationStack = new Stack<LogLocation>();

        /// <summary>
        /// Initializes the <see cref="Logger"/> class.
        /// </summary>
        public Logger(ILogger output, IProgressReport progress = null)
        {
            LoggerOutput = output;
            ProgressReport = progress;
        }

        /// <summary>
        /// Gets the context as a string.
        /// </summary>
        /// <value>The context.</value>
        private string ContextAsText
        {
            get { return HasContext ? string.Join("/", ContextStack) : null; }
        }

        /// <summary>
        ///   Gets or sets the logger output.
        /// </summary>
        /// <value>The logger output.</value>
        public override ILogger LoggerOutput { get; }

        /// <summary>
        ///   Gets a value indicating whether this instance has context.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has context; otherwise, <c>false</c>.
        /// </value>
        private bool HasContext
        {
            get { return ContextStack.Count > 0; }
        }

        /// <summary>
        ///   Gets a value indicating whether this instance has errors.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has errors; otherwise, <c>false</c>.
        /// </value>
        public override bool HasErrors => _errorCount > 0;

        /// <summary>
        ///   Gets or sets the progress report.
        /// </summary>
        /// <value>The progress report.</value>
        public override IProgressReport ProgressReport { get; }

        /// <summary>
        ///   Pushes a context string.
        /// </summary>
        /// <param name = "context">The context.</param>
        public override void PushContext(string context)
        {
            ContextStack.Add(context);
        }

        /// <summary>
        ///   Pushes a context location.
        /// </summary>
        /// <param name = "fileName">Name of the file.</param>
        /// <param name = "line">The line.</param>
        /// <param name = "column">The column.</param>
        public override void PushLocation(string fileName, int line = 1, int column = 1)
        {
            FileLocationStack.Push(new LogLocation(fileName, line, column));
        }

        /// <summary>
        ///   Pops the context location.
        /// </summary>
        public override void PopLocation()
        {
            FileLocationStack.Pop();
        }

        /// <summary>
        ///   Pushes a context formatted string.
        /// </summary>
        /// <param name = "context">The context.</param>
        /// <param name = "parameters">The parameters.</param>
        public override void PushContext(string context, params object[] parameters)
        {
            ContextStack.Add(string.Format(context, parameters));
        }

        /// <summary>
        ///   Pops the context.
        /// </summary>
        public override void PopContext()
        {
            if (ContextStack.Count > 0)
                ContextStack.RemoveAt(ContextStack.Count - 1);
        }

        /// <summary>
        ///   Logs the specified progress level and message.
        /// </summary>
        /// <param name = "level">The level.</param>
        /// <param name = "message">The message.</param>
        /// <param name = "parameters">The parameters.</param>
        public override void Progress(int level, string message, params object[] parameters)
        {
            Message(message, parameters);
            if (ProgressReport != null)
            {
                if (ProgressReport.ProgressStatus(level, string.Format(message, parameters)))
                    Exit("Process aborted manually");
            }
        }

        /// <summary>
        /// Exits the process.
        /// </summary>
        /// <param name="reason">The reason.</param>
        /// <param name="parameters">The parameters.</param>
        public override void Exit(string reason, params object[] parameters)
        {
            string message = string.Format(reason, parameters);
            if (ProgressReport != null)
                ProgressReport.FatalExit(message);

            if (LoggerOutput != null)
                LoggerOutput.Exit(message, 1);
        }

        /// <summary>
        ///   Logs the raw message to the LoggerOutput.
        /// </summary>
        /// <param name = "type">The type.</param>
        /// <param name = "message">The message.</param>
        /// <param name = "exception">The exception.</param>
        /// <param name = "parameters">The parameters.</param>
        public override void LogRawMessage(LogLevel type, string code, string message, Exception exception, params object[] parameters)
        {
            var logLocation = FileLocationStack.Count > 0 ? FileLocationStack.Peek() : null;

            if (LoggerOutput == null)
                Console.WriteLine("Warning, unable to log error. No LoggerOutput configured");
            else
                LoggerOutput.Log(type, logLocation, ContextAsText, code, message, exception, parameters);

            if (type == LogLevel.Error || type == LogLevel.Fatal)
                _errorCount++;
        }
    }
}