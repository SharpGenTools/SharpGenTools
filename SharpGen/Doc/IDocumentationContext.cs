#nullable enable

using System;
using SharpGen.Logging;

namespace SharpGen.Doc;

public interface IDocumentationContext
{
    IDocItem CreateItem();

    IDocSubItem CreateSubItem();

    LoggerBase Logger { get; }

    IFindDocumentationResult CreateSuccessfulFindDocumentationResult(IDocItem item);

    /// <remarks>
    /// Real delay before attempts to retry will not match <see cref="retryDelay"/> with any precision.
    /// It's more of a general guidance for the scheduler, rather than an actual time span.
    /// </remarks>
    IFindDocumentationResult CreateFailedFindDocumentationResult(TimeSpan retryDelay);
}