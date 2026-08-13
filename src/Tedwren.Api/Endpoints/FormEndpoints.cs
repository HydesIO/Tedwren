using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the Forms Library HTTP endpoints (PRD-Phase 2 checklist/inspection engine). Reads are scoped to the
/// caller's company (R15) and require an authenticated user (the fallback policy); writes — create, update,
/// publish, archive — additionally require the "RequireWrite" role policy. No route is anonymous; the
/// worker-facing fill flow is a later phase.
/// </summary>
public static class FormEndpoints
{
    /// <summary>Registers the <c>/api/forms</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapFormEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/forms").WithTags("Forms");

        group.MapGet("/templates", async (IFormTemplateService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetTemplatesAsync(cancellationToken)))
            .WithName("GetFormTemplates");

        group.MapGet("/templates/{id:guid}", async (Guid id, IFormTemplateService service, CancellationToken cancellationToken) =>
                await service.GetTemplateAsync(id, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("GetFormTemplate");

        group.MapGet("/templates/{id:guid}/fill", async (Guid id, IFormTemplateService service, CancellationToken cancellationToken) =>
                await service.GetTemplateForFillAsync(id, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("GetFormTemplateForFill");

        group.MapPost("/templates", async (CreateFormTemplateRequest request, IFormTemplateService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var id = await service.CreateAsync(request, cancellationToken);
                    return Results.Created($"/api/forms/templates/{id}", new { id });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CreateFormTemplate")
            .RequireAuthorization("RequireWrite");

        group.MapPut("/templates/{id:guid}", async (Guid id, UpdateFormTemplateRequest request, IFormTemplateService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.UpdateAsync(id, request, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateFormTemplate")
            .RequireAuthorization("RequireWrite");

        group.MapPost("/templates/{id:guid}/publish", async (Guid id, IFormTemplateService service, CancellationToken cancellationToken) =>
                await service.PublishAsync(id, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("PublishFormTemplate")
            .RequireAuthorization("RequireWrite");

        group.MapPost("/templates/{id:guid}/archive", async (Guid id, IFormTemplateService service, CancellationToken cancellationToken) =>
                await service.ArchiveAsync(id, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("ArchiveFormTemplate")
            .RequireAuthorization("RequireWrite");

        return app;
    }
}
