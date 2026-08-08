using Tedwren.Abstractions.Contracts.Decisions;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the site-entry decision-record HTTP endpoints (R10). The store is introduced here (read + record);
/// the main-contractor gate (Phase 17) records real five-check decisions through it. Records are
/// reconstructable and never edited.
/// </summary>
public static class DecisionEndpoints
{
    /// <summary>Registers the <c>/api/decisions</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapDecisionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/decisions").WithTags("Decisions");

        group.MapGet("/people/{personId:guid}", async (Guid personId, IDecisionService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetForPersonAsync(personId, cancellationToken)))
            .WithName("GetPersonDecisions");

        group.MapGet("/sites/{siteId:guid}", async (Guid siteId, IDecisionService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetForSiteAsync(siteId, cancellationToken)))
            .WithName("GetSiteDecisions");

        group.MapPost("/", async (RecordDecisionRequest request, IDecisionService service, CancellationToken cancellationToken) =>
            {
                var id = await service.RecordAsync(request, cancellationToken);
                return Results.Ok(id);
            })
            .WithName("RecordDecision");

        return app;
    }
}
