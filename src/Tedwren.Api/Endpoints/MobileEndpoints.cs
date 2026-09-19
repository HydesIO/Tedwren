using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tedwren.Abstractions.Services;
using Tedwren.Api.Auth;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the authenticated operative (mobile) read surface (<c>/api/mobile/*</c>, M3): the operative's own
/// profile, hours, sites and dashboard. Every handler reads the PersonId + CompanyId from the operative token
/// (never a client-supplied value), so an operative can only ever read their own data (R15). Gated by the
/// <c>RequireOperative</c> policy, so console tokens cannot reach it and operative tokens cannot reach console
/// endpoints (the fallback now requires a console role).
/// </summary>
public static class MobileEndpoints
{
    /// <summary>Registers the <c>/api/mobile</c> operative read group.</summary>
    public static IEndpointRouteBuilder MapMobileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile").WithTags("Mobile").RequireAuthorization("RequireOperative");

        group.MapGet("/me", async (ClaimsPrincipal user, IMobileSurfaceService surface, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out var companyId, out var personId))
                {
                    return Results.Unauthorized();
                }

                var detail = await surface.GetProfileAsync(companyId, personId, cancellationToken);
                return detail is null ? Results.NotFound() : Results.Ok(detail);
            })
            .WithName("MobileMe");

        group.MapGet("/my-hours", async (DateOnly? week, ClaimsPrincipal user, ITimesheetService timesheets, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out var companyId, out var personId))
                {
                    return Results.Unauthorized();
                }

                var weekStart = week ?? CurrentWeekStart();
                return Results.Ok(await timesheets.GetOperativeHoursAsync(companyId, personId, weekStart, cancellationToken));
            })
            .WithName("MobileMyHours");

        group.MapGet("/sites", async (ClaimsPrincipal user, IMobileSurfaceService surface, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out var companyId, out _))
                {
                    return Results.Unauthorized();
                }

                // Scoped to the operative's own company from the token (R15).
                return Results.Ok(await surface.GetSitesAsync(companyId, cancellationToken));
            })
            .WithName("MobileSites");

        group.MapGet("/dashboard", async (ClaimsPrincipal user, IOperativeDashboardService dashboard, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out var companyId, out var personId))
                {
                    return Results.Unauthorized();
                }

                var dto = await dashboard.GetAsync(companyId, personId, cancellationToken);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            })
            .WithName("MobileDashboard");

        return app;
    }

    /// <summary>
    /// Reads the operative's PersonId (sub) and CompanyId (company) from the token. JwtBearer maps the JWT
    /// <c>sub</c> claim to <see cref="ClaimTypes.NameIdentifier"/>, so read that first and fall back to the raw
    /// <c>sub</c> — the same pattern as <c>ClaimsCurrentUserService</c>.
    /// </summary>
    private static bool TryGetOperative(ClaimsPrincipal user, out Guid companyId, out Guid personId)
    {
        companyId = Guid.Empty;
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out personId)
            && Guid.TryParse(user.FindFirstValue(JwtOperativeTokenIssuer.CompanyClaim), out companyId);
    }

    /// <summary>The Monday (UTC date) of the current week — the timesheet week boundary (SUB-8).</summary>
    private static DateOnly CurrentWeekStart()
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        return today.AddDays(-daysSinceMonday);
    }
}
