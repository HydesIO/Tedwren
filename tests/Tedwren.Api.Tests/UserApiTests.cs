using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Users;
using Tedwren.Abstractions.Notifications;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the console user endpoints in the API's default (mock) mode (SF-20/SF-23/Q2):
/// the seed list is returned, inviting adds an account, duplicate emails conflict, and suspend/reactivate
/// change status without removing the account.
/// </summary>
public sealed class UserApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>The seed company invited users are attached to in these tests.</summary>
    private static readonly Guid CompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    /// <summary>Uses an isolated host so user state does not leak between tests.</summary>
    public UserApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact]
    public async Task GetUsers_ReturnsSeededList()
    {
        var client = CreateClient();

        var users = await client.GetFromJsonAsync<IReadOnlyList<UserDto>>("/api/users");

        Assert.NotNull(users);
        Assert.NotEmpty(users!);
        Assert.Contains(users!, u => u.Status == "Suspended"); // every status is represented
    }

    [Fact] // SF-23 — the read-only auditor role is offered
    public async Task GetRoles_IncludeReadOnlyAuditor()
    {
        var client = CreateClient();

        var roles = await client.GetFromJsonAsync<IReadOnlyList<RoleOption>>("/api/users/roles");

        Assert.Contains(roles!, r => r.Value == "Auditor" && !r.CanWrite);
    }

    [Fact] // SF-20 — inviting creates an invited account, then suspend/reactivate flips status
    public async Task InviteThenSuspendThenReactivate_ChangesStatus()
    {
        var client = CreateClient();
        var invite = new InviteUserRequest(CompanyId, "Test Person", "test.person@example.com", "SiteManager");

        var created = await client.PostAsJsonAsync("/api/users", invite);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<InviteUserResult>())!.UserId;

        var invited = await client.GetFromJsonAsync<UserDto>($"/api/users/{id}");
        Assert.Equal("Invited", invited!.Status);

        var suspended = await (await client.PostAsync($"/api/users/{id}/suspend", null))
            .Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("Suspended", suspended!.Status);

        var reactivated = await (await client.PostAsync($"/api/users/{id}/reactivate", null))
            .Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal("Active", reactivated!.Status);
    }

    [Fact] // the invite is wired to the email pipeline: an invitation email is recorded to the outbox
    public async Task Invite_SendsInvitationEmail_WithAcceptLink()
    {
        var client = CreateClient();
        var invite = new InviteUserRequest(CompanyId, "Invitee Person", "invitee@example.com", "SiteManager");

        var created = await client.PostAsJsonAsync("/api/users", invite);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var result = (await created.Content.ReadFromJsonAsync<InviteUserResult>())!;

        var outbox = await client.GetFromJsonAsync<List<OutboxMessage>>("/api/jobs/outbox");
        var email = Assert.Single(outbox!, m => m.Channel == "Email" && m.Recipient == "invitee@example.com");
        Assert.Contains($"accept-invite?token={result.AcceptToken}", email.Body);
    }

    [Fact]
    public async Task Invite_DuplicateEmail_Conflicts()
    {
        var client = CreateClient();
        var invite = new InviteUserRequest(CompanyId, "Dup One", "dup@example.com", "Administrator");
        await client.PostAsJsonAsync("/api/users", invite);

        var second = await client.PostAsJsonAsync("/api/users",
            new InviteUserRequest(CompanyId, "Dup Two", "dup@example.com", "Auditor"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    private sealed record CreatedResponse(Guid Id);
}
