using Tedwren.Abstractions.Contracts.Affiliates;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the affiliate endpoints: platform-admin management of affiliates and payouts under
/// <c>/api/affiliates</c>, and the anonymous, token-gated agreement view/sign/PDF under
/// <c>/api/affiliate-agreements</c> (an affiliate signs without an account, R9).
/// </summary>
public static class AffiliateEndpoints
{
    /// <summary>Registers the affiliate endpoint groups.</summary>
    public static IEndpointRouteBuilder MapAffiliateEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/affiliates").WithTags("Affiliates").RequireAuthorization("PlatformAdmin");

        admin.MapGet("/", async (IAffiliateService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListAsync(cancellationToken)))
            .WithName("ListAffiliates");

        admin.MapGet("/{id:guid}", async (Guid id, IAffiliateService service, CancellationToken cancellationToken) =>
                await service.GetAsync(id, cancellationToken) is { } detail ? Results.Ok(detail) : Results.NotFound())
            .WithName("GetAffiliate");

        admin.MapPost("/", async (CreateAffiliateRequest request, IAffiliateService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return Results.Ok(await service.CreateAsync(request, cancellationToken));
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CreateAffiliate");

        admin.MapPut("/{id:guid}", async (Guid id, UpdateAffiliateRequest request, IAffiliateService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.UpdateAsync(id, request, cancellationToken) is { } aff ? Results.Ok(aff) : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateAffiliate");

        admin.MapPost("/{id:guid}/payouts", async (Guid id, CreatePayoutRequest request, IAffiliateService service, CancellationToken cancellationToken) =>
                await service.CreatePayoutAsync(id, request, cancellationToken) is { } payout ? Results.Ok(payout) : Results.NotFound())
            .WithName("CreateAffiliatePayout");

        admin.MapPost("/payouts/{payoutId:guid}/paid", async (Guid payoutId, IAffiliateService service, CancellationToken cancellationToken) =>
                await service.MarkPayoutPaidAsync(payoutId, cancellationToken) is { } payout ? Results.Ok(payout) : Results.NotFound())
            .WithName("MarkAffiliatePayoutPaid");

        admin.MapPost("/{id:guid}/resend-agreement", async (Guid id, IAffiliateService service, CancellationToken cancellationToken) =>
                await service.ResendAgreementAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound())
            .WithName("ResendAffiliateAgreement");

        // Anonymous, token-gated agreement flow (the affiliate signs without an account, R9), rate-limited.
        var agreement = app.MapGroup("/api/affiliate-agreements").WithTags("Affiliates").AllowAnonymous().RequireRateLimiting("public");

        agreement.MapGet("/{token}", async (string token, IAffiliateService service, CancellationToken cancellationToken) =>
                await service.GetAgreementAsync(token, cancellationToken) is { } view ? Results.Ok(view) : Results.NotFound())
            .WithName("GetAffiliateAgreement");

        agreement.MapPost("/{token}/sign", async (string token, SignAffiliateAgreementRequest request, IAffiliateService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.SignAgreementAsync(token, request, cancellationToken) is { } view
                        ? Results.Ok(view) : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("SignAffiliateAgreement");

        agreement.MapGet("/{token}/pdf", async (string token, IAffiliateService service, CancellationToken cancellationToken) =>
                await service.GetAgreementPdfAsync(token, cancellationToken) is { } pdf
                    ? Results.File(pdf, "application/pdf", "affiliate-agreement.pdf")
                    : Results.NotFound())
            .WithName("GetAffiliateAgreementPdf");

        return app;
    }
}
