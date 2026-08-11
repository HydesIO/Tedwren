using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Auth;

/// <summary>
/// Ensures a bootstrap Administrator account exists so the console can be signed into on a fresh install
/// (idempotent). Email + password come from the <c>Seed</c> configuration section; a real deployment sets a
/// strong password there. Does nothing if a user with the seed email already exists.
/// </summary>
public sealed class AdminUserSeeder
{
    /// <summary>The company the bootstrap admin belongs to (matches the client's default tenant, R15).</summary>
    public static readonly Guid SeedCompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    private readonly IUserRepository _users;
    private readonly SeedAdminOptions _options;

    /// <summary>Creates the seeder over the user repository and seed options.</summary>
    public AdminUserSeeder(IUserRepository users, SeedAdminOptions options)
    {
        _users = users;
        _options = options;
    }

    /// <summary>Creates the bootstrap admin if it does not already exist.</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _users.GetByEmailAsync(_options.Email, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var admin = new User
        {
            CompanyId = SeedCompanyId,
            Name = "Administrator",
            Email = _options.Email,
            Role = AccessRole.Administrator,
            Status = UserStatus.Active,
            PasswordHash = PasswordHasher.Hash(_options.Password),
            PasswordSetUtc = DateTimeOffset.UtcNow,
        };
        await _users.AddAsync(admin, cancellationToken);
    }
}

/// <summary>Bootstrap-admin credentials, bound from the <c>Seed</c> configuration section.</summary>
public sealed class SeedAdminOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Seed";

    /// <summary>Bootstrap admin email (sign-in identity).</summary>
    public string Email { get; set; } = "admin@tedwren.local";

    /// <summary>Bootstrap admin password. Override with a strong value in any real deployment.</summary>
    public string Password { get; set; } = "ChangeMe12345";
}
