using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Rams;
using Tedwren.Api.Auth;
using Tedwren.Application.Rams;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the operative (mobile) RAMS surface (<c>/api/mobile/rams/*</c>, Subcontractor Onboarding spec Gate 5). An
/// operative reads the current live, approved RAMS for their subcontractor and signs it before they can start on
/// site (MC-8). Gated by <c>RequireOperative</c> (RAMS is part of the "hse" module the manager decision already
/// gates on — the operative surface itself is core). PersonId comes from the operative token, never the body (R15),
/// and both actions delegate to the shared <see cref="RamsGate"/> so this path never diverges from the manager's
/// site-entry decision or the operative's own sign-in gate.
/// </summary>
public static class MobileRamsEndpoints
{
    /// <summary>Registers the <c>/api/mobile/rams</c> operative RAMS group.</summary>
    public static IEndpointRouteBuilder MapMobileRamsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile/rams").WithTags("Mobile")
            .RequireAuthorization("RequireOperative");

        // The current live, approved RAMS the operative must read and sign (null when none applies to this worker).
        group.MapGet("/live", async (ClaimsPrincipal user, RamsGate gate, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out _, out var personId))
                {
                    return Results.Unauthorized();
                }

                var live = await gate.GetLiveForOperativeAsync(personId, cancellationToken);
                return live is null ? Results.NoContent() : Results.Ok(live);
            })
            .WithName("MobileLiveRams");

        // Record the operative's signature of the live RAMS version (Gate 5). 409 when the submission is no longer
        // the current live approved version, so the app re-reads /live rather than signing a stale document.
        group.MapPost("/sign", async (SignRamsRequest request, ClaimsPrincipal user, RamsGate gate, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out _, out var personId))
                {
                    return Results.Unauthorized();
                }

                var ack = await gate.AcknowledgeAsync(personId, request, cancellationToken);
                return ack is null ? Results.Conflict() : Results.Ok(ack);
            })
            .WithName("MobileSignRams");

        return app;
    }

    /// <summary>Reads the operative's CompanyId (company claim) and PersonId (sub) from the token — never the body (R15).</summary>
    private static bool TryGetOperative(ClaimsPrincipal user, out Guid companyId, out Guid personId)
    {
        companyId = Guid.Empty;
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out personId)
            && Guid.TryParse(user.FindFirstValue(JwtOperativeTokenIssuer.CompanyClaim), out companyId);
    }
}
