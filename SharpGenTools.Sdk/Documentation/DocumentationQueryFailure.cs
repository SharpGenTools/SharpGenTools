#nullable enable

using System;
using System.Collections.Generic;

namespace SharpGenTools.Sdk.Documentation;

public sealed class DocumentationQueryFailure
{
    public DocumentationQueryFailure(string query) => Query = query ?? throw new ArgumentNullException(nameof(query));

    public IReadOnlyList<Exception>? Exceptions { get; set; }

    public string Query { get; }

    public string? FailedProviderName { get; set; }

    public bool TreatProviderFailuresAsErrors { get; set; }

    private sealed class QueryEqualityComparer : IEqualityComparer<DocumentationQueryFailure>
    {
        public bool Equals(DocumentationQueryFailure? x, DocumentationQueryFailure? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return string.Equals(x.Query, y.Query, StringComparison.InvariantCulture);
        }

        public int GetHashCode(DocumentationQueryFailure obj)
        {
            return StringComparer.InvariantCulture.GetHashCode(obj.Query);
        }
    }

    public static IEqualityComparer<DocumentationQueryFailure> QueryComparer { get; } = new QueryEqualityComparer();
}