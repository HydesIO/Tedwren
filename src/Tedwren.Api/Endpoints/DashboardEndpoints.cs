using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the dashboard endpoints (<c>/api/dashboard</c>): the aggregated summary (KPIs, compliance
/// breakdown, site risk) and the standalone compliance breakdown for the Compliance page.
/// </summary>
public static class DashboardEndpoints
{
    /// <summary>Registers the <c>/api/dashboard</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard").WithTags("Dashboard");

        group.MapGet("/", async (IDashboardService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetSummaryAsync(cancellationToken)))
            .WithName("GetDashboardSummary");

        group.MapGet("/compliance", async (IDashboardService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetComplianceAsync(cancellationToken)))
            .WithName("GetDashboardCompliance");

        return app;
    }
}
