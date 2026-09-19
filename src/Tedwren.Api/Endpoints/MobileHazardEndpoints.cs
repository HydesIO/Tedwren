using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Services;
using Tedwren.Api.Auth;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the operative (mobile) hazard / near-miss endpoint (<c>/api/mobile/hazards</c>, M5), reusing the HSE hazard
/// domain (PRD §8.2). Gated by <c>RequireOperative</c> <b>and</b> the <c>hse</c> module (fail-closed 403 without it,
/// Q2), so it is available only to operatives at companies that hold HSE. The reporter's name + CompanyId come from
/// the operative token (never the body, R15). Idempotent on the device client id (R4/R16).
/// </summary>
public static class MobileHazardEndpoints
{
    /// <summary>Registers the <c>/api/mobile/hazards</c> operative hazard group.</summary>
    public static IEndpointRouteBuilder MapMobileHazardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile/hazards").WithTags("Mobile")
            .RequireAuthorization("RequireOperative")
            .AddEndpointFilter(ModuleGate.Require("hse"));

        group.MapPost("", async (MobileReportHazardRequest request, ClaimsPrincipal user, IMobileHazardService hazards, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out var companyId, out var reportedBy))
                {
                    return Results.Unauthorized();
                }

                try
                {
                    return Results.Ok(await hazards.ReportAsync(companyId, reportedBy, request, cancellationToken));
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (InvalidOperationException)
                {
                    return Results.Conflict(); // client id already belongs to another company (impossible collision, R15)
                }
            })
            .WithName("MobileReportHazard");

        return app;
    }

    /// <summary>Reads the operative's CompanyId (company claim) + display name (name claim) from the token — never the body (R15).</summary>
    private static bool TryGetOperative(ClaimsPrincipal user, out Guid companyId, out string reportedBy)
    {
        companyId = Guid.Empty;
        reportedBy = user.FindFirstValue(ClaimTypes.Name) ?? "Operative";
        return Guid.TryParse(user.FindFirstValue(JwtOperativeTokenIssuer.CompanyClaim), out companyId);
    }
}
