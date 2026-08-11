using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Auth;

/// <summary>
/// Resolves the current console operator from the authenticated request's claims (replaces the former config
/// stub). Returns the real signed-in name, role and tenant company id (R15). Anonymous requests yield an
/// empty read-only identity.
/// </summary>
public sealed class ClaimsCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    /// <summary>Creates the service over the HTTP context accessor.</summary>
    public ClaimsCurrentUserService(IHttpContextAccessor http) => _http = http;

    /// <summary>Returns the current operator's identity and role from the request claims.</summary>
    public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var principal = _http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult(new CurrentUserDto("Guest", "Auditor", CompanyId: null));
        }

        var name = principal.FindFirstValue(ClaimTypes.Name) ?? "User";
        var role = principal.FindFirstValue(ClaimTypes.Role) ?? "Auditor";
        Guid? companyId = Guid.TryParse(principal.FindFirstValue(JwtTokenIssuer.CompanyClaim), out var cid) ? cid : null;
        return Task.FromResult(new CurrentUserDto(name, role, companyId));
    }
}
