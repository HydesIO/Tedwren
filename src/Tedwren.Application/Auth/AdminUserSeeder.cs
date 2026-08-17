using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Auth;

/// <summary>
/// Ensures the master administrator accounts exist so the console can be signed into on a fresh install
/// (idempotent). Seeds the bootstrap admin from the <c>Seed</c> configuration section plus the fixed set of
/// named Tedwren master administrators (<see cref="MasterAdmins"/>). Each is a full <see
/// cref="AccessRole.Administrator"/> — the highest console role — in the Tedwren tenant. Existing accounts are
/// left untouched (matched case-insensitively by email), so re-running never clobbers a chosen password or
/// role. A real deployment sets a strong password in the <c>Seed</c> section, which every seeded admin should
/// change on first sign-in.
/// </summary>
public sealed class AdminUserSeeder
{
    /// <summary>The company the bootstrap admin belongs to (matches the client's default tenant, R15).</summary>
    public static readonly Guid SeedCompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    /// <summary>
    /// The fixed tenant id for the seeded Subcontractor demo company (Apex). Kept wholly separate from the
    /// Main Contractor tenant (<see cref="SeedCompanyId"/>) so the two demo accounts share no data and neither
    /// can surface the other's records (R15) — the demos are independent, not linked in any way.
    /// </summary>
    public static readonly Guid SubcontractorSeedCompanyId = Guid.Parse("22222222-2222-4222-8222-000000000002");

    /// <summary>
    /// The named Tedwren master administrators, seeded on every environment. These are the platform operators;
    /// each is created as a full <see cref="AccessRole.Administrator"/> in the Tedwren tenant.
    /// </summary>
    public static readonly IReadOnlyList<SeedAdmin> MasterAdmins = new[]
    {
        new SeedAdmin("Leigh Hydes", "leigh.hydes@tedwren.com"),
        new SeedAdmin("James Darby", "james.darby@tedwren.com"),
        new SeedAdmin("James Wheeler", "james.wheeler@tedwren.com"),
    };

    private readonly IUserRepository _users;
    private readonly SeedAdminOptions _options;

    /// <summary>Creates the seeder over the user repository and seed options.</summary>
    public AdminUserSeeder(IUserRepository users, SeedAdminOptions options)
    {
        _users = users;
        _options = options;
    }

    /// <summary>Creates every master administrator that does not already exist.</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // The configurable bootstrap admin (a fresh install can always be signed into via the Seed section).
        await EnsureAdminAsync("Administrator", _options.Email, cancellationToken);

        // The named Tedwren master administrators.
        foreach (var admin in MasterAdmins)
        {
            await EnsureAdminAsync(admin.Name, admin.Email, cancellationToken);
        }
    }

    /// <summary>Creates one Administrator account with the seed password if no user with that email exists yet.</summary>
    private async Task EnsureAdminAsync(string name, string email, CancellationToken cancellationToken)
    {
        var existing = await _users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var admin = new User
        {
            CompanyId = SeedCompanyId,
            Name = name,
            Email = email,
            Role = AccessRole.Administrator,
            Status = UserStatus.Active,
            PasswordHash = PasswordHasher.Hash(_options.Password),
            PasswordSetUtc = DateTimeOffset.UtcNow,
        };
        await _users.AddAsync(admin, cancellationToken);
    }
}

/// <summary>A named master administrator to seed: display name plus the sign-in email identity.</summary>
/// <param name="Name">Full display name.</param>
/// <param name="Email">Sign-in email identity.</param>
public sealed record SeedAdmin(string Name, string Email);

/// <summary>Bootstrap-admin credentials, bound from the <c>Seed</c> configuration section.</summary>
public sealed class SeedAdminOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Seed";

    /// <summary>Bootstrap admin email (sign-in identity).</summary>
    public string Email { get; set; } = "admin@tedwren.local";

    /// <summary>
    /// Password used for every seeded master administrator. Override with a strong value in any real
    /// deployment; seeded admins should change it on first sign-in.
    /// </summary>
    public string Password { get; set; } = "ChangeMe12345";
}
