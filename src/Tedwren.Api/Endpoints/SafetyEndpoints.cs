using Tedwren.Abstractions.Contracts.Safety;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the safety-events endpoints (<c>/api/safety</c>, PRD §8.2): hazard / near-miss reporting and the structured
/// accident / incident record (with RIDDOR). The whole group is part of the paid HSE module, so it is
/// entitlement-gated server-side and fails closed (Q2); the caller's company and identity are resolved from the
/// request claims (R15), never trusted from the client.
/// </summary>
public static class SafetyEndpoints
{
    /// <summary>Registers the <c>/api/safety</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapSafetyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/safety").WithTags("Safety").AddEndpointFilter(ModuleGate.Require("hse"));

        MapHazardEndpoints(group);
        MapIncidentEndpoints(group);
        return app;
    }

    /// <summary>Maps the hazard / near-miss report routes.</summary>
    private static void MapHazardEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/hazards", async (ICurrentUserService currentUser, IHazardReportService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return Results.Ok(await service.ListAsync(companyId, cancellationToken));
            })
            .WithName("ListHazards");

        group.MapGet("/hazards/stats", async (ICurrentUserService currentUser, IHazardReportService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return Results.Ok(await service.GetStatsAsync(companyId, cancellationToken));
            })
            .WithName("HazardStats");

        group.MapGet("/hazards/{id:guid}", async (Guid id, ICurrentUserService currentUser, IHazardReportService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return await service.GetAsync(companyId, id, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound();
            })
            .WithName("GetHazard");

        group.MapPost("/hazards", async (ReportHazardRequest request, ICurrentUserService currentUser, IHazardReportService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var user = await currentUser.GetCurrentAsync(cancellationToken);
                    var dto = await service.ReportAsync(user.CompanyId ?? Guid.Empty, user.Name, request, cancellationToken);
                    return Results.Created($"/api/safety/hazards/{dto.Id}", dto);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("ReportHazard").RequireAuthorization("RequireWrite");

        group.MapPost("/hazards/{id:guid}/assign", async (Guid id, AssignHazardRequest request, ICurrentUserService currentUser, IHazardReportService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                    return await service.AssignAsync(companyId, id, request.AssignedTo, request.Category, cancellationToken)
                        ? Results.NoContent() : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("AssignHazard").RequireAuthorization("RequireWrite");

        group.MapPost("/hazards/{id:guid}/close", async (Guid id, CloseHazardRequest request, ICurrentUserService currentUser, IHazardReportService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                    return await service.CloseAsync(companyId, id, request?.Note ?? string.Empty, cancellationToken)
                        ? Results.NoContent() : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CloseHazard").RequireAuthorization("RequireWrite");
    }

    /// <summary>Maps the accident / incident record routes.</summary>
    private static void MapIncidentEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/incidents", async (ICurrentUserService currentUser, IIncidentReportService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return Results.Ok(await service.ListAsync(companyId, cancellationToken));
            })
            .WithName("ListIncidents");

        group.MapGet("/incidents/{id:guid}", async (Guid id, ICurrentUserService currentUser, IIncidentReportService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return await service.GetAsync(companyId, id, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound();
            })
            .WithName("GetIncident");

        group.MapPost("/incidents", async (ReportIncidentRequest request, ICurrentUserService currentUser, IIncidentReportService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var user = await currentUser.GetCurrentAsync(cancellationToken);
                    var dto = await service.ReportAsync(user.CompanyId ?? Guid.Empty, user.Name, request, cancellationToken);
                    return Results.Created($"/api/safety/incidents/{dto.Id}", dto);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("ReportIncident").RequireAuthorization("RequireWrite");

        group.MapPut("/incidents/{id:guid}/investigation", async (Guid id, UpdateIncidentInvestigationRequest request, ICurrentUserService currentUser, IIncidentReportService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return await service.UpdateInvestigationAsync(companyId, id, request, cancellationToken)
                    ? Results.NoContent() : Results.NotFound();
            })
            .WithName("UpdateIncidentInvestigation").RequireAuthorization("RequireWrite");

        group.MapPost("/incidents/{id:guid}/close", async (Guid id, ICurrentUserService currentUser, IIncidentReportService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return await service.CloseAsync(companyId, id, cancellationToken)
                    ? Results.NoContent() : Results.NotFound();
            })
            .WithName("CloseIncident").RequireAuthorization("RequireWrite");
    }
}
