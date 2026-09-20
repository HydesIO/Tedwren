using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the authenticated manager site-entry endpoints (<c>/api/manager/*</c>, M7): the live muster (MC-12–MC-14)
/// and an authenticated entry decision + day-only override (MC-8/MC-11). These are the console-authenticated
/// counterparts to the anonymous <c>/api/site-entry</c> kiosk routes — a signed-in manager on the mobile app
/// reaches them with their console token. The company is always taken from the token (R15); the override is
/// attributed to the signed-in manager, not a free-text value (MC-11). The decision is made against current data,
/// fails closed and is never queued offline (R2/R3). Reads use the secure-by-default fallback policy (any console
/// role, including a read-only Auditor); the decision requires the <c>RequireWrite</c> policy (SF-23).
/// </summary>
public static class ManagerSiteEntryEndpoints
{
    /// <summary>Registers the <c>/api/manager</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapManagerSiteEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/manager").WithTags("Manager");

        group.MapGet("/muster/{siteId:guid}", async (Guid siteId, ISiteService sites, ISiteEntryService siteEntry, CancellationToken cancellationToken) =>
            {
                // R15: reuse the tenant-scoped site read (scoped to the token's company via ICurrentUserService) —
                // a cross-company or unknown site resolves to null and is refused before any muster is served.
                if (await sites.GetSiteAsync(siteId.ToString(), cancellationToken) is null)
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                return Results.Ok(await siteEntry.GetMusterAsync(siteId, cancellationToken));
            })
            .WithName("ManagerGetMuster");

        group.MapPost("/entry/decide", async (ManagerDecideRequest request, ICurrentUserService currentUser, ISiteService sites, ISiteEntryService siteEntry, CancellationToken cancellationToken) =>
            {
                var me = await currentUser.GetCurrentAsync(cancellationToken);
                if (me.CompanyId is not { } companyId)
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                // R15: the site must belong to the manager's company before any decision is recorded against it.
                if (await sites.GetSiteAsync(request.SiteId.ToString(), cancellationToken) is null)
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                // MC-11: attribute the override to the authenticated manager, never a client-supplied name.
                var over = string.IsNullOrWhiteSpace(request.OverrideReason)
                    ? null
                    : new ManagerOverrideDto(me.Name, request.OverrideReason!.Trim());

                var result = await siteEntry.DecideAsync(
                    new DecideEntryRequest(companyId, request.SiteId, request.PersonId, request.PropertyId, over),
                    cancellationToken);

                return Results.Ok(result);
            })
            .WithName("ManagerDecideEntry")
            .RequireAuthorization("RequireWrite");

        return app;
    }
}
