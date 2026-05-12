using Primitives.Auditing.Models;

namespace Primitives.Auditing.Abstractions;

/// <summary>
/// Persistent store for audit events.
/// Replace the default in-memory store with a database-backed implementation for production.
/// </summary>
public interface IAuditStore
{
    /// <summary>Persists a single audit event.</summary>
    Task SaveAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>Queries stored audit events according to <paramref name="query"/> criteria.</summary>
    Task<AuditQueryResult> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default);
}
