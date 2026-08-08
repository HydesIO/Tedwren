using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock audit repository (API mock mode). Registered as a singleton.
/// Seeded with a small demo trail under a fixed demo company so the API returns data.
/// </summary>
public sealed class InMemoryAuditStore
{
    /// <summary>The fixed demo company the seed entries belong to.</summary>
    public static readonly Guid DemoCompanyId = Guid.Parse("33333333-3333-4333-8333-000000000001");

    /// <summary>Audit entries by id.</summary>
    public ConcurrentDictionary<Guid, AuditEntry> Entries { get; } = new();

    /// <summary>Creates the store and loads the demo seed.</summary>
    public InMemoryAuditStore() : this(seed: true)
    {
    }

    /// <summary>Creates the store, optionally loading the demo seed (tests pass false for a clean store).</summary>
    public InMemoryAuditStore(bool seed)
    {
        if (!seed)
        {
            return;
        }

        Add("Alex Morgan", "Onboarded operative", "M. Adeyemi", "Workforce", new DateTimeOffset(2026, 8, 4, 9, 12, 0, TimeSpan.Zero));
        Add("System", "RAMS uploaded", "Harbour Point", "Documents", new DateTimeOffset(2026, 8, 4, 8, 48, 0, TimeSpan.Zero));
        Add("Dana Price", "Issued permit HW-2043", "Meridian Tower", "Permits", new DateTimeOffset(2026, 8, 4, 7, 30, 0, TimeSpan.Zero));
        Add("System", "Blocked site entry", "Kingsway Retail", "Access", new DateTimeOffset(2026, 8, 3, 17, 5, 0, TimeSpan.Zero));
        Add("Alex Morgan", "Updated company details", "Kingsway M&E", "Organisation", new DateTimeOffset(2026, 8, 3, 14, 22, 0, TimeSpan.Zero));
    }

    /// <summary>Adds a seed entry.</summary>
    private void Add(string actor, string action, string entity, string category, DateTimeOffset occurredUtc)
    {
        var entry = new AuditEntry
        {
            CompanyId = DemoCompanyId,
            Actor = actor,
            Action = action,
            Entity = entity,
            Category = category,
            OccurredUtc = occurredUtc,
        };
        Entries[entry.Id] = entry;
    }
}
