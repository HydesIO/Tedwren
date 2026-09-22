using Tedwren.Abstractions.Contracts.Rams;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the RAMS review endpoints (<c>/api/rams</c>, PRD §8.2). The whole group is part of the paid HSE module, so
/// it is entitlement-gated server-side and fails closed (Q2); the caller's company and reviewer identity are
/// resolved from the request claims (R15), never trusted from the client.
/// </summary>
public static class RamsEndpoints
{
    /// <summary>Registers the <c>/api/rams</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapRamsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rams").WithTags("Rams").AddEndpointFilter(ModuleGate.Require("hse"));

        group.MapGet("/", async (ICurrentUserService currentUser, IRamsService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return Results.Ok(await service.ListAsync(companyId, cancellationToken));
            })
            .WithName("ListRams");

        group.MapGet("/queue", async (ICurrentUserService currentUser, IRamsService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return Results.Ok(await service.GetReviewQueueAsync(companyId, cancellationToken));
            })
            .WithName("RamsReviewQueue");

        group.MapPost("/", async (SubmitRamsRequest request, ICurrentUserService currentUser, IRamsService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                    var dto = await service.SubmitAsync(companyId, request, cancellationToken);
                    return Results.Created($"/api/rams/{dto.Id}", dto);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("SubmitRams").RequireAuthorization("RequireWrite");

        group.MapPost("/{id:guid}/approve", async (Guid id, ICurrentUserService currentUser, IRamsService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var user = await currentUser.GetCurrentAsync(cancellationToken);
                    return await service.ApproveAsync(user.CompanyId ?? Guid.Empty, id, user.Name, cancellationToken)
                        ? Results.NoContent() : Results.NotFound();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { reason = ex.Message });
                }
            })
            .WithName("ApproveRams").RequireAuthorization("RequireWrite");

        group.MapPost("/{id:guid}/approve-with-comments", async (Guid id, ReviewRamsRequest request, ICurrentUserService currentUser, IRamsService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var user = await currentUser.GetCurrentAsync(cancellationToken);
                    return await service.ApproveWithCommentsAsync(user.CompanyId ?? Guid.Empty, id, user.Name, request?.Note ?? string.Empty, cancellationToken)
                        ? Results.NoContent() : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { reason = ex.Message });
                }
            })
            .WithName("ApproveRamsWithComments").RequireAuthorization("RequireWrite");

        group.MapPost("/{id:guid}/reject", async (Guid id, ReviewRamsRequest request, ICurrentUserService currentUser, IRamsService service, CancellationToken cancellationToken) =>
                await DecideAsync(id, request, currentUser, service, reject: true, cancellationToken))
            .WithName("RejectRams").RequireAuthorization("RequireWrite");

        group.MapPost("/{id:guid}/return", async (Guid id, ReviewRamsRequest request, ICurrentUserService currentUser, IRamsService service, CancellationToken cancellationToken) =>
                await DecideAsync(id, request, currentUser, service, reject: false, cancellationToken))
            .WithName("ReturnRams").RequireAuthorization("RequireWrite");

        return app;
    }

    /// <summary>Shared reject/return handler: a required note maps to a 400, a wrong-state RAMS to a 409, an unknown one to a 404.</summary>
    private static async Task<IResult> DecideAsync(
        Guid id, ReviewRamsRequest request, ICurrentUserService currentUser, IRamsService service, bool reject, CancellationToken cancellationToken)
    {
        try
        {
            var user = await currentUser.GetCurrentAsync(cancellationToken);
            var companyId = user.CompanyId ?? Guid.Empty;
            var note = request?.Note ?? string.Empty;
            var ok = reject
                ? await service.RejectAsync(companyId, id, user.Name, note, cancellationToken)
                : await service.ReturnAsync(companyId, id, user.Name, note, cancellationToken);
            return ok ? Results.NoContent() : Results.NotFound();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { reason = ex.Message });
        }
    }
}
