using System;
using SharpGen.Doc;
using SharpGen.Logging;
using SharpGen.Platform;
using SharpGen.Platform.Documentation;
using SharpGenTools.Sdk.Internal;

#nullable enable

namespace SharpGenTools.Sdk.Documentation
{
    internal sealed class DocumentationContext : IDocumentationContext
    {
        public DocumentationContext(LoggerBase logger) =>
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        internal ObservableSet<DocumentationQueryFailure> Failures { get; } =
            new(DocumentationQueryFailure.QueryComparer);

        public IDocItem CreateItem() => new DocItem();

        public IDocSubItem CreateSubItem() => new DocSubItem();

        public LoggerBase Logger { get; }

        public IFindDocumentationResult CreateSuccessfulFindDocumentationResult(IDocItem item) =>
            new FindDocumentationResultSuccess(item);

        public IFindDocumentationResult CreateFailedFindDocumentationResult(TimeSpan retryDelay) =>
            new FindDocumentationResultFailure(retryDelay);
    }
}