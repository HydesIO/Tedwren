using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Tedwren.Domain.Enums;

namespace Tedwren.Api.Auth;

/// <summary>
/// TEST-ONLY authentication handler that authenticates every request as an Administrator of the seed company.
/// Selected exclusively by the API test host (<c>Auth:TestBypass=true</c>) so the end-to-end tests exercise
/// the authorised endpoints without minting real JWTs. Never enabled in a real deployment (default false),
/// mirroring the <c>DataSource:Mode=InMemory</c> test hook.
/// </summary>
public sealed class TestBypassAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>The scheme name.</summary>
    public const string SchemeName = "TestBypass";

    /// <summary>The seed company the fabricated identity is scoped to (matches the client's default tenant).</summary>
    private static readonly Guid SeedCompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    /// <summary>
    /// A fixed user id for the fabricated identity, so self-service endpoints (e.g. <c>/api/me/profile</c>) can
    /// resolve "the caller" deterministically in tests. A test that exercises those endpoints seeds a matching
    /// user with this id; every other test simply ignores it.
    /// </summary>
    public static readonly Guid OperatorId = Guid.Parse("33333333-3333-4333-8333-000000000001");

    /// <summary>Creates the handler.</summary>
    public TestBypassAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>Always succeeds with a fabricated Administrator identity.</summary>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, OperatorId.ToString()),
            new Claim(ClaimTypes.Name, "Test Administrator"),
            new Claim(ClaimTypes.Role, AccessRole.Administrator.ToString()),
            new Claim(JwtTokenIssuer.CompanyClaim, SeedCompanyId.ToString()),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
