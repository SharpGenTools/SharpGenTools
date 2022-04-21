using System;

namespace SharpGenTools.Sdk;

internal sealed class CodeGenFailedException : Exception
{
    public CodeGenFailedException()
    {
    }

    public CodeGenFailedException(string message) : base(message)
    {
    }
}