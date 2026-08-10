using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IAuditRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryAuditRepository : IAuditRepository
{
    private readonly InMemoryAuditStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryAuditRepository(InMemoryAuditStore store) => _store = store;

    /// <summary>Searches by optional company, free text and date range, newest first.</summary>
    public Task<IReadOnlyList<AuditEntry>> SearchAsync(Guid? companyId, string? text, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        IEnumerable<AuditEntry> query = _store.Entries.Values;

        if (companyId is { } company)
        {
            query = query.Where(e => e.CompanyId == company);
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            query = query.Where(e => Matches(e, text));
        }

        if (from is { } start)
        {
            query = query.Where(e => DateOnly.FromDateTime(e.OccurredUtc.UtcDateTime) >= start);
        }

        if (to is { } end)
        {
            query = query.Where(e => DateOnly.FromDateTime(e.OccurredUtc.UtcDateTime) <= end);
        }

        IReadOnlyList<AuditEntry> results = query.OrderByDescending(e => e.OccurredUtc).ToList();
        return Task.FromResult(results);
    }

    /// <summary>Appends an audit entry.</summary>
    public Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        _store.Entries[entry.Id] = entry;
        return Task.CompletedTask;
    }

    /// <summary>Case-insensitive free-text match across actor, action, entity and reference (SF-20).</summary>
    private static bool Matches(AuditEntry e, string text) =>
        e.Actor.Contains(text, StringComparison.OrdinalIgnoreCase)
        || e.Action.Contains(text, StringComparison.OrdinalIgnoreCase)
        || e.Entity.Contains(text, StringComparison.OrdinalIgnoreCase)
        || (e.Reference?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false);
}
