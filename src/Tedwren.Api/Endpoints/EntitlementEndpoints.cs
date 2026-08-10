using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the module-entitlement HTTP endpoints (Q2, SF-22). One authoritative, server-side, fail-closed
/// answer to "has this customer bought this module", so navigation shows only what is purchased.
/// </summary>
public static class EntitlementEndpoints
{
    /// <summary>Registers the <c>/api/entitlements</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapEntitlementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/entitlements").WithTags("Entitlements");

        group.MapGet("/{companyId:guid}", async (Guid companyId, IEntitlementService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetForCompanyAsync(companyId, cancellationToken)))
            .WithName("GetEntitlements");

        group.MapGet("/{companyId:guid}/{moduleKey}", async (Guid companyId, string moduleKey, IEntitlementService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.IsEnabledAsync(companyId, moduleKey, cancellationToken)))
            .WithName("IsModuleEnabled");

        return app;
    }
}
