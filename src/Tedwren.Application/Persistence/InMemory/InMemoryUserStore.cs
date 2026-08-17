using System.Collections.Concurrent;
using Tedwren.Application.Auth;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Shared in-memory store backing the mock user repository (API <c>DataSource=Mock</c>). Registered as a
/// singleton so reads and writes share one dataset within the process. Seeded with a small, deterministic
/// set of console users — one per role, including a read-only auditor (SF-23) and a suspended account — so
/// the Users screen has data and every state is visible. Two demo login accounts (Main Contractor and
/// Subcontractor) carry passwords so each separate demo tenant can be signed into. These password-bearing
/// demo accounts exist only in the in-memory data double, never in a real database.
/// </summary>
public sealed class InMemoryUserStore
{
    /// <summary>The Main Contractor demo company (matches the site store's owner company, R15).</summary>
    public static readonly Guid OwnerCompanyId = AdminUserSeeder.SeedCompanyId;

    /// <summary>The separate Subcontractor demo company (Apex).</summary>
    public static readonly Guid SubcontractorCompanyId = AdminUserSeeder.SubcontractorSeedCompanyId;

    /// <summary>Shared password for the two demo login accounts (mock data double only).</summary>
    public const string DemoPassword = "Demo12345!";

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
        // Main Contractor (Meridian) console users.
        Add("Alex Morgan", "alex.morgan@meridian.example", AccessRole.Administrator, UserStatus.Active, lastActiveDaysAgo: 0);
        Add("Priya Shah", "priya.shah@meridian.example", AccessRole.ComplianceManager, UserStatus.Active, lastActiveDaysAgo: 1);
        Add("Danny Cole", "danny.cole@meridian.example", AccessRole.SiteManager, UserStatus.Active, lastActiveDaysAgo: 3);
        Add("Ruth Bevan", "ruth.bevan@insurer.example", AccessRole.Auditor, UserStatus.Active, lastActiveDaysAgo: 12);
        Add("Sam Whitfield", "sam.whitfield@meridian.example", AccessRole.SiteManager, UserStatus.Invited, lastActiveDaysAgo: null);
        Add("Jordan Vale", "jordan.vale@meridian.example", AccessRole.ComplianceManager, UserStatus.Suspended, lastActiveDaysAgo: 96);

        // Subcontractor (Apex) console users — a wholly separate set on the separate demo tenant.
        Add("Grace Bello", "grace.bello@apex.example", AccessRole.Administrator, UserStatus.Active, lastActiveDaysAgo: 0, companyId: SubcontractorCompanyId);
        Add("Owen Pryce", "owen.pryce@apex.example", AccessRole.SiteManager, UserStatus.Active, lastActiveDaysAgo: 2, companyId: SubcontractorCompanyId);
        Add("Nadia Khan", "nadia.khan@apex.example", AccessRole.Auditor, UserStatus.Active, lastActiveDaysAgo: 9, companyId: SubcontractorCompanyId);

        // Two demo login accounts (with passwords) so each separate demo tenant can be signed into. The Main
        // Contractor demo is a compliance manager (a full tenant-console account that is not a Tedwren platform
        // operator), and the Subcontractor demo is an administrator of the Apex tenant.
        Add("Meridian Demo", "demo.main@tedwren.example", AccessRole.ComplianceManager, UserStatus.Active, lastActiveDaysAgo: 0, password: DemoPassword);
        Add("Apex Demo", "demo.sub@tedwren.example", AccessRole.Administrator, UserStatus.Active, lastActiveDaysAgo: 0, companyId: SubcontractorCompanyId, password: DemoPassword);
    }

    /// <summary>Adds a seed user, optionally on a specific tenant and with a set (sign-in) password.</summary>
    private void Add(
        string name,
        string email,
        AccessRole role,
        UserStatus status,
        int? lastActiveDaysAgo,
        Guid? companyId = null,
        string? password = null)
    {
        var user = new User
        {
            CompanyId = companyId ?? OwnerCompanyId,
            Name = name,
            Email = email,
            Role = role,
            Status = status,
            LastActiveUtc = lastActiveDaysAgo is { } days ? DateTimeOffset.UtcNow.AddDays(-days) : null,
            PasswordHash = password is null ? null : PasswordHasher.Hash(password),
            PasswordSetUtc = password is null ? null : DateTimeOffset.UtcNow,
        };
        Users[user.Id] = user;
    }
}
