using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Auth;

namespace Tedwren.Api.Auth;

/// <summary>
/// Issues signed operative (mobile) JWT access tokens (M2): audience <see cref="JwtOptions.MobileAudience"/>,
/// subject = person id, carrying the tenant company (R15), the bound device id and an "Operative" role — so they
/// are distinct from console tokens and cannot satisfy console authorization policies (RequireWrite / AdminOnly).
/// </summary>
public sealed class JwtOperativeTokenIssuer : IOperativeTokenIssuer
{
    /// <summary>Claim type carrying the operative's tenant company id (R15) — same claim name as the console issuer.</summary>
    public const string CompanyClaim = JwtTokenIssuer.CompanyClaim;

    /// <summary>Claim type carrying the bound device id.</summary>
    public const string DeviceClaim = "device_id";

    /// <summary>The role name carried by operative tokens (used by the RequireOperative policy in M3).</summary>
    public const string OperativeRole = "Operative";

    private readonly JwtOptions _options;

    /// <summary>Creates the issuer over the JWT options.</summary>
    public JwtOperativeTokenIssuer(JwtOptions options) => _options = options;

    /// <summary>Issues a short-lived operative access token for a bound device.</summary>
    public IssuedToken IssueAccessToken(Guid personId, Guid companyId, string name, string deviceId)
    {
        var expiresUtc = DateTimeOffset.UtcNow.AddMinutes(_options.MobileLifetimeMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, personId.ToString()),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Role, OperativeRole),
            new Claim(CompanyClaim, companyId.ToString()),
            new Claim(DeviceClaim, deviceId),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.MobileAudience,
            claims: claims,
            expires: expiresUtc.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresUtc);
    }
}
