using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Trades;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the trade self-service onboarding endpoints (<c>/api/trades</c>, UAT-023). An authorised manager invites
/// a trade and reviews submissions (approve / reject / return); the trade uses the token+passcode gated
/// view/upload/submit endpoints anonymously (no console account, SUB-4/MC-27). Reviews are tenant-scoped (R15).
/// </summary>
public static class TradeOnboardingEndpoints
{
    /// <summary>Registers the <c>/api/trades</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapTradeOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/trades").WithTags("Trades");

        // Admin invites a trade (creates the trade company + link). Writers only (Auditor excluded).
        group.MapPost("/invites", async (CreateTradeInviteRequest request, ClaimsPrincipal user, ITradeOnboardingService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    Guid? userId = Guid.TryParse(user.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub), out var uid) ? uid : null;
                    return Results.Ok(await service.InviteTradeAsync(request, userId, cancellationToken));
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("InviteTrade").RequireAuthorization("RequireWrite");

        // The review queue for the caller's tenant (R15).
        group.MapGet("/reviews", async (ITradeOnboardingService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetReviewQueueAsync(cancellationToken)))
            .WithName("GetTradeReviews").RequireAuthorization("RequireWrite");

        group.MapPost("/reviews/{inviteId:guid}/approve", (Guid inviteId, ReviewTradeRequest request, ITradeOnboardingService service, CancellationToken cancellationToken) =>
                DecideAsync(() => service.ApproveAsync(inviteId, request, cancellationToken)))
            .WithName("ApproveTrade").RequireAuthorization("RequireWrite");

        group.MapPost("/reviews/{inviteId:guid}/reject", (Guid inviteId, ReviewTradeRequest request, ITradeOnboardingService service, CancellationToken cancellationToken) =>
                DecideAsync(() => service.RejectAsync(inviteId, request, cancellationToken)))
            .WithName("RejectTrade").RequireAuthorization("RequireWrite");

        group.MapPost("/reviews/{inviteId:guid}/return", (Guid inviteId, ReviewTradeRequest request, ITradeOnboardingService service, CancellationToken cancellationToken) =>
                DecideAsync(() => service.ReturnAsync(inviteId, request, cancellationToken)))
            .WithName("ReturnTrade").RequireAuthorization("RequireWrite");

        // Recipient flow — anonymous, token+passcode gated (SUB-4, R9).
        group.MapGet("/by-link/{token}", async (string token, string? passcode, ITradeOnboardingService service, CancellationToken cancellationToken) =>
                await service.GetByTokenAsync(token, passcode, cancellationToken) is { } view ? Results.Ok(view) : Results.StatusCode(StatusCodes.Status403Forbidden))
            .WithName("ViewTradeInvite").AllowAnonymous();

        group.MapPost("/by-link/{token}/documents", async (string token, string? passcode, SubmitTradeDocumentRequest request, ITradeOnboardingService service, CancellationToken cancellationToken) =>
                await service.SubmitDocumentAsync(token, passcode, request, cancellationToken) is { } view ? Results.Ok(view) : Results.StatusCode(StatusCodes.Status403Forbidden))
            .WithName("SubmitTradeDocument").AllowAnonymous();

        group.MapPost("/by-link/{token}/submit", async (string token, string? passcode, ITradeOnboardingService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.SubmitForReviewAsync(token, passcode, cancellationToken) is { } view
                        ? Results.Ok(view) : Results.StatusCode(StatusCodes.Status403Forbidden);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { reason = ex.Message });
                }
            })
            .WithName("SubmitTradeForReview").AllowAnonymous();

        return app;
    }

    /// <summary>Shared handling for the three review decisions: 404 when not found/out of scope, 400 on a missing note, 409 when not reviewable.</summary>
    private static async Task<IResult> DecideAsync(Func<Task<TradeReviewItemDto?>> decide)
    {
        try
        {
            return await decide() is { } item ? Results.Ok(item) : Results.NotFound();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { reason = ex.Message });
        }
    }
}
