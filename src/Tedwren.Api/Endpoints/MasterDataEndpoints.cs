using Tedwren.Abstractions.Contracts.MasterData;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the compliance master-data endpoints (<c>/api/master-data</c>): the SSIP/document-heading/"other
/// requirements" lists the subcontractor-onboarding wizard draws on (spec §5–§8). Reads are available to any
/// authenticated console user (tenant-scoped, R15) via the secure-by-default fallback policy; adding an
/// org-scoped custom entry and editing/soft-deleting need write access. Adding to the shared (global) list is
/// platform-admin only, enforced inside <see cref="IMasterDataService"/> so a customer Administrator cannot.
/// </summary>
public static class MasterDataEndpoints
{
    /// <summary>Registers the <c>/api/master-data</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapMasterDataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/master-data").WithTags("MasterData");

        group.MapGet("/{listKey}", async (string listKey, IMasterDataService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetListAsync(listKey, cancellationToken)))
            .WithName("GetMasterList");

        group.MapGet("/{listKey}/manage", async (string listKey, IMasterDataService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetForManagementAsync(listKey, cancellationToken)))
            .WithName("GetMasterListForManagement").RequireAuthorization("RequireWrite");

        group.MapPost("/", async (CreateMasterListItemRequest request, IMasterDataService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var id = await service.CreateAsync(request, cancellationToken);
                    return Results.Created($"/api/master-data/{request.ListKey}", new { id });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            })
            .WithName("CreateMasterListItem").RequireAuthorization("RequireWrite");

        group.MapPut("/{id:guid}", async (Guid id, UpdateMasterListItemRequest request, IMasterDataService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    await service.UpdateAsync(id, request, cancellationToken);
                    return Results.NoContent();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            })
            .WithName("UpdateMasterListItem").RequireAuthorization("RequireWrite");

        group.MapDelete("/{id:guid}", async (Guid id, IMasterDataService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    await service.DeactivateAsync(id, cancellationToken);
                    return Results.NoContent();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            })
            .WithName("DeactivateMasterListItem").RequireAuthorization("RequireWrite");

        return app;
    }
}
