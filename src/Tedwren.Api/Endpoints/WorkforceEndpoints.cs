using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the org-wide workforce endpoints (<c>/api/workforce</c>): the operative register and a single
/// operative's profile, composed from person, engagement, qualification and decision data.
/// </summary>
public static class WorkforceEndpoints
{
    /// <summary>Registers the <c>/api/workforce</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapWorkforceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/workforce").WithTags("Workforce");

        group.MapGet("/", async (IWorkforceService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListOperativesAsync(cancellationToken)))
            .WithName("ListOperatives");

        group.MapGet("/{slug}", async (string slug, IWorkforceService service, CancellationToken cancellationToken) =>
            {
                var operative = await service.GetOperativeBySlugAsync(slug, cancellationToken);
                return operative is null ? Results.NotFound() : Results.Ok(operative);
            })
            .WithName("GetOperative");

        return app;
    }
}
