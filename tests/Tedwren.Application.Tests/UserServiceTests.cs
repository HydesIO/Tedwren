using Tedwren.Abstractions.Contracts.Users;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Users;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the console user-management rules (SF-20/SF-23/Q2) on <see cref="UserService"/> over a clean
/// in-memory store: inviting starts an account invited, duplicate emails are rejected, the auditor role is
/// read-only, and suspending withdraws access without deleting the account.
/// </summary>
public sealed class UserServiceTests
{
    private static readonly Guid CompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    private static UserService CreateSut(out InMemoryUserStore store)
    {
        store = new InMemoryUserStore(seed: false);
        return new UserService(new InMemoryUserRepository(store));
    }

    [Fact] // SF-20
    public async Task InviteUser_StartsInvited_AndIsListed()
    {
        var service = CreateSut(out _);

        var id = (await service.InviteUserAsync(new InviteUserRequest(CompanyId, "Jo Bloggs", "jo@example.com", "SiteManager"))).UserId;

        var user = await service.GetUserAsync(id);
        Assert.NotNull(user);
        Assert.Equal("Invited", user!.Status);
        Assert.Equal("SiteManager", user.Role);
        Assert.Single(await service.GetUsersAsync());
    }

    [Fact] // R15 — the invited user is attached to the requested company, not a hard-coded one
    public async Task InviteUser_AttachesToRequestedCompany()
    {
        var service = CreateSut(out var store);
        var companyId = Guid.Parse("33333333-3333-4333-8333-000000000009");

        var id = (await service.InviteUserAsync(new InviteUserRequest(companyId, "Jo Bloggs", "jo@example.com", "Administrator"))).UserId;

        Assert.Equal(companyId, store.Users[id].CompanyId);
    }

    [Fact] // a company id is required to place the new user
    public async Task InviteUser_EmptyCompanyId_Throws()
    {
        var service = CreateSut(out _);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.InviteUserAsync(new InviteUserRequest(Guid.Empty, "Jo Bloggs", "jo@example.com", "Administrator")));
    }

    [Fact] // one account per email
    public async Task InviteUser_DuplicateEmail_Throws()
    {
        var service = CreateSut(out _);
        await service.InviteUserAsync(new InviteUserRequest(CompanyId, "Jo Bloggs", "jo@example.com", "Administrator"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.InviteUserAsync(new InviteUserRequest(CompanyId, "Jo Two", "JO@EXAMPLE.COM", "Auditor")));
    }

    [Theory]
    [InlineData("", "jo@example.com")]
    [InlineData("Jo", "not-an-email")]
    public async Task InviteUser_InvalidInput_Throws(string name, string email)
    {
        var service = CreateSut(out _);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.InviteUserAsync(new InviteUserRequest(CompanyId, name, email, "Administrator")));
    }

    [Fact] // SF-23
    public async Task Roles_IncludeReadOnlyAuditor()
    {
        var service = CreateSut(out _);

        var roles = await service.GetRolesAsync();

        var auditor = Assert.Single(roles, r => r.Value == nameof(AccessRole.Auditor));
        Assert.False(auditor.CanWrite);
        Assert.All(roles.Where(r => r.Value != nameof(AccessRole.Auditor)), r => Assert.True(r.CanWrite));
    }

    [Fact] // SF-20 — access withdrawn without deletion
    public async Task Suspend_ThenReactivate_KeepsAccount()
    {
        var service = CreateSut(out _);
        var id = (await service.InviteUserAsync(new InviteUserRequest(CompanyId, "Jo Bloggs", "jo@example.com", "ComplianceManager"))).UserId;

        var suspended = await service.SuspendUserAsync(id);
        Assert.Equal("Suspended", suspended!.Status);
        Assert.Single(await service.GetUsersAsync()); // still present, not deleted

        var reactivated = await service.ReactivateUserAsync(id);
        Assert.Equal("Active", reactivated!.Status);
    }

    [Fact]
    public async Task UpdateUser_ChangesNameAndRole()
    {
        var service = CreateSut(out _);
        var id = (await service.InviteUserAsync(new InviteUserRequest(CompanyId, "Jo Bloggs", "jo@example.com", "Auditor"))).UserId;

        var updated = await service.UpdateUserAsync(id, new UpdateUserRequest("Josephine Bloggs", "Administrator"));

        Assert.Equal("Josephine Bloggs", updated!.Name);
        Assert.Equal("Administrator", updated.Role);
        Assert.True(updated.CanWrite);
    }

    [Fact]
    public async Task UpdateOrSuspend_UnknownUser_ReturnsNull()
    {
        var service = CreateSut(out _);

        Assert.Null(await service.UpdateUserAsync(Guid.NewGuid(), new UpdateUserRequest("X", "Auditor")));
        Assert.Null(await service.SuspendUserAsync(Guid.NewGuid()));
        Assert.Null(await service.ReactivateUserAsync(Guid.NewGuid()));
    }
}
