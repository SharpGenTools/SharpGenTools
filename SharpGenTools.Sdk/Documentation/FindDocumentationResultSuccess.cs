using System;
using SharpGen.Doc;

namespace SharpGenTools.Sdk.Documentation
{
    internal sealed class FindDocumentationResultSuccess : IFindDocumentationResult
    {
        public FindDocumentationResultSuccess(IDocItem item) =>
            Item = item ?? throw new ArgumentNullException(nameof(item));

        public bool Success => true;
        public IDocItem Item { get; }
    }
}