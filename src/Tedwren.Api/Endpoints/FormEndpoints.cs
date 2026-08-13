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

        // Submissions — completing a form at organisation/site level (requirement 7), append-only evidence (R4/R10).
        group.MapGet("/submissions", async (IFormSubmissionService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetSubmissionsAsync(cancellationToken)))
            .WithName("GetFormSubmissions");

        group.MapGet("/submissions/{id:guid}", async (Guid id, IFormSubmissionService service, CancellationToken cancellationToken) =>
                await service.GetSubmissionAsync(id, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("GetFormSubmission");

        group.MapPost("/submissions", async (CreateFormSubmissionRequest request, IFormSubmissionService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var id = await service.SubmitAsync(request, cancellationToken);
                    return Results.Created($"/api/forms/submissions/{id}", new { id });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("SubmitForm");

        group.MapPost("/submissions/{id:guid}/approve", async (Guid id, ReviewFormSubmissionRequest request, IFormSubmissionService service, CancellationToken cancellationToken) =>
                await service.ApproveAsync(id, request, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("ApproveFormSubmission")
            .RequireAuthorization("RequireWrite");

        group.MapPost("/submissions/{id:guid}/reject", async (Guid id, ReviewFormSubmissionRequest request, IFormSubmissionService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.RejectAsync(id, request, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("RejectFormSubmission")
            .RequireAuthorization("RequireWrite");

        group.MapGet("/submissions/files/{fileId:guid}", async (Guid fileId, IFormSubmissionService service, CancellationToken cancellationToken) =>
                await service.GetFileAsync(fileId, cancellationToken) is { } file
                    ? Results.File(file.Content, file.ContentType, file.FileName)
                    : Results.NotFound())
            .WithName("GetFormSubmissionFile");

        // PDF & email of a completed form (requirements 4 & 6).
        group.MapGet("/submissions/{id:guid}/pdf", async (Guid id, IFormSubmissionService service, CancellationToken cancellationToken) =>
                await service.GeneratePdfAsync(id, cancellationToken) is { } pdf
                    ? Results.File(pdf.Content, pdf.ContentType, pdf.FileName)
                    : Results.NotFound())
            .WithName("GetFormSubmissionPdf");

        group.MapPost("/submissions/{id:guid}/email", async (Guid id, EmailFormSubmissionRequest request, IFormSubmissionService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.EmailAsync(id, request.RecipientEmail, cancellationToken)
                        ? Results.NoContent()
                        : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("EmailFormSubmission")
            .RequireAuthorization("RequireWrite");

        // Assignments — assign a form to a site / operator / the organisation / an induction (requirement 5).
        group.MapGet("/assignments", async (IFormAssignmentService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetAssignmentsAsync(cancellationToken)))
            .WithName("GetFormAssignments");

        group.MapPost("/assignments", async (CreateFormAssignmentRequest request, IFormAssignmentService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    var id = await service.CreateAsync(request, cancellationToken);
                    return Results.Created($"/api/forms/assignments/{id}", new { id });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CreateFormAssignment")
            .RequireAuthorization("RequireWrite");

        group.MapDelete("/assignments/{id:guid}", async (Guid id, IFormAssignmentService service, CancellationToken cancellationToken) =>
                await service.DeleteAsync(id, cancellationToken) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteFormAssignment")
            .RequireAuthorization("RequireWrite");

        return app;
    }
}
