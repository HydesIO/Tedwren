namespace Tedwren.Application.Auth;

/// <summary>
/// Issues a signed operative (mobile) access token — a JWT with the operative audience, carrying the person id,
/// tenant company (R15), display name, the bound device id and an "operative" role, distinct from console
/// tokens (<see cref="ITokenIssuer"/>). Implemented in the API layer.
/// </summary>
public interface IOperativeTokenIssuer
{
    /// <summary>Issues a short-lived access token for an operative on a bound device.</summary>
    IssuedToken IssueAccessToken(Guid personId, Guid companyId, string name, string deviceId);
}
