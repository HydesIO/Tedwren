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

        group.MapPost("/sessions", async (StartInductionRequest request, IInductionService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.StartAsync(request, cancellationToken)))
            .WithName("StartInduction");

        group.MapGet("/sessions/{sessionId:guid}", async (Guid sessionId, IInductionService service, CancellationToken cancellationToken) =>
                await service.GetSessionAsync(sessionId, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("GetInductionSession");

        group.MapPost("/sessions/{sessionId:guid}/steps/{stepId}/complete", async (Guid sessionId, string stepId, IInductionService service, CancellationToken cancellationToken) =>
                await service.CompleteStepAsync(sessionId, stepId, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("CompleteInductionStep");

        group.MapPost("/sessions/{sessionId:guid}/quiz", async (Guid sessionId, SubmitQuizRequest request, IInductionService service, CancellationToken cancellationToken) =>
                await service.SubmitQuizAsync(sessionId, request, cancellationToken) is { } result ? Results.Ok(result) : Results.NotFound())
            .WithName("SubmitInductionQuiz");

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
            .WithName("FinalizeInduction");

        group.MapPost("/sessions/{sessionId:guid}/reset", async (Guid sessionId, ResetInductionRequest request, IInductionService service, CancellationToken cancellationToken) =>
                await service.ResetAsync(sessionId, request, cancellationToken) is { } dto ? Results.Ok(dto) : Results.NotFound())
            .WithName("ResetInduction");

        group.MapGet("/company/{companyId:guid}", async (Guid companyId, IInductionService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetForCompanyAsync(companyId, cancellationToken)))
            .WithName("GetCompanyInductions");

        return app;
    }
}
