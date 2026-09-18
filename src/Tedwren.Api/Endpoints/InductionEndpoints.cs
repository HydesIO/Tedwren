using System.Security.Claims;
using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Abstractions.Services;

namespace Tedwren.Api.Endpoints;

/// <summary>
/// Maps the digital-induction HTTP endpoints (MC-1–MC-7, MC-15, MC-20, R5). The device-facing routes serve the
/// steps and quiz <b>without answers</b> and submit answers for server-side scoring (R5); completion is gated
/// on required steps and a passed quiz (MC-4); a failed induction can be reset by a manager with a reason
/// (MC-6). A not-ready finalise returns 409.
/// </summary>
public static class InductionEndpoints
{
    /// <summary>Registers the <c>/api/inductions</c> endpoint group.</summary>
    public static IEndpointRouteBuilder MapInductionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inductions").WithTags("Inductions");

        group.MapGet("/templates/{companyId:guid}", async (Guid companyId, IInductionService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetTemplatesAsync(companyId, cancellationToken)))
            .WithName("GetInductionTemplates");

        group.MapPost("/templates", async (CreateInductionTemplateRequest request, IInductionService service, CancellationToken cancellationToken) =>
            {
                var id = await service.CreateDefaultTemplateAsync(request, cancellationToken);
                return Results.Created($"/api/inductions/templates/{request.CompanyId}", new { id });
            })
            .WithName("CreateInductionTemplate");

        // Authoring (MC-15) — authorised admin only (fallback policy applies): fetch with answers, then update.
        group.MapGet("/templates/{templateId:guid}/edit", async (Guid templateId, IInductionService service, CancellationToken cancellationToken) =>
                await service.GetTemplateForEditAsync(templateId, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("GetInductionTemplateForEdit");

        // The shipped-default induction as an authoring DTO, NOT persisted — seeds the "new induction" form so
        // the template row is created only on Publish (no orphan when the admin opens then cancels the builder).
        group.MapGet("/templates/default/edit", async (IInductionService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetDefaultTemplateForEditAsync(cancellationToken)))
            .WithName("GetDefaultInductionTemplateForEdit");

        group.MapPut("/templates/{templateId:guid}", async (Guid templateId, UpdateInductionTemplateRequest request, IInductionService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.UpdateTemplateAsync(templateId, request, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateInductionTemplate");

        // Admin creates a shareable, tokenised induction link (UAT-018) — authorised (the fallback policy applies).
        group.MapPost("/links", async (CreateInductionLinkRequest request, ClaimsPrincipal user, IInductionService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    Guid? userId = Guid.TryParse(user.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub), out var uid) ? uid : null;
                    return Results.Ok(await service.CreateLinkAsync(request, userId, cancellationToken));
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CreateInductionLink");

        // The worker opens a shared link with no console account (MC-1/MC-2, UAT-018) — anonymous, token+passcode gated.
        group.MapGet("/by-link/{token}", async (string token, string? passcode, IInductionService service, CancellationToken cancellationToken) =>
                await service.GetLinkAsync(token, passcode, cancellationToken) is { } view ? Results.Ok(view) : Results.StatusCode(StatusCodes.Status403Forbidden))
            .WithName("ViewInductionLink").AllowAnonymous();

        group.MapPost("/by-link/{token}/session", async (string token, StartInductionFromLinkRequest request, IInductionService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.StartFromLinkAsync(token, request.Passcode, request.PersonName, cancellationToken) is { } session
                        ? Results.Ok(session) : Results.StatusCode(StatusCodes.Status403Forbidden);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("StartInductionFromLink").AllowAnonymous();

        // The worker's take-flow runs from a link with no console account (MC-1/MC-2) — anonymous.
        group.MapPost("/sessions", async (StartInductionRequest request, IInductionService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.StartAsync(request, cancellationToken)))
            .WithName("StartInduction").AllowAnonymous();

        group.MapGet("/sessions/{sessionId:guid}", async (Guid sessionId, IInductionService service, CancellationToken cancellationToken) =>
                await service.GetSessionAsync(sessionId, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("GetInductionSession").AllowAnonymous();

        group.MapPost("/sessions/{sessionId:guid}/steps/{stepId}/complete", async (Guid sessionId, string stepId, IInductionService service, CancellationToken cancellationToken) =>
                await service.CompleteStepAsync(sessionId, stepId, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("CompleteInductionStep").AllowAnonymous();

        // The induction-embedded form (requirement 5, R5): served and submitted through the worker's own session, so
        // the anonymous surface only ever exposes forms attached to the induction being taken — never arbitrary forms.
        group.MapGet("/sessions/{sessionId:guid}/forms/{stepId}", async (Guid sessionId, string stepId, IInductionService service, CancellationToken cancellationToken) =>
                await service.GetSessionFormAsync(sessionId, stepId, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("GetInductionSessionForm").AllowAnonymous();

        group.MapPost("/sessions/{sessionId:guid}/forms/{stepId}", async (Guid sessionId, string stepId, CreateFormSubmissionRequest request, IInductionService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.SubmitSessionFormAsync(sessionId, stepId, request, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound();
                }
                catch (ArgumentException ex)
                {
                    // Required-by-default validation failed (R2).
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("SubmitInductionSessionForm").AllowAnonymous();

        group.MapPost("/sessions/{sessionId:guid}/quiz", async (Guid sessionId, SubmitQuizRequest request, IInductionService service, CancellationToken cancellationToken) =>
                await service.SubmitQuizAsync(sessionId, request, cancellationToken) is { } result ? Results.Ok(result) : Results.NotFound())
            .WithName("SubmitInductionQuiz").AllowAnonymous();

        group.MapPost("/sessions/{sessionId:guid}/finalize", async (Guid sessionId, FinalizeInductionRequest request, IInductionService service, CancellationToken cancellationToken) =>
            {
                try
                {
                    return await service.FinalizeAsync(sessionId, request, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound();
                }
                catch (InvalidOperationException ex)
                {
                    // MC-4: cannot complete until required steps are done and the quiz is passed.
                    return Results.Conflict(new { reason = ex.Message });
                }
            })
            .WithName("FinalizeInduction").AllowAnonymous();

        group.MapPost("/sessions/{sessionId:guid}/reset", async (Guid sessionId, ResetInductionRequest request, IInductionService service, CancellationToken cancellationToken) =>
                await service.ResetAsync(sessionId, request, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("ResetInduction");

        group.MapGet("/company/{companyId:guid}", async (Guid companyId, IInductionService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetForCompanyAsync(companyId, cancellationToken)))
            .WithName("GetCompanyInductions");

        return app;
    }
}
