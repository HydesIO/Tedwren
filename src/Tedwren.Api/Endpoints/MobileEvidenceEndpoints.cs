using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Services;
using Tedwren.Api.Auth;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the operative (mobile) field-evidence endpoint (<c>/api/mobile/evidence</c>, M5): an offline-captured photo
/// + note + location, synced when the device is back online. PersonId + CompanyId are read from the operative token
/// (never the body, R15). <b>Ungated</b> — available to every operative. Idempotent on the device client id (R4/R16).
/// </summary>
public static class MobileEvidenceEndpoints
{
    /// <summary>Registers the <c>/api/mobile/evidence</c> operative evidence group.</summary>
    public static IEndpointRouteBuilder MapMobileEvidenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile/evidence").WithTags("Mobile").RequireAuthorization("RequireOperative");

        group.MapPost("", async (MobileReportEvidenceRequest request, ClaimsPrincipal user, IMobileEvidenceService evidence, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out var companyId, out var personId))
                {
                    return Results.Unauthorized();
                }

                try
                {
                    return Results.Ok(await evidence.ReportAsync(companyId, personId, request, cancellationToken));
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
            .WithName("MobileReportEvidence");

        return app;
    }

    /// <summary>Reads the operative's PersonId (sub) + CompanyId (company claim) from the token — never the body (R15).</summary>
    private static bool TryGetOperative(ClaimsPrincipal user, out Guid companyId, out Guid personId)
    {
        companyId = Guid.Empty;
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out personId)
            && Guid.TryParse(user.FindFirstValue(JwtOperativeTokenIssuer.CompanyClaim), out companyId);
    }
}
