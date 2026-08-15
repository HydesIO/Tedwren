using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Auth;
using Tedwren.Domain.Enums;

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
        return Task.FromResult(new CurrentUserDto(name, role, companyId, IsPlatformAdmin(role, companyId)));
    }

    /// <summary>
    /// A platform administrator is an <see cref="AccessRole.Administrator"/> in the fixed Tedwren tenant
    /// (<see cref="AdminUserSeeder.SeedCompanyId"/>) — a Tedwren operator, not a customer's own company
    /// administrator. This is the authoritative, server-side gate for the admin area.
    /// </summary>
    private static bool IsPlatformAdmin(string role, Guid? companyId) =>
        string.Equals(role, AccessRole.Administrator.ToString(), StringComparison.OrdinalIgnoreCase) &&
        companyId == AdminUserSeeder.SeedCompanyId;
}
