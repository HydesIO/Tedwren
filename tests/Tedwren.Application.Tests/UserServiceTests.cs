using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Users;
using Tedwren.Abstractions.Notifications;
using Tedwren.Application.Notifications;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Users;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the console user-management rules (SF-20/SF-23/Q2) on <see cref="UserService"/> over a clean
/// in-memory store: inviting starts an account invited, duplicate emails are rejected, the auditor role is
/// read-only, suspending withdraws access without deleting the account, and inviting emails the branded
/// accept-invite link (best-effort).
/// </summary>
public sealed class UserServiceTests
{
    private static readonly Guid CompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    private static UserService CreateSut(out InMemoryUserStore store) =>
        CreateSut(out store, out _, new EmailOptions());

    private static UserService CreateSut(out InMemoryUserStore store, out NotificationOutbox outbox, EmailOptions options)
    {
        store = new InMemoryUserStore(seed: false);
        outbox = new NotificationOutbox();
        return new UserService(new InMemoryUserRepository(store), new OutboxEmailSender(outbox), options);
    }

    private static UserService CreateSut(out InMemoryUserStore store, IEmailSender email, EmailOptions options)
    {
        store = new InMemoryUserStore(seed: false);
        return new UserService(new InMemoryUserRepository(store), email, options);
    }

    /// <summary>An email sender that always fails, to prove invites survive a delivery outage.</summary>
    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("send failed");

        public Task SendHtmlAsync(string toEmail, string subject, string contentHtml, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("send failed");
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

    [Fact] // invite emails the branded accept-invite link when a real provider is configured
    public async Task InviteUser_WithResendProvider_EmailsAcceptLink_AndReportsSent()
    {
        var options = new EmailOptions { Provider = EmailProvider.Resend, ConsoleBaseUrl = "https://console.tedwren.test" };
        var service = CreateSut(out _, out var outbox, options);

        var result = await service.InviteUserAsync(new InviteUserRequest(CompanyId, "Jo Bloggs", "jo@example.com", "SiteManager"));

        Assert.True(result.EmailSent);
        var message = Assert.Single(outbox.Messages);
        Assert.Equal("Email", message.Channel);
        Assert.Equal("jo@example.com", message.Recipient);
        Assert.Contains($"https://console.tedwren.test/accept-invite?token={result.AcceptToken}", message.Body);
    }

    [Fact] // with the outbox stub (default), the message is still recorded but EmailSent is false (fallback link)
    public async Task InviteUser_WithOutboxProvider_RecordsMessage_ButReportsNotSent()
    {
        var service = CreateSut(out _, out var outbox, new EmailOptions()); // Provider defaults to Outbox

        var result = await service.InviteUserAsync(new InviteUserRequest(CompanyId, "Jo Bloggs", "jo@example.com", "SiteManager"));

        Assert.False(result.EmailSent);
        Assert.Single(outbox.Messages);
    }

    [Fact] // best-effort: a delivery failure never loses the invite
    public async Task InviteUser_WhenSendThrows_StillCreatesUser_AndReportsNotSent()
    {
        var options = new EmailOptions { Provider = EmailProvider.Resend };
        var service = CreateSut(out _, new ThrowingEmailSender(), options);

        var result = await service.InviteUserAsync(new InviteUserRequest(CompanyId, "Jo Bloggs", "jo@example.com", "SiteManager"));

        Assert.False(result.EmailSent);
        Assert.NotEqual(Guid.Empty, result.UserId);
        var user = await service.GetUserAsync(result.UserId);
        Assert.Equal("Invited", user!.Status);
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
