using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Onboarding;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the self-service onboarding endpoints (<c>/api/onboarding</c>): an authorised admin creates a link;
/// the operative uses the token+passcode gated view/submit/cards endpoints anonymously (no account, SF-4).
/// </summary>
public static class OnboardingEndpoints
{
    /// <summary>Registers the <c>/api/onboarding</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/onboarding").WithTags("Onboarding");

        // Admin creates a link (authorised — the fallback policy already requires a signed-in user).
        group.MapPost("/", async (CreateOnboardingLinkRequest request, ClaimsPrincipal user, IOnboardingService service, CancellationToken cancellationToken) =>
            {
                Guid? userId = Guid.TryParse(user.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub), out var uid) ? uid : null;
                return Results.Ok(await service.CreateAsync(request, userId, cancellationToken));
            })
            .WithName("CreateOnboardingLink");

        // Recipient flow — anonymous, token+passcode gated (SF-4, R9).
        var recipient = app.MapGroup("/api/onboarding").WithTags("Onboarding").AllowAnonymous();

        recipient.MapGet("/view", async (string token, string? passcode, IOnboardingService service, CancellationToken cancellationToken) =>
                await service.GetByTokenAsync(token, passcode, cancellationToken) is { } view ? Results.Ok(view) : Results.StatusCode(StatusCodes.Status403Forbidden))
            .WithName("ViewOnboarding");

        recipient.MapPost("/submit", async (string token, string? passcode, SubmitOnboardingDetailsRequest request, IOnboardingService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.SubmitDetailsAsync(token, passcode, request, cancellationToken) is { } view
                        ? Results.Ok(view) : Results.StatusCode(StatusCodes.Status403Forbidden);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("SubmitOnboardingDetails");

        recipient.MapPost("/cards", async (string token, string? passcode, CaptureOnboardingCardRequest request, IOnboardingService service, CancellationToken cancellationToken) =>
                await service.CaptureCardAsync(token, passcode, request, cancellationToken) is { } view
                    ? Results.Ok(view) : Results.StatusCode(StatusCodes.Status403Forbidden))
            .WithName("CaptureOnboardingCard");

        return app;
    }
}
