using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Services;
using Tedwren.Api.Auth;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the authenticated operative (mobile) attendance actions (<c>/api/mobile/attendance/*</c>, M4): sign in,
/// sign out and the operative's current on-site state. The PersonId is always read from the operative token,
/// never a client-supplied value, and each site is guarded against the token's company before the shared
/// <see cref="IAttendanceService"/> runs (R15) — the service itself is person+site only. Attendance is
/// <b>online-only and never queued</b> (R2/R3); the browser sign-in path remains for everyone (R1). Gated by the
/// <c>RequireOperative</c> policy, so console tokens cannot reach it.
/// </summary>
public static class MobileAttendanceEndpoints
{
    /// <summary>Registers the <c>/api/mobile/attendance</c> operative attendance group.</summary>
    public static IEndpointRouteBuilder MapMobileAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile/attendance").WithTags("Mobile").RequireAuthorization("RequireOperative");

        group.MapPost("/sign-in", async (MobileSignInRequest request, ClaimsPrincipal user, ISiteService sites, IAttendanceService attendance, CancellationToken cancellationToken) =>
            {
                if (!TryGetPerson(user, out var personId))
                {
                    return Results.Unauthorized();
                }

                // R15: reuse the tenant-scoped site read (scoped to the token's company via ICurrentUserService) —
                // a cross-company or unknown site resolves to null and is refused before any record is written.
                if (await sites.GetSiteAsync(request.SiteId.ToString(), cancellationToken) is null)
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                var result = await attendance.SignInAsync(
                    new SignInRequest(personId, request.SiteId, request.PropertyId, request.Latitude, request.Longitude, SignInMethod.AssignmentLink),
                    cancellationToken);

                // Every attempt is recorded, including refusals (SF-16), so the outcome rides in the 200 body.
                return Results.Ok(result);
            })
            .WithName("MobileAttendanceSignIn");

        group.MapPost("/sign-out", async (MobileSignOutRequest request, ClaimsPrincipal user, ISiteService sites, IAttendanceService attendance, CancellationToken cancellationToken) =>
            {
                if (!TryGetPerson(user, out var personId))
                {
                    return Results.Unauthorized();
                }

                if (await sites.GetSiteAsync(request.SiteId.ToString(), cancellationToken) is null)
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                var result = await attendance.SignOutAsync(
                    new SignOutRequest(personId, request.SiteId, request.Latitude, request.Longitude, SignInMethod.AssignmentLink),
                    cancellationToken);

                return Results.Ok(result);
            })
            .WithName("MobileAttendanceSignOut");

        group.MapGet("/current", async (ClaimsPrincipal user, IMobileAttendanceService attendance, CancellationToken cancellationToken) =>
            {
                if (!TryGetPerson(user, out var personId))
                {
                    return Results.Unauthorized();
                }

                // 204 when the operative is not signed in anywhere (SF-18) — the client maps it to "no current".
                var current = await attendance.GetCurrentAsync(personId, cancellationToken);
                return current is null ? Results.NoContent() : Results.Ok(current);
            })
            .WithName("MobileAttendanceCurrent");

        return app;
    }

    /// <summary>
    /// Reads the operative's PersonId (sub) from the token. JwtBearer maps the JWT <c>sub</c> claim to
    /// <see cref="ClaimTypes.NameIdentifier"/>, so read that first and fall back to the raw <c>sub</c> — the same
    /// pattern as <c>MobileEndpoints</c> and <c>ClaimsCurrentUserService</c>.
    /// </summary>
    private static bool TryGetPerson(ClaimsPrincipal user, out Guid personId)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out personId);
    }
}
