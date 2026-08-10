using System.Collections.Concurrent;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock user repository (API <c>DataSource=Mock</c>). Registered as a
/// singleton so reads and writes share one dataset within the process. Seeded with a small, deterministic
/// set of console users — one per role, including a read-only auditor (SF-23) and a suspended account — so
/// the Users screen has data and every state is visible.
/// </summary>
public sealed class InMemoryUserStore
{
    /// <summary>The company these seed users belong to (matches the site store's owner company, R15).</summary>
    public static readonly Guid OwnerCompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    /// <summary>Users by id.</summary>
    public ConcurrentDictionary<Guid, User> Users { get; } = new();

    /// <summary>Creates the store and loads the demo seed.</summary>
    public InMemoryUserStore() : this(seed: true)
    {
    }

    /// <summary>Creates the store, optionally loading the demo seed (tests pass false for a clean store).</summary>
    public InMemoryUserStore(bool seed)
    {
        if (seed)
        {
            Seed();
        }
    }

    /// <summary>Loads a small, deterministic set of console users covering every role and status.</summary>
    private void Seed()
    {
        Add("Alex Morgan", "alex.morgan@meridian.example", AccessRole.Administrator, UserStatus.Active, lastActiveDaysAgo: 0);
        Add("Priya Shah", "priya.shah@meridian.example", AccessRole.ComplianceManager, UserStatus.Active, lastActiveDaysAgo: 1);
        Add("Danny Cole", "danny.cole@meridian.example", AccessRole.SiteManager, UserStatus.Active, lastActiveDaysAgo: 3);
        Add("Ruth Bevan", "ruth.bevan@insurer.example", AccessRole.Auditor, UserStatus.Active, lastActiveDaysAgo: 12);
        Add("Sam Whitfield", "sam.whitfield@meridian.example", AccessRole.SiteManager, UserStatus.Invited, lastActiveDaysAgo: null);
        Add("Jordan Vale", "jordan.vale@meridian.example", AccessRole.ComplianceManager, UserStatus.Suspended, lastActiveDaysAgo: 96);
    }

    /// <summary>Adds a seed user.</summary>
    private void Add(string name, string email, AccessRole role, UserStatus status, int? lastActiveDaysAgo)
    {
        var user = new User
        {
            CompanyId = OwnerCompanyId,
            Name = name,
            Email = email,
            Role = role,
            Status = status,
            LastActiveUtc = lastActiveDaysAgo is { } days ? DateTimeOffset.UtcNow.AddDays(-days) : null,
        };
        Users[user.Id] = user;
    }
}
