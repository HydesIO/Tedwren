using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the unified compliance evidence export endpoints (<c>/api/evidence</c>, PRD §8.2). The whole group is part
/// of the paid HSE module, so it is entitlement-gated server-side and fails closed (Q2); the caller's company is
/// resolved from the request claims (R15), never trusted from the client.
/// </summary>
public static class EvidenceEndpoints
{
    /// <summary>Registers the <c>/api/evidence</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapEvidenceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/evidence").WithTags("Evidence").AddEndpointFilter(ModuleGate.Require("hse"));

        group.MapGet("/summary", async (ICurrentUserService currentUser, IEvidenceExportService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return Results.Ok(await service.GetSummaryAsync(companyId, cancellationToken));
            })
            .WithName("EvidenceSummary");

        group.MapGet("/export", async (ICurrentUserService currentUser, IEvidenceExportService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                var file = await service.BuildZipAsync(companyId, cancellationToken);
                return Results.File(file.Content, file.ContentType, file.FileName);
            })
            .WithName("EvidenceExport");

        return app;
    }
}
