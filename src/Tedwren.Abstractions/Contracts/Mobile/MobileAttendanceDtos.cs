namespace Tedwren.Abstractions.Contracts.Mobile;

/// <summary>
/// An operative's sign-in request from the app (M4). Carries no <c>PersonId</c>: the person is taken from the
/// operative token, never the body (R15), and the site is guarded against the token's company before the
/// attendance service runs. Location is optional; the site's policy decides what happens without it (SF-15).
/// </summary>
public sealed record MobileSignInRequest(
    Guid SiteId,
    Guid? PropertyId,
    double? Latitude,
    double? Longitude);

/// <summary>
/// An operative's sign-out request from the app (M4). As with <see cref="MobileSignInRequest"/>, the person is
/// taken from the token, never the body (R15). No property id — sign-out is per site (SF-17).
/// </summary>
public sealed record MobileSignOutRequest(
    Guid SiteId,
    double? Latitude,
    double? Longitude);

/// <summary>
/// The operative's current open sign-in, if any (M4) — the site they are recorded present at and since when
/// (UTC, displayed UK-local per R11). Null when the operative is not currently signed in anywhere (SF-18).
/// </summary>
public sealed record CurrentAttendanceDto(
    Guid SiteId,
    string SiteName,
    Guid? PropertyId,
    DateTimeOffset SinceUtc);
