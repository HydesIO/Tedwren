using Tedwren.Abstractions.Contracts.Subcontractors;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the subcontractor onboarding endpoints (<c>/api/subcontractor-onboarding</c>): the main-contractor
/// set-up & configuration flow (Subcontractor Onboarding spec Stage 1 / §4). Creating a subcontractor needs
/// console write access (<c>RequireWrite</c>); reads are available to any authenticated console user and scoped
/// to the caller's tenant inside the service (R15).
/// </summary>
public static class SubcontractorOnboardingEndpoints
{
    /// <summary>Registers the <c>/api/subcontractor-onboarding</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapSubcontractorOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subcontractor-onboarding").WithTags("SubcontractorOnboarding");

        group.MapPost("/", async (SetupSubcontractorRequest request, ISubcontractorOnboardingService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var result = await service.SetupAsync(request, cancellationToken);
                    return Results.Created($"/api/subcontractor-onboarding/{result.SubcontractorCompanyId}", result);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            })
            .WithName("SetupSubcontractor").RequireAuthorization("RequireWrite");

        group.MapGet("/{subcontractorCompanyId:guid}", async (Guid subcontractorCompanyId, ISubcontractorOnboardingService service, CancellationToken cancellationToken) =>
            {
                var config = await service.GetBySubcontractorAsync(subcontractorCompanyId, cancellationToken);
                return config is null ? Results.NotFound() : Results.Ok(config);
            })
            .WithName("GetSubcontractorConfig");

        // Gate 1 status for a subcontractor — whether every "required before work" document is present & valid (spec §2).
        group.MapGet("/{subcontractorCompanyId:guid}/gate1", async (Guid subcontractorCompanyId, ISubcontractorOnboardingService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.EvaluateGate1Async(subcontractorCompanyId, cancellationToken)))
            .WithName("EvaluateSubcontractorGate1");

        // Subcontractors whose live RAMS is due for re-review under its configured cycle (spec §4; beyond PRD v6.4).
        group.MapGet("/rams-review-due", async (DateTimeOffset? asOf, ISubcontractorOnboardingService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetSubcontractorsDueForRamsReviewAsync(asOf ?? DateTimeOffset.UtcNow, cancellationToken)))
            .WithName("SubcontractorsDueForRamsReview");

        return app;
    }
}
