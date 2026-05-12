using Primitives.Auditing.Models;

namespace Primitives.Auditing.Abstractions;

/// <summary>
/// Records audit events and queries the audit trail.
/// </summary>
public interface IAuditLogger
{
    /// <summary>Records a single audit event.</summary>
    Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);

    /// <summary>Returns audit events matching the supplied query parameters, newest first.</summary>
    Task<AuditQueryResult> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default);
}
