using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Services;
using Tedwren.Api.Auth;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the operative (mobile) accreditation surface (<c>/api/mobile/cards/*</c>, Subcontractor Onboarding spec
/// Stage 4 / Gate 3). An operative photographs and uploads their qualification cards / accreditations, which land
/// in the register for a manager to confirm (SF-6 — never auto-accepted); the picker lists the qualification-type
/// library. <c>RequireOperative</c> (core, no module gate); PersonId comes from the operative token, never the body
/// (R15). The write reuses <see cref="IQualificationService.CaptureCardAsync"/> and is idempotent on the request's
/// client id, so the app's at-least-once outbox never duplicates a card.
/// </summary>
public static class MobileCardEndpoints
{
    /// <summary>Registers the <c>/api/mobile/cards</c> operative accreditation group.</summary>
    public static IEndpointRouteBuilder MapMobileCardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile/cards").WithTags("Mobile")
            .RequireAuthorization("RequireOperative");

        // The qualification-type library, for the accreditation-type picker.
        group.MapGet("/types", async (IQualificationService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetQualificationTypesAsync(cancellationToken)))
            .WithName("MobileCardTypes");

        // Capture the operative's own card (needs a manager to confirm, SF-6). Idempotent on the client id.
        group.MapPost("/", async (MobileCaptureCardRequest request, ClaimsPrincipal user, IQualificationService service, CancellationToken cancellationToken) =>
            {
                if (!TryGetOperative(user, out _, out var personId))
                {
                    return Results.Unauthorized();
                }

                var id = await service.CaptureCardAsync(new CaptureCardRequest(
                    personId, request.QualificationTypeId, request.CardNumber, request.HolderName,
                    request.IssuedOn, request.ExpiresOn, NeedsReview: true, ImageReference: request.PhotoReference,
                    CaptureClientId: request.ClientId), cancellationToken);
                return Results.Ok(new { id });
            })
            .WithName("MobileCaptureCard");

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
