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
            .WithName("CreatePermit");

        group.MapGet("/company/{companyId:guid}", async (Guid companyId, IPermitService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.ListForCompanyAsync(companyId, cancellationToken)))
            .WithName("ListCompanyPermits");

        return app;
    }
}
