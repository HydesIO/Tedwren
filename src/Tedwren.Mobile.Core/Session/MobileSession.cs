namespace Tedwren.Mobile.Core.Session;

/// <summary>
/// An authenticated app session: the bearer token, when it expires, and the signed-in identity. Built from the
/// API's auth result (console) or the operative auth result (mobile). Held in memory; the durable refresh token
/// lives in encrypted secure storage, not here.
/// </summary>
public sealed record MobileSession(
    string Token,
    DateTimeOffset ExpiresUtc,
    string Name,
    string Role,
    Guid CompanyId)
{
    /// <summary>The home experience this session lands on (operative vs manager), derived from <see cref="Role"/>.</summary>
    public RoleHome Home => RoleHomeResolver.Resolve(Role);

    /// <summary>Whether the access token has expired as of <paramref name="asOfUtc"/> (no clock skew applied here).</summary>
    public bool IsExpired(DateTimeOffset asOfUtc) => asOfUtc >= ExpiresUtc;
}
