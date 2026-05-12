namespace Primitives.Auditing.Models;

/// <summary>Paginated result from an audit trail query.</summary>
public sealed class AuditQueryResult
{
    /// <summary>Events matching the query on the requested page, newest first.</summary>
    public required IReadOnlyList<AuditEvent> Events { get; init; }

    /// <summary>Total number of matching events across all pages.</summary>
    public required long TotalCount { get; init; }

    /// <summary>The page index that produced this result.</summary>
    public required int Page { get; init; }

    /// <summary>The page size used for this query.</summary>
    public required int PageSize { get; init; }
}
