using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the demo-data admin endpoints (<c>/api/admin/demo-data</c>) used by the Product Admin portal to seed,
/// recreate and delete the demonstration dataset, and to poll a running operation's progress. The whole group
/// is gated by the <c>PlatformAdmin</c> policy so only a Tedwren platform administrator can reach it.
/// </summary>
public static class DemoDataEndpoints
{
    /// <summary>Registers the demo-data endpoint group.</summary>
    public static IEndpointRouteBuilder MapDemoDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/demo-data")
            .WithTags("Demo data")
            .RequireAuthorization("PlatformAdmin");

        group.MapGet("/status", async (IDemoDataService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetStatusAsync(cancellationToken)))
            .WithName("GetDemoDataStatus");

        group.MapGet("/progress", (IDemoDataService service) =>
                Results.Ok(service.GetProgress()))
            .WithName("GetDemoDataProgress");

        group.MapPost("/seed", async (IDemoDataService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.SeedAsync(cancellationToken)))
            .WithName("SeedDemoData");

        group.MapPost("/delete", async (IDemoDataService service, CancellationToken cancellationToken) =>
            {
                await service.DeleteAsync(cancellationToken);
                return Results.Ok(await service.GetStatusAsync(cancellationToken));
            })
            .WithName("DeleteDemoData");

        return app;
    }
}
