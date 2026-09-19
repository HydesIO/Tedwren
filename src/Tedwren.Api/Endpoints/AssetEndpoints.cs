using Tedwren.Abstractions.Contracts.Assets;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the plant &amp; equipment register endpoints (<c>/api/assets</c>, PRD §8.2). The whole group is part of
/// the paid Health, Safety &amp; Compliance module, so it is entitlement-gated server-side and fails closed (Q2);
/// the caller's company is resolved from the request claims (R15), never trusted from the client.
/// </summary>
public static class AssetEndpoints
{
    /// <summary>Registers the <c>/api/assets</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assets").WithTags("Assets").AddEndpointFilter(ModuleGate.Require("hse"));

        group.MapGet("/", async (ICurrentUserService currentUser, IAssetService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return Results.Ok(await service.ListForCompanyAsync(companyId, cancellationToken));
            })
            .WithName("ListAssets");

        group.MapPost("/", async (CreateAssetRequest request, ICurrentUserService currentUser, IAssetService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                    var id = await service.CreateAsync(companyId, request, cancellationToken);
                    return Results.Created($"/api/assets/{id}", new { id });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CreateAsset").RequireAuthorization("RequireWrite");

        group.MapPut("/{id:guid}", async (Guid id, UpdateAssetRequest request, ICurrentUserService currentUser, IAssetService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                    return await service.UpdateAsync(companyId, id, request, cancellationToken) ? Results.NoContent() : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateAsset").RequireAuthorization("RequireWrite");

        group.MapPost("/{id:guid}/retire", async (Guid id, ICurrentUserService currentUser, IAssetService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return await service.RetireAsync(companyId, id, cancellationToken) ? Results.NoContent() : Results.NotFound();
            })
            .WithName("RetireAsset").RequireAuthorization("RequireWrite");

        return app;
    }
}
