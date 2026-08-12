using Tedwren.Abstractions.Contracts.Auth;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the anonymous authentication endpoints (<c>/api/auth</c>): sign in and accept an invitation. These
/// must stay anonymous — they are how a caller obtains a token in the first place.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>Registers the <c>/api/auth</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        group.MapPost("/login", async (LoginRequest request, IAuthService service, CancellationToken cancellationToken) =>
            {
                var result = await service.LoginAsync(request, cancellationToken);
                return result is null ? Results.Unauthorized() : Results.Ok(result);
            })
            .WithName("Login");

        group.MapPost("/accept-invite", async (AcceptInviteRequest request, IAuthService service, CancellationToken cancellationToken) =>
            {
                var result = await service.AcceptInviteAsync(request, cancellationToken);
                return result is null ? Results.BadRequest(new { error = "The invitation link is invalid or has expired." }) : Results.Ok(result);
            })
            .WithName("AcceptInvite");

        // Always returns 200 regardless of whether the email is registered — the service reveals nothing about
        // account existence (no enumeration). The reset link, when sent, lands on /reset-password (D1).
        group.MapPost("/forgot-password", async (ForgotPasswordRequest request, IAuthService service, CancellationToken cancellationToken) =>
            {
                await service.RequestPasswordResetAsync(request, cancellationToken);
                return Results.Ok();
            })
            .WithName("ForgotPassword");

        return app;
    }
}
