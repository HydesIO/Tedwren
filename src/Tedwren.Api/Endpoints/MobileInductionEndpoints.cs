using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Api.Auth;
using Tedwren.Application.Inductions;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the operative (mobile) induction surface (<c>/api/mobile/inductions/*</c>, Subcontractor Onboarding spec
/// Stage 4 / Gate 4). The operative resumes or starts their main-contractor induction, completes the steps, takes
/// the server-scored quiz (R5) and finalises with a signature + optional consent (MC-5/MC-20). Gated by
/// <c>RequireOperative</c> (induction is a core MC feature — no module gate, matching the console group). PersonId
/// and CompanyId come from the operative token, never the body, and every action is scoped to the operative's own
/// session (R15) by <see cref="OperativeInductionService"/>.
/// </summary>
public static class MobileInductionEndpoints
{
    /// <summary>Registers the <c>/api/mobile/inductions</c> operative induction group.</summary>
    public static IEndpointRouteBuilder MapMobileInductionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile/inductions").WithTags("Mobile")
            .RequireAuthorization("RequireOperative");

        // The operative's current induction (resumes in-progress/failed, or a valid pass; starts one when due).
        group.MapGet("/current", async (ClaimsPrincipal user, OperativeInductionService inductions, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out var companyId, out var personId, out var name))
                {
                    return Results.Unauthorized();
                }

                var session = await inductions.GetCurrentAsync(companyId, personId, name, cancellationToken);
                return session is null ? Results.NotFound() : Results.Ok(session);
            })
            .WithName("MobileCurrentInduction");

        group.MapPost("/{sessionId:guid}/steps/{stepId}/complete", async (Guid sessionId, string stepId, ClaimsPrincipal user, OperativeInductionService inductions, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out _, out var personId, out _))
                {
                    return Results.Unauthorized();
                }

                var session = await inductions.CompleteStepAsync(personId, sessionId, stepId, cancellationToken);
                return session is null ? Results.NotFound() : Results.Ok(session);
            })
            .WithName("MobileCompleteInductionStep");

        group.MapPost("/{sessionId:guid}/quiz", async (Guid sessionId, SubmitQuizRequest request, ClaimsPrincipal user, OperativeInductionService inductions, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out _, out var personId, out _))
                {
                    return Results.Unauthorized();
                }

                var result = await inductions.SubmitQuizAsync(personId, sessionId, request, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .WithName("MobileSubmitInductionQuiz");

        group.MapPost("/{sessionId:guid}/finalize", async (Guid sessionId, FinalizeInductionRequest request, ClaimsPrincipal user, OperativeInductionService inductions, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out _, out var personId, out _))
                {
                    return Results.Unauthorized();
                }

                try
                {
                    var session = await inductions.FinalizeAsync(personId, sessionId, request, cancellationToken);
                    return session is null ? Results.NotFound() : Results.Ok(session);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { reason = ex.Message });
                }
            })
            .WithName("MobileFinalizeInduction");

        return app;
    }

    /// <summary>Reads the operative's CompanyId (company claim), PersonId (sub) and display name from the token — never the body (R15).</summary>
    private static bool TryGetOperative(ClaimsPrincipal user, out Guid companyId, out Guid personId, out string name)
    {
        companyId = Guid.Empty;
        name = user.FindFirstValue(ClaimTypes.Name) ?? "Operative";
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out personId)
            && Guid.TryParse(user.FindFirstValue(JwtOperativeTokenIssuer.CompanyClaim), out companyId);
    }
}
