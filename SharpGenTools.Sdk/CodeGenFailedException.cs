using System;

namespace SharpGenTools.Sdk
{
    internal class CodeGenFailedException : Exception
    {
        public CodeGenFailedException()
        {
        }

        public CodeGenFailedException(string message) : base(message)
        {
        }
    }
}