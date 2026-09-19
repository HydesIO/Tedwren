using Tedwren.Abstractions.Contracts.Permits;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>Maps the permits-to-work endpoints (<c>/api/permits</c>): raise a permit and list a company's permits.</summary>
public static class PermitEndpoints
{
    /// <summary>Registers the <c>/api/permits</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapPermitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/permits").WithTags("Permits");

        group.MapPost("/", async (CreatePermitRequest request, IPermitService service, CancellationToken cancellationToken) =>
            {
                var id = await service.CreateAsync(request, cancellationToken);
                return Results.Created($"/api/permits/{id}", id);
            })
            .WithName("CreatePermit").RequireAuthorization("RequireWrite");

        group.MapGet("/company/{companyId:guid}", async (Guid companyId, IPermitService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListForCompanyAsync(companyId, cancellationToken)))
            .WithName("ListCompanyPermits");

        // Lifecycle transitions (PRD §8.2). Writes, so gated to non-Auditor roles (SF-23); the caller's company is
        // resolved server-side (R15). A wrong-state transition is a 409; an unknown/cross-tenant permit is a 404.
        group.MapPost("/{id:guid}/approve", async (Guid id, IPermitService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.ApproveAsync(id, cancellationToken) ? Results.Ok() : Results.NotFound();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { reason = ex.Message });
                }
            })
            .WithName("ApprovePermit").RequireAuthorization("RequireWrite");

        group.MapPost("/{id:guid}/close", async (Guid id, ClosePermitRequest request, IPermitService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.CloseAsync(id, request?.Reason, cancellationToken) ? Results.Ok() : Results.NotFound();
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { reason = ex.Message });
                }
            })
            .WithName("ClosePermit").RequireAuthorization("RequireWrite");

        return app;
    }
}
