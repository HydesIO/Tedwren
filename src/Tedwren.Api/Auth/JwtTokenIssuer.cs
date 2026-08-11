using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Tedwren.Abstractions.Configuration;
using Tedwren.Application.Auth;
using Tedwren.Domain.Entities;

namespace Tedwren.Api.Auth;

/// <summary>Issues signed JWT bearer tokens carrying the user's id, company, role and name (D1).</summary>
public sealed class JwtTokenIssuer : ITokenIssuer
{
    /// <summary>Claim type carrying the user's tenant company id (R15).</summary>
    public const string CompanyClaim = "company";

    private readonly JwtOptions _options;

    /// <summary>Creates the issuer over the JWT options.</summary>
    public JwtTokenIssuer(JwtOptions options) => _options = options;

    /// <summary>Issues a token for the user, valid for the configured lifetime.</summary>
    public IssuedToken Issue(User user)
    {
        var expiresUtc = DateTimeOffset.UtcNow.AddMinutes(_options.LifetimeMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(CompanyClaim, user.CompanyId.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresUtc.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresUtc);
    }
}
