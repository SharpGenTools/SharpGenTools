using System;
using System.Runtime.Serialization;

namespace SharpGenTools.Sdk.Documentation
{
    internal sealed class DocumentationProviderFailedException : Exception
    {
        public DocumentationProviderFailedException()
        {
        }

        public DocumentationProviderFailedException(string message) : base(message)
        {
        }

        public DocumentationProviderFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public DocumentationProviderFailedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}