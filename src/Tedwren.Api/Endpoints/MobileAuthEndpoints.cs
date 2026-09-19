using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the anonymous operative (mobile) authentication endpoints (<c>/api/mobile/auth</c>): request a one-time
/// code, verify it (binding this device), and refresh. These must stay anonymous — they are how an operative
/// obtains a token in the first place — and are rate-limited per client IP (the kiosk policy) so the code/refresh
/// surface can't be brute-forced.
/// </summary>
public static class MobileAuthEndpoints
{
    /// <summary>Registers the <c>/api/mobile/auth</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapMobileAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mobile/auth").WithTags("MobileAuth").AllowAnonymous().RequireRateLimiting("kiosk");

        // Always 200: the response reveals nothing about whether the number is a known operative (no enumeration).
        group.MapPost("/request-otp", async (RequestOtpRequest request, IOperativeAuthService service, CancellationToken cancellationToken) =>
            {
                await service.RequestOtpAsync(request, cancellationToken);
                return Results.Ok();
            })
            .WithName("RequestOperativeOtp");

        group.MapPost("/verify-otp", async (VerifyOtpRequest request, IOperativeAuthService service, CancellationToken cancellationToken) =>
            {
                var outcome = await service.VerifyOtpAsync(request, cancellationToken);
                return outcome.Status switch
                {
                    OperativeAuthStatus.Success => Results.Ok(outcome.Result),
                    OperativeAuthStatus.DeviceConflict => Results.Content(outcome.Message, "text/plain", null, StatusCodes.Status409Conflict),
                    _ => Results.Content(outcome.Message, "text/plain", null, StatusCodes.Status401Unauthorized),
                };
            })
            .WithName("VerifyOperativeOtp");

        group.MapPost("/refresh", async (RefreshTokenRequest request, IOperativeAuthService service, CancellationToken cancellationToken) =>
            {
                var outcome = await service.RefreshAsync(request, cancellationToken);
                return outcome.Status == OperativeAuthStatus.Success
                    ? Results.Ok(outcome.Result)
                    : Results.Content(outcome.Message, "text/plain", null, StatusCodes.Status401Unauthorized);
            })
            .WithName("RefreshOperativeToken");

        return app;
    }
}
