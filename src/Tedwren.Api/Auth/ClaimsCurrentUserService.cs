using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Account;
using Tedwren.Application.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Enums;

namespace Tedwren.Api.Auth;

/// <summary>
/// Resolves the current console operator from the authenticated request's claims (replaces the former config
/// stub). Returns the real signed-in name, role, tenant company id (R15), own user id and avatar URL. Anonymous
/// requests yield an empty read-only identity.
/// </summary>
public sealed class ClaimsCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    private readonly IUserRepository _users;

    /// <summary>Creates the service over the HTTP context accessor and user repository (for the avatar URL).</summary>
    public ClaimsCurrentUserService(IHttpContextAccessor http, IUserRepository users)
    {
        _http = http;
        _users = users;
    }

    /// <summary>Returns the current operator's identity, role, own id and avatar from the request claims.</summary>
    public async Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var principal = _http.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return new CurrentUserDto("Guest", "Auditor", CompanyId: null);
        }

        var name = principal.FindFirstValue(ClaimTypes.Name) ?? "User";
        var role = principal.FindFirstValue(ClaimTypes.Role) ?? "Auditor";
        Guid? companyId = Guid.TryParse(principal.FindFirstValue(JwtTokenIssuer.CompanyClaim), out var cid) ? cid : null;

        // The user id is the JWT subject. The default JWT bearer handler maps the inbound "sub" claim to
        // ClaimTypes.NameIdentifier, so read that first and fall back to the raw "sub". Resolving the avatar
        // from the stored record keeps the top-bar avatar current without a second call or a re-issued token.
        Guid? userId =
            Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid :
            Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var sub) ? sub : null;
        string? avatarUrl = null;
        if (userId is { } id)
        {
            var user = await _users.GetByIdAsync(id, cancellationToken);
            avatarUrl = ProfileService.AvatarUrl(user?.AvatarImageReference);
        }

        return new CurrentUserDto(name, role, companyId, IsPlatformAdmin(role, companyId), avatarUrl, userId);
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
