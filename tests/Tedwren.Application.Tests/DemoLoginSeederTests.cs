using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Auth;
using Tedwren.Application.DemoData;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the startup demo-login seed (<see cref="DemoLoginSeeder"/>): when demo mode is enabled it makes every
/// published demo credential signable-in (the two demo-tenant administrators, the named platform administrators
/// and the demo operative), it is idempotent, it repairs a drifted password/status, and it does nothing at all
/// when demo mode is off (fail-closed).
/// </summary>
public sealed class DemoLoginSeederTests
{
    /// <summary>Builds a seeder over fresh, unseeded in-memory stores plus a user store, returning both for assertions.</summary>
    private static (DemoLoginSeeder Seeder, InMemoryUserStore Users, InMemoryOrganisationStore Org) CreateSut(bool demoEnabled)
    {
        var orgStore = new InMemoryOrganisationStore(seed: false);
        var userStore = new InMemoryUserStore(seed: false);
        var seeder = new DemoLoginSeeder(
            new InMemoryCompanyRepository(orgStore),
            new InMemoryUserRepository(userStore),
            new InMemoryPersonRepository(orgStore),
            new InMemoryEngagementRepository(orgStore),
            new DemoOptions { Enabled = demoEnabled });
        return (seeder, userStore, orgStore);
    }

    private static User? ByEmail(InMemoryUserStore users, string email) =>
        users.Users.Values.SingleOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

    [Fact]
    public async Task Run_makes_every_published_demo_login_signable_in_when_enabled()
    {
        var (seeder, users, _) = CreateSut(demoEnabled: true);

        await seeder.RunAsync();

        // The two demo-tenant administrators — active, in their own tenant, with the published demo password.
        var contractor = ByEmail(users, "contractor@tedwren.com");
        Assert.NotNull(contractor);
        Assert.Equal(DemoDataIds.MainCompanyId, contractor!.CompanyId);
        Assert.Equal(AccessRole.Administrator, contractor.Role);
        Assert.Equal(UserStatus.Active, contractor.Status);
        Assert.True(PasswordHasher.Verify(DemoCredentials.TenantPassword, contractor.PasswordHash));

        var subcontractor = ByEmail(users, "subcontractor@tedwren.com");
        Assert.NotNull(subcontractor);
        Assert.Equal(DemoDataIds.SubCompanyId, subcontractor!.CompanyId);
        Assert.Equal(UserStatus.Active, subcontractor.Status);
        Assert.True(PasswordHasher.Verify(DemoCredentials.TenantPassword, subcontractor.PasswordHash));

        // The three named platform administrators — active, in the Tedwren tenant, at the demo admin password.
        foreach (var admin in AdminUserSeeder.MasterAdmins)
        {
            var user = ByEmail(users, admin.Email);
            Assert.NotNull(user);
            Assert.Equal(AdminUserSeeder.SeedCompanyId, user!.CompanyId);
            Assert.Equal(AccessRole.Administrator, user.Role);
            Assert.Equal(UserStatus.Active, user.Status);
            Assert.True(PasswordHasher.Verify(DemoCredentials.AdminPassword, user.PasswordHash));
        }
    }

    [Fact]
    public async Task Run_seeds_the_demo_tenants_and_operative_identity_when_enabled()
    {
        var (seeder, _, org) = CreateSut(demoEnabled: true);

        await seeder.RunAsync();

        var companies = new InMemoryCompanyRepository(org);
        Assert.NotNull(await companies.GetByIdAsync(DemoDataIds.MainCompanyId));
        Assert.NotNull(await companies.GetByIdAsync(DemoDataIds.SubCompanyId));

        // The demo operative's person + active engagement, so the emulator's operative@ demo sign-in resolves.
        var people = new InMemoryPersonRepository(org);
        Assert.NotNull(await people.GetByIdAsync(DemoOperatives.PersonId));

        var engagements = new InMemoryEngagementRepository(org);
        var engagement = await engagements.GetAsync(DemoDataIds.MainCompanyId, DemoOperatives.EngagementId);
        Assert.NotNull(engagement);
        Assert.Equal(EngagementStatus.Active, engagement!.Status);
    }

    [Fact]
    public async Task Run_is_a_no_op_when_demo_mode_is_disabled()
    {
        var (seeder, users, org) = CreateSut(demoEnabled: false);

        await seeder.RunAsync();

        Assert.Empty(users.Users);
        Assert.Empty(await new InMemoryCompanyRepository(org).GetAllAsync());
    }

    [Fact]
    public async Task Run_is_idempotent()
    {
        var (seeder, users, _) = CreateSut(demoEnabled: true);

        await seeder.RunAsync();
        var afterFirst = users.Users.Count;
        await seeder.RunAsync();

        // No account is duplicated on a second run (accounts are keyed by email/deterministic id).
        Assert.Equal(afterFirst, users.Users.Count);
        Assert.Single(users.Users.Values.Where(u => string.Equals(u.Email, "contractor@tedwren.com", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Run_heals_a_drifted_password_and_suspended_status()
    {
        var (seeder, users, _) = CreateSut(demoEnabled: true);

        // Pre-existing platform admin whose password has drifted and whose account has been suspended.
        var email = AdminUserSeeder.MasterAdmins[0].Email;
        var existing = new User
        {
            CompanyId = AdminUserSeeder.SeedCompanyId,
            Name = "Drifted Admin",
            Email = email,
            Role = AccessRole.Administrator,
            Status = UserStatus.Suspended,
            PasswordHash = PasswordHasher.Hash("not-the-demo-password"),
        };
        users.Users[existing.Id] = existing;

        await seeder.RunAsync();

        var healed = ByEmail(users, email);
        Assert.NotNull(healed);
        Assert.Equal(existing.Id, healed!.Id); // repaired in place, not duplicated
        Assert.Equal(UserStatus.Active, healed.Status);
        Assert.True(PasswordHasher.Verify(DemoCredentials.AdminPassword, healed.PasswordHash));
    }
}
