using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the sites HTTP endpoints (SF-6, SF-14, SF-25, SF-26). Each delegates to <see cref="ISiteService"/>,
/// so the same behaviour is served whether the data is mock or database, and by the web client or a future
/// mobile app.
/// </summary>
public static class SiteEndpoints
{
    /// <summary>Registers the <c>/api/sites</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapSiteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sites").WithTags("Sites");

        group.MapGet("/", async (ISiteService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetSitesAsync(cancellationToken)))
            .WithName("GetSites");

        group.MapGet("/{slug}", async (string slug, ISiteService service, CancellationToken cancellationToken) =>
            {
                var site = await service.GetSiteAsync(slug, cancellationToken);
                return site is null ? Results.NotFound() : Results.Ok(site);
            })
            .WithName("GetSite");

        group.MapPost("/", async (CreateSiteRequest request, ISiteService service, CancellationToken cancellationToken) =>
            {
                var id = await service.CreateSiteAsync(request, cancellationToken);
                return Results.Created($"/api/sites/{id}", new { id });
            })
            .WithName("CreateSite");

        group.MapPut("/{siteId:guid}", async (Guid siteId, UpdateSiteRequest request, ISiteService service, CancellationToken cancellationToken) =>
            {
                var updated = await service.UpdateSiteAsync(siteId, request, cancellationToken);
                return updated ? Results.NoContent() : Results.NotFound();
            })
            .WithName("UpdateSite");

        group.MapPost("/{siteId:guid}/properties",
                async (Guid siteId, AddPropertyBody body, ISiteService service, CancellationToken cancellationToken) =>
                {
                    var id = await service.AddPropertyAsync(
                        new AddSitePropertyRequest(siteId, body.Address, body.Units, body.Boundary), cancellationToken);
                    return id is null ? Results.NotFound() : Results.Created($"/api/sites/{siteId}/properties/{id}", new { id });
                })
            .WithName("AddSiteProperty");

        // A console user's assigned sites (MC-21/UAT-011): a site manager sees only these.
        group.MapGet("/assignments/{userId:guid}",
                async (Guid userId, ISiteService service, CancellationToken cancellationToken) =>
                    Results.Ok(await service.GetAssignedSiteIdsAsync(userId, cancellationToken)))
            .WithName("GetSiteAssignments");

        group.MapPut("/assignments/{userId:guid}",
                async (Guid userId, AssignSitesBody body, ISiteService service, CancellationToken cancellationToken) =>
                {
                    await service.SetAssignedSitesAsync(userId, body.SiteIds, cancellationToken);
                    return Results.NoContent();
                })
            .WithName("SetSiteAssignments");

        return app;
    }

    /// <summary>Body for adding a property — the site id comes from the route.</summary>
    private sealed record AddPropertyBody(string Address, int Units, GeofenceDto Boundary);

    /// <summary>Body for setting a user's assigned sites — the user id comes from the route (MC-21/UAT-011).</summary>
    private sealed record AssignSitesBody(IReadOnlyList<Guid> SiteIds);
}
