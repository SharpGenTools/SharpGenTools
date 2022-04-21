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

namespace SharpGen.Logging;

/// <summary>
/// Root Logger class.
/// </summary>
public sealed class Logger : LoggerBase
{
    private int _errorCount;
    private readonly List<string> contextStack = new();
    private readonly Stack<LogLocation> fileLocationStack = new();

    public Logger(ILogger output, IProgressReport progress = null)
    {
        LoggerOutput = output;
        ProgressReport = progress;
    }

    /// <summary>
    /// Gets the context as a string.
    /// </summary>
    private string ContextAsText => HasContext ? string.Join("/", contextStack) : null;

    public override ILogger LoggerOutput { get; }

    private bool HasContext => contextStack.Count > 0;

    public override bool HasErrors => _errorCount > 0;

    public override IProgressReport ProgressReport { get; }

    /// <summary>
    ///   Pushes a context string.
    /// </summary>
    public override void PushContext(string context)
    {
        contextStack.Add(context);
    }

    /// <summary>
    ///   Pushes a context location.
    /// </summary>
    public override void PushLocation(string fileName, int line = 1, int column = 1)
    {
        fileLocationStack.Push(new LogLocation(fileName, line, column));
    }

    /// <summary>
    ///   Pops the context location.
    /// </summary>
    public override void PopLocation()
    {
        fileLocationStack.Pop();
    }

    /// <summary>
    ///   Pushes a context formatted string.
    /// </summary>
    public override void PushContext(string context, params object[] parameters)
    {
        contextStack.Add(string.Format(context, parameters));
    }

    /// <summary>
    ///   Pops the context.
    /// </summary>
    public override void PopContext()
    {
        if (contextStack.Count > 0)
            contextStack.RemoveAt(contextStack.Count - 1);
    }

    /// <summary>
    ///   Logs the specified progress level and message.
    /// </summary>
    public override void Progress(int level, string message, params object[] parameters)
    {
        Message(message, parameters);
        if (ProgressReport?.ProgressStatus(level, string.Format(message, parameters)) == true)
            Exit("Process aborted manually");
    }

    /// <summary>
    /// Exits the process.
    /// </summary>
    public override void Exit(string reason, params object[] parameters)
    {
        var message = string.Format(reason, parameters);
        ProgressReport?.FatalExit(message);
        LoggerOutput?.Exit(message, 1);
    }

    /// <summary>
    ///   Logs the raw message to the LoggerOutput.
    /// </summary>
    public override void LogRawMessage(LogLevel type, string code, string message, Exception exception, params object[] parameters)
    {
        var logLocation = fileLocationStack.Count > 0 ? fileLocationStack.Peek() : null;

        if (LoggerOutput == null)
            Console.WriteLine("Warning, unable to log error. No LoggerOutput configured");
        else
            LoggerOutput.Log(type, logLocation, ContextAsText, code, message, exception, parameters);

        if (type is LogLevel.Error or LogLevel.Fatal)
            _errorCount++;
    }
}