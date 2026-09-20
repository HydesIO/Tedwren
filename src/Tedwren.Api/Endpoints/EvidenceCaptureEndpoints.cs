using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the manager evidence-review endpoints (<c>/api/evidence-captures</c>, M7): read-only access to the field
/// evidence operatives captured on the mobile app (M5). Authenticated console users only (the secure-by-default
/// fallback policy — an operative token carries no console role and is refused); every read is scoped to the
/// caller's company via <see cref="ICurrentUserService"/> (R15). Photos are fetched separately through the
/// authorised <c>GET /api/images/{id}</c> route (R9).
/// </summary>
public static class EvidenceCaptureEndpoints
{
    /// <summary>Registers the <c>/api/evidence-captures</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapEvidenceCaptureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/evidence-captures").WithTags("Evidence");

        group.MapGet("/", async (Guid? personId, ICurrentUserService currentUser, IEvidenceCaptureQueryService service, CancellationToken cancellationToken) =>
            {
                var me = await currentUser.GetCurrentAsync(cancellationToken);
                return me.CompanyId is { } companyId
                    ? Results.Ok(await service.GetForCompanyAsync(companyId, personId, cancellationToken))
                    : Results.Forbid();
            })
            .WithName("GetEvidenceCaptures");

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUserService currentUser, IEvidenceCaptureQueryService service, CancellationToken cancellationToken) =>
            {
                var me = await currentUser.GetCurrentAsync(cancellationToken);
                if (me.CompanyId is not { } companyId)
                {
                    return Results.Forbid();
                }

                return await service.GetAsync(companyId, id, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound();
            })
            .WithName("GetEvidenceCapture");

        return app;
    }
}
