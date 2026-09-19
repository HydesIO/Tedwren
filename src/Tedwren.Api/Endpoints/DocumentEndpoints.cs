using Tedwren.Abstractions.Contracts.Documents;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the document distribution &amp; acknowledgement endpoints (<c>/api/documents</c>, PRD §8.2). The whole
/// group is part of the paid HSE module, so it is entitlement-gated server-side and fails closed (Q2); the
/// caller's company and sender identity are resolved from the request claims (R15), never trusted from the client.
/// </summary>
public static class DocumentEndpoints
{
    /// <summary>Registers the <c>/api/documents</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents").AddEndpointFilter(ModuleGate.Require("hse"));

        group.MapGet("/", async (ICurrentUserService currentUser, IDocumentDistributionService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return Results.Ok(await service.ListAsync(companyId, cancellationToken));
            })
            .WithName("ListDocumentDistributions");

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUserService currentUser, IDocumentDistributionService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return await service.GetAsync(companyId, id, cancellationToken) is { } detail ? Results.Ok(detail) : Results.NotFound();
            })
            .WithName("GetDocumentDistribution");

        group.MapPost("/", async (CreateDistributionRequest request, ICurrentUserService currentUser, IDocumentDistributionService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var user = await currentUser.GetCurrentAsync(cancellationToken);
                    var dto = await service.CreateAsync(user.CompanyId ?? Guid.Empty, user.Name, request, cancellationToken);
                    return Results.Created($"/api/documents/{dto.Id}", dto);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CreateDocumentDistribution").RequireAuthorization("RequireWrite");

        group.MapPost("/acknowledgements/{ackId:guid}/sign", async (Guid ackId, ICurrentUserService currentUser, IDocumentDistributionService service, CancellationToken cancellationToken) =>
            {
                var companyId = (await currentUser.GetCurrentAsync(cancellationToken)).CompanyId ?? Guid.Empty;
                return await service.AcknowledgeAsync(companyId, ackId, cancellationToken) ? Results.NoContent() : Results.NotFound();
            })
            .WithName("SignDocumentAcknowledgement").RequireAuthorization("RequireWrite");

        return app;
    }
}
