using Tedwren.Abstractions.Contracts.Audit;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the audit-trail HTTP endpoints (SF-20). Search covers free text (actor/action/entity/reference)
/// and a date range; a companion route exports the same matches as a CSV download. Reads are scoped to the
/// supplied company (R15).
/// </summary>
public static class AuditEndpoints
{
    /// <summary>Registers the <c>/api/audit</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit").WithTags("Audit");

        group.MapGet("/", async (Guid? companyId, string? text, DateOnly? from, DateOnly? to,
                IAuditService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.SearchAsync(companyId, text, from, to, cancellationToken)))
            .WithName("SearchAudit");

        group.MapGet("/export", async (Guid? companyId, string? text, DateOnly? from, DateOnly? to,
                IAuditService service, CancellationToken cancellationToken) =>
            {
                var csv = await service.ExportCsvAsync(companyId, text, from, to, cancellationToken);
                return Results.File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "audit-log.csv");
            })
            .WithName("ExportAudit");

        group.MapPost("/", async (RecordAuditRequest request, IAuditService service, CancellationToken cancellationToken) =>
            {
                await service.RecordAsync(request, cancellationToken);
                return Results.NoContent();
            })
            .WithName("RecordAudit");

        return app;
    }
}
