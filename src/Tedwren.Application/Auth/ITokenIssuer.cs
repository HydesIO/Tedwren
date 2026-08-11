using Tedwren.Domain.Entities;

namespace Tedwren.Application.Auth;

/// <summary>Issues a signed bearer token for a user. Implemented in the API layer (JWT).</summary>
public interface ITokenIssuer
{
    /// <summary>Issues a token carrying the user's id, company, role and name.</summary>
    IssuedToken Issue(User user);
}

/// <summary>A signed token and its expiry.</summary>
public sealed record IssuedToken(string Token, DateTimeOffset ExpiresUtc);
