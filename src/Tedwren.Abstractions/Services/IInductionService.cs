using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Contracts.Inductions;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The digital-induction service (MC-1–MC-7, MC-15, MC-20, R5). Runs the whole induction in a phone browser:
/// configurable capture steps (MC-3), a quiz whose answers are <b>never sent to the device</b> and are scored
/// on the server (R5), required-step gating before completion (MC-4), failed-attempt handling with manager
/// reset (MC-6), a completion reference with configurable validity and re-induction supersedes (MC-7), and a
/// separate optional consent (MC-20). Company reads are tenant-scoped (R15).
/// </summary>
public interface IInductionService
{
    /// <summary>Lists a company's induction templates (MC-3).</summary>
    Task<IReadOnlyList<InductionTemplateDto>> GetTemplatesAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Creates an induction template for a company, seeded from the shipped default (MC-3/SF-12), and returns its id.</summary>
    Task<Guid> CreateDefaultTemplateAsync(CreateInductionTemplateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns a template for authoring (answers + config, MC-15) — authorised admin only. Null if not found.</summary>
    Task<InductionTemplateAuthoringDto?> GetTemplateForEditAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>Updates a template's content and configuration (MC-4/MC-5/MC-15). Null when not found.</summary>
    Task<InductionTemplateAuthoringDto?> UpdateTemplateAsync(Guid templateId, UpdateInductionTemplateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Starts an induction, superseding the operative's prior induction for the same template (MC-1/MC-7).</summary>
    Task<InductionSessionDto> StartAsync(StartInductionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the device-facing session (steps + quiz without answers), or null (R5).</summary>
    Task<InductionSessionDto?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>Marks a step completed (MC-4). Returns the updated session, or null if not found.</summary>
    Task<InductionSessionDto?> CompleteStepAsync(Guid sessionId, string stepId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the published form attached to a <c>Form</c> step of the session, ready to fill (Forms Library
    /// integration, requirement 5). Only forms reached through the worker's own session are exposed on this
    /// anonymous path (R5). Null when the session/step is not found, the step is not a form, or no published
    /// version exists.
    /// </summary>
    Task<FormTemplateDto?> GetSessionFormAsync(Guid sessionId, string stepId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits the completed form for a session's <c>Form</c> step (the anonymous take-flow) and marks the step
    /// done (MC-4, requirement 5). The submission is recorded against the session's company and operative. Returns
    /// the updated session, or null when the session/step is not found or the step is not a form. Throws
    /// <see cref="System.ArgumentException"/> when required fields are unanswered.
    /// </summary>
    Task<InductionSessionDto?> SubmitSessionFormAsync(Guid sessionId, string stepId, CreateFormSubmissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Scores a quiz submission on the server and records the attempt (R5, MC-6). Null if not found.</summary>
    Task<QuizResultDto?> SubmitQuizAsync(Guid sessionId, SubmitQuizRequest request, CancellationToken cancellationToken = default);

    /// <summary>Finalises the induction (signature + consent) once required steps are done and the quiz passed (MC-4/MC-5/MC-20). Null if not found; throws if not ready.</summary>
    Task<InductionSessionDto?> FinalizeAsync(Guid sessionId, FinalizeInductionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Resets a failed induction so it can be retaken, recording the reason (MC-6). Null if not found.</summary>
    Task<InductionSessionDto?> ResetAsync(Guid sessionId, ResetInductionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists a company's induction sessions for the console (MC-15).</summary>
    Task<IReadOnlyList<InductionSummaryDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
