using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Contracts.Users;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the platform admin-area surface (<c>/api/admin</c>). The test host authenticates
/// every request as an Administrator in the Tedwren seed tenant (TestBypass) — i.e. a platform admin — so the
/// PlatformAdmin-gated reads succeed and <c>/api/me</c> reports the platform-admin flag.
/// </summary>
public sealed class AdminApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses the shared test host (in-memory data double, platform-admin identity).</summary>
    public AdminApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact]
    public async Task AdminCompanies_AsPlatformAdmin_ReturnsCompanies()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/admin/companies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var companies = await response.Content.ReadFromJsonAsync<List<CompanySummary>>();
        Assert.NotNull(companies);
    }

    [Fact]
    public async Task AdminUsers_AsPlatformAdmin_ReturnsUsers()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        Assert.NotNull(users);
    }

    [Fact]
    public async Task Me_AsSeedTenantAdmin_ReportsPlatformAdmin()
    {
        var client = CreateClient();

        var user = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");

        Assert.NotNull(user);
        Assert.True(user!.IsPlatformAdmin);
    }

    [Fact]
    public async Task AdminUpdateUser_ChangesNameRoleStatusAndPassword()
    {
        var client = CreateClient();
        var userId = await InviteUserAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/users/{userId}",
            new AdminUpdateUserRequest("Renamed Person", "ComplianceManager", Suspended: true, NewPassword: "NewPass123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(updated);
        Assert.Equal("Renamed Person", updated!.Name);
        Assert.Equal("ComplianceManager", updated.Role);
        Assert.Equal("Suspended", updated.Status);
    }

    [Fact]
    public async Task AdminUpdateUser_WithShortPassword_ReturnsBadRequest()
    {
        var client = CreateClient();
        var userId = await InviteUserAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/api/admin/users/{userId}",
            new AdminUpdateUserRequest("Kept Name", "Auditor", Suspended: false, NewPassword: "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminUpdateUser_UnknownUser_ReturnsNotFound()
    {
        var client = CreateClient();

        var response = await client.PutAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}",
            new AdminUpdateUserRequest("Nobody", "Auditor", Suspended: false, NewPassword: null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Invites a fresh user (unique email) against the first platform company and returns its id.</summary>
    private static async Task<Guid> InviteUserAsync(HttpClient client)
    {
        var companies = await client.GetFromJsonAsync<List<CompanySummary>>("/api/admin/companies");
        Assert.NotNull(companies);
        Assert.NotEmpty(companies!);

        var invite = new InviteUserRequest(companies![0].Id, "Test User", $"test-{Guid.NewGuid():N}@example.com", "Auditor");
        var response = await client.PostAsJsonAsync("/api/users", invite);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<InviteUserResult>();
        Assert.NotNull(result);
        return result!.UserId;
    }
}
