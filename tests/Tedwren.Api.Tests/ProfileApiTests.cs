using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Contracts.Account;
using Tedwren.Api.Auth;
using Tedwren.Application.Auth;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the self-service account endpoints (<c>/api/me/*</c>, SF-20): the whole group is
/// authenticated (401 for anonymous, secure-by-default), the caller only ever reads/writes their own record,
/// change-password verifies the current secret, and an avatar upload round-trips. The authenticated caller is
/// seeded to match the test-bypass identity so "the caller" resolves deterministically.
/// </summary>
public sealed class ProfileApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>The seed company the fabricated caller belongs to (matches the client's default tenant).</summary>
    private static readonly Guid CompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    private const string CallerPassword = "CallerPass1";

    public ProfileApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>Overwrites the test-bypass caller with a fresh active administrator, so each test starts clean.</summary>
    private HttpClient CreateAuthenticatedClient()
    {
        var store = _factory.Services.GetRequiredService<InMemoryUserStore>();
        store.Users[TestBypassAuthenticationHandler.OperatorId] = new User
        {
            Id = TestBypassAuthenticationHandler.OperatorId,
            CompanyId = CompanyId,
            Name = "Test Caller",
            Email = "test.caller@example.com",
            Role = AccessRole.Administrator,
            Status = UserStatus.Active,
            PasswordHash = PasswordHasher.Hash(CallerPassword),
            PasswordSetUtc = DateTimeOffset.UtcNow.AddDays(-10),
        };
        return _factory.CreateClient();
    }

    [Fact] // secure-by-default: every /api/me endpoint rejects an anonymous caller
    public async Task MeEndpoints_Anonymous_Return401()
    {
        var anon = _factory.WithWebHostBuilder(b => b.UseSetting("Auth:TestBypass", "false")).CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/me/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anon.PutAsJsonAsync("/api/me/profile", new UpdateMyProfileRequest("X", "x@y.com", null))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anon.PostAsJsonAsync("/api/me/password", new ChangePasswordRequest("a", "bbbbbbbb"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await anon.PostAsJsonAsync("/api/me/avatar", new UpdateAvatarRequest("AAAA", "image/png"))).StatusCode);
    }

    [Fact] // SF-20 — the authenticated caller reads their own record
    public async Task GetProfile_ReturnsOwnRecord()
    {
        var client = CreateAuthenticatedClient();

        var me = await client.GetFromJsonAsync<MyProfileDto>("/api/me/profile");

        Assert.NotNull(me);
        Assert.Equal("test.caller@example.com", me!.Email);
        Assert.True(me.IsAdministrator);
    }

    [Fact] // SF-20 — updating details persists and is reflected on the next read
    public async Task UpdateProfile_PersistsChanges()
    {
        var client = CreateAuthenticatedClient();

        var updated = await client.PutAsJsonAsync("/api/me/profile",
            new UpdateMyProfileRequest("Renamed Caller", "renamed.caller@example.com", "07700 900999"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var me = await client.GetFromJsonAsync<MyProfileDto>("/api/me/profile");
        Assert.Equal("Renamed Caller", me!.Name);
        Assert.Equal("07700 900999", me.Mobile);
    }

    [Fact] // wrong current password is rejected with a 400
    public async Task ChangePassword_WrongCurrent_BadRequest()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/me/password",
            new ChangePasswordRequest("not-the-password", "BrandNewPass1"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact] // the correct current password lets the caller set a new one (204)
    public async Task ChangePassword_Correct_NoContent()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.PostAsJsonAsync("/api/me/password",
            new ChangePasswordRequest(CallerPassword, "BrandNewPass1"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact] // SF-20/R9 — an avatar upload returns the profile with a resolved image URL
    public async Task UploadAvatar_ReturnsResolvedUrl()
    {
        var client = CreateAuthenticatedClient();
        var base64 = Convert.ToBase64String(new byte[] { 9, 8, 7, 6 });

        var response = await client.PostAsJsonAsync("/api/me/avatar", new UpdateAvatarRequest(base64, "image/png"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await response.Content.ReadFromJsonAsync<MyProfileDto>();
        Assert.NotNull(me!.AvatarUrl);
        Assert.StartsWith("/api/images/", me.AvatarUrl);
    }
}
