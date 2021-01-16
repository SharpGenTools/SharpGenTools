using System;
using SharpGen.Doc;

namespace SharpGenTools.Sdk.Documentation
{
    internal sealed class FindDocumentationResultFailure : IFindDocumentationResult
    {
        public FindDocumentationResultFailure(TimeSpan retryDelay) => RetryDelay = retryDelay;

        public bool Success => false;
        public TimeSpan RetryDelay { get; }
    }
}