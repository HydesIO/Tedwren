using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tedwren.Abstractions.Services;
using Tedwren.Api.Auth;
using Tedwren.Application.Auth;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// Unit tests for the platform-admin computation in <see cref="ClaimsCurrentUserService"/>: a platform admin
/// is an <see cref="AccessRole.Administrator"/> in the fixed Tedwren tenant, and nothing else. This is the
/// authoritative, server-side gate for the admin area, so it is verified independently of the HTTP host.
/// </summary>
public sealed class ClaimsCurrentUserServiceTests
{
    // Builds the service over a fabricated principal. With no claims the identity is unauthenticated.
    private static ICurrentUserService ServiceFor(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: claims.Length > 0 ? "Test" : null);
        var context = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        // An empty in-memory user repository is enough here: these tests only assert the platform-admin
        // computation, which is derived from claims and never touches the stored user record.
        var users = new InMemoryUserRepository(new InMemoryUserStore(seed: false));
        return new ClaimsCurrentUserService(new HttpContextAccessor { HttpContext = context }, users);
    }

    [Fact]
    public async Task Administrator_InSeedTenant_IsPlatformAdmin()
    {
        var service = ServiceFor(
            new Claim(ClaimTypes.Name, "Leigh Hydes"),
            new Claim(ClaimTypes.Role, AccessRole.Administrator.ToString()),
            new Claim(JwtTokenIssuer.CompanyClaim, AdminUserSeeder.SeedCompanyId.ToString()));

        var user = await service.GetCurrentAsync();

        Assert.True(user.IsPlatformAdmin);
    }

    [Fact]
    public async Task Administrator_InAnotherTenant_IsNotPlatformAdmin()
    {
        // A customer's own company administrator must never be treated as a Tedwren platform admin.
        var service = ServiceFor(
            new Claim(ClaimTypes.Name, "Customer Admin"),
            new Claim(ClaimTypes.Role, AccessRole.Administrator.ToString()),
            new Claim(JwtTokenIssuer.CompanyClaim, Guid.NewGuid().ToString()));

        var user = await service.GetCurrentAsync();

        Assert.False(user.IsPlatformAdmin);
    }

    [Fact]
    public async Task NonAdministrator_InSeedTenant_IsNotPlatformAdmin()
    {
        var service = ServiceFor(
            new Claim(ClaimTypes.Name, "Site Manager"),
            new Claim(ClaimTypes.Role, AccessRole.SiteManager.ToString()),
            new Claim(JwtTokenIssuer.CompanyClaim, AdminUserSeeder.SeedCompanyId.ToString()));

        var user = await service.GetCurrentAsync();

        Assert.False(user.IsPlatformAdmin);
    }

    [Fact]
    public async Task Anonymous_IsNotPlatformAdmin()
    {
        var service = ServiceFor();

        var user = await service.GetCurrentAsync();

        Assert.False(user.IsPlatformAdmin);
    }
}
