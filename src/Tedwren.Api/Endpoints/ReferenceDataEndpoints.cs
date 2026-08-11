using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the reference-data endpoints (<c>/api/reference</c>) that serve the fixed form option lists
/// (trades, company types, permit types, regions) from the database.
/// </summary>
public static class ReferenceDataEndpoints
{
    /// <summary>Registers the <c>/api/reference</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapReferenceDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reference").WithTags("Reference");

        group.MapGet("/{listKey}", async (string listKey, IReferenceDataService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetListAsync(listKey, cancellationToken)))
            .WithName("GetReferenceList");

        return app;
    }
}
