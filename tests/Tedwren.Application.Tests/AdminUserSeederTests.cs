using Tedwren.Application.Auth;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the master-administrator seed (<see cref="AdminUserSeeder"/>): the bootstrap admin plus the named
/// Tedwren master administrators are created as active <see cref="AccessRole.Administrator"/> accounts in the
/// seed tenant, it is idempotent (re-running adds nothing), and it never clobbers an existing account.
/// </summary>
public sealed class AdminUserSeederTests
{
    private static (AdminUserSeeder Seeder, InMemoryUserStore Store) CreateSut(SeedAdminOptions? options = null)
    {
        var store = new InMemoryUserStore(seed: false);
        var seeder = new AdminUserSeeder(new InMemoryUserRepository(store), options ?? new SeedAdminOptions());
        return (seeder, store);
    }

    [Fact]
    public async Task Run_SeedsBootstrapAndNamedMasterAdmins()
    {
        var (seeder, store) = CreateSut();

        await seeder.RunAsync();

        // The three named Tedwren master administrators are present.
        string[] expected = { "leigh.hydes@tedwren.com", "james.darby@tedwren.com", "james.wheeler@tedwren.com" };
        foreach (var email in expected)
        {
            var user = store.Users.Values.Single(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(AccessRole.Administrator, user.Role);
            Assert.Equal(UserStatus.Active, user.Status);
            Assert.Equal(AdminUserSeeder.SeedCompanyId, user.CompanyId);
            Assert.NotNull(user.PasswordHash); // signable-in immediately
        }

        // Plus the configurable bootstrap admin — four accounts in total.
        Assert.Contains(store.Users.Values, u => u.Email == new SeedAdminOptions().Email);
        Assert.Equal(4, store.Users.Count);
    }

    [Fact]
    public async Task Run_IsIdempotent()
    {
        var (seeder, store) = CreateSut();

        await seeder.RunAsync();
        await seeder.RunAsync();

        Assert.Equal(4, store.Users.Count);
    }

    [Fact]
    public async Task Run_DoesNotClobberExistingAccount()
    {
        var (seeder, store) = CreateSut();
        await seeder.RunAsync();

        // Demote a seeded master admin, then re-run: the existing account is left untouched.
        var leigh = store.Users.Values.Single(u => u.Email == "leigh.hydes@tedwren.com");
        leigh.Role = AccessRole.Auditor;

        await seeder.RunAsync();

        Assert.Equal(AccessRole.Auditor, store.Users.Values.Single(u => u.Email == "leigh.hydes@tedwren.com").Role);
        Assert.Equal(4, store.Users.Count);
    }
}
