using System;

namespace SharpGenTools.Sdk
{
    [Serializable]
    internal class CodeGenFailedException : Exception
    {
        public CodeGenFailedException()
        {
        }

        public CodeGenFailedException(string message) : base(message)
        {
        }

        public CodeGenFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}