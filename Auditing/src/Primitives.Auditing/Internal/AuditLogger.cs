using Microsoft.Extensions.Logging;
using Primitives.Auditing.Abstractions;
using Primitives.Auditing.Models;

namespace Primitives.Auditing.Internal;

/// <summary>Default <see cref="IAuditLogger"/> that delegates to <see cref="IAuditStore"/>.</summary>
internal sealed class AuditLogger : IAuditLogger
{
    private readonly IAuditStore _store;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(IAuditStore store, ILogger<AuditLogger> logger)
    {
        _store  = store;
        _logger = logger;
    }

    public async Task LogAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        await _store.SaveAsync(auditEvent, cancellationToken).ConfigureAwait(false);
        _logger.LogDebug(
            "Audit: [{Outcome}] {ActorId} performed {Action} on {ResourceType}/{ResourceId} (tenant={TenantId})",
            auditEvent.Outcome,
            auditEvent.ActorId,
            auditEvent.Action,
            auditEvent.ResourceType,
            auditEvent.ResourceId,
            auditEvent.TenantId);
    }

    public Task<AuditQueryResult> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
        => _store.QueryAsync(query, cancellationToken);
}
