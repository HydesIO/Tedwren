using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the site-entry decision and muster HTTP endpoints (MC-8–MC-14, MC-28, R2, R3, R10, R14). The decision
/// endpoint always returns 200 with a result body — including the admission, the actionable block reason and
/// every check (even those not run and why) — so the gate can act on it and the record reconstructs later. It
/// is reachable on no-compound schemes (MC-28) because it needs no fixed point of presence.
/// </summary>
public static class SiteEntryEndpoints
{
    /// <summary>Registers the <c>/api/site-entry</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapSiteEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/site-entry").WithTags("SiteEntry");

        group.MapPost("/decide", async (DecideEntryRequest request, ISiteEntryService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.DecideAsync(request, cancellationToken)))
            .WithName("DecideSiteEntry");

        group.MapGet("/muster/{siteId:guid}", async (Guid siteId, ISiteEntryService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetMusterAsync(siteId, cancellationToken)))
            .WithName("GetMuster");

        return app;
    }
}
