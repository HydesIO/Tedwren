using Tedwren.Abstractions.Contracts.Havs;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the HAVs exposure endpoints (<c>/api/havs</c>, PRD §8.2). The whole group is part of the paid HSE module,
/// so it is entitlement-gated server-side and fails closed (Q2); the caller's company and identity are resolved
/// from the request claims (R15), never trusted from the client.
/// </summary>
public static class HavsEndpoints
{
    /// <summary>Registers the <c>/api/havs</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapHavsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/havs").WithTags("Havs").AddEndpointFilter(ModuleGate.Require("hse"));

        group.MapGet("/", async (ICurrentUserService currentUser, IHavsExposureService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return Results.Ok(await service.ListAsync(companyId, cancellationToken));
            })
            .WithName("ListHavsExposure");

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUserService currentUser, IHavsExposureService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return await service.GetAsync(companyId, id, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound();
            })
            .WithName("GetHavsExposure");

        group.MapPost("/", async (CreateHavsExposureRequest request, ICurrentUserService currentUser, IHavsExposureService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var user = await currentUser.GetCurrentAsync(cancellationToken);
                    var dto = await service.RecordAsync(user.CompanyId ?? Guid.Empty, user.Name, request, cancellationToken);
                    return Results.Created($"/api/havs/{dto.Id}", dto);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("RecordHavsExposure").RequireAuthorization("RequireWrite");

        return app;
    }
}
