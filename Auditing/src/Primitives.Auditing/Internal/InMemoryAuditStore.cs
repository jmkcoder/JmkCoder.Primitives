using System.Collections.Concurrent;
using Primitives.Auditing.Abstractions;
using Primitives.Auditing.Models;

namespace Primitives.Auditing.Internal;

/// <summary>Thread-safe in-memory audit store. Not suitable for production use.</summary>
internal sealed class InMemoryAuditStore : IAuditStore
{
    private readonly ConcurrentBag<AuditEvent> _events = [];

    public Task SaveAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        _events.Add(auditEvent);
        return Task.CompletedTask;
    }

    public Task<AuditQueryResult> QueryAsync(AuditQuery query, CancellationToken cancellationToken = default)
    {
        var filtered = _events.AsEnumerable();

        if (query.TenantId is not null)
            filtered = filtered.Where(e => string.Equals(e.TenantId, query.TenantId, StringComparison.OrdinalIgnoreCase));
        if (query.ActorId is not null)
            filtered = filtered.Where(e => string.Equals(e.ActorId, query.ActorId, StringComparison.OrdinalIgnoreCase));
        if (query.Action is not null)
            filtered = filtered.Where(e => string.Equals(e.Action, query.Action, StringComparison.OrdinalIgnoreCase));
        if (query.ResourceType is not null)
            filtered = filtered.Where(e => string.Equals(e.ResourceType, query.ResourceType, StringComparison.OrdinalIgnoreCase));
        if (query.ResourceId is not null)
            filtered = filtered.Where(e => string.Equals(e.ResourceId, query.ResourceId, StringComparison.OrdinalIgnoreCase));
        if (query.From.HasValue)
            filtered = filtered.Where(e => e.OccurredAt >= query.From.Value);
        if (query.To.HasValue)
            filtered = filtered.Where(e => e.OccurredAt < query.To.Value);
        if (query.Outcome.HasValue)
            filtered = filtered.Where(e => e.Outcome == query.Outcome.Value);

        var sorted     = filtered.OrderByDescending(e => e.OccurredAt).ToList();
        var totalCount = (long)sorted.Count;
        var page       = sorted.Skip(query.Page * query.PageSize).Take(query.PageSize).ToList();

        return Task.FromResult(new AuditQueryResult
        {
            Events     = page,
            TotalCount = totalCount,
            Page       = query.Page,
            PageSize   = query.PageSize,
        });
    }
}
