namespace Tedwren.Abstractions.Contracts.Inductions;

/// <summary>A configurable induction step as shown on the device (MC-3).</summary>
public sealed record InductionStepDto(string Id, string Kind, string Label, bool Required);

/// <summary>
/// A quiz question <b>as sent to the device</b> — the prompt and options only. The correct answer is never
/// included; scoring happens on the server (R5).
/// </summary>
public sealed record InductionQuizQuestionDto(string Id, string Prompt, IReadOnlyList<string> Options);

/// <summary>An induction template for management/preview (does not expose quiz answers, R5).</summary>
public sealed record InductionTemplateDto(
    Guid Id,
    string Name,
    int ValidityDays,
    int PassMark,
    IReadOnlyList<InductionStepDto> Steps,
    int QuestionCount);

/// <summary>The device-facing induction session: what the operative needs to complete the induction (R5).</summary>
public sealed record InductionSessionDto(
    Guid Id,
    Guid TemplateId,
    string TemplateName,
    Guid PersonId,
    string PersonName,
    string Status,
    IReadOnlyList<InductionStepDto> Steps,
    IReadOnlyList<string> CompletedStepIds,
    IReadOnlyList<InductionQuizQuestionDto> Questions,
    int PassMark,
    string? CompletionReference,
    DateTimeOffset? ExpiresUtc,
    int AttemptCount);

/// <summary>
/// Request to create an induction template for a company by seeding the shipped default steps and quiz (MC-3),
/// so a newly-onboarded main contractor can run an induction immediately (SF-12). The customer refines the
/// steps and questions afterwards.
/// </summary>
public sealed record CreateInductionTemplateRequest(Guid CompanyId, string Name, int ValidityDays, int PassMark);

/// <summary>Request to start an induction for an operative (MC-1).</summary>
public sealed record StartInductionRequest(Guid CompanyId, Guid TemplateId, Guid PersonId, string PersonName);

/// <summary>Request to submit quiz answers (question id → chosen option index) for server-side scoring (R5).</summary>
public sealed record SubmitQuizRequest(IReadOnlyDictionary<string, int> Answers);

/// <summary>The outcome of a scored quiz attempt — score and pass/fail only; no answers are returned (R5).</summary>
public sealed record QuizResultDto(int Correct, int Total, bool Passed, int AttemptCount);

/// <summary>Request to finalise an induction: signature and the separate, optional consent (MC-5, MC-20).</summary>
public sealed record FinalizeInductionRequest(string SignatureName, bool ConsentGiven);

/// <summary>Request for a manager to reset a failed induction, with a recorded reason (MC-6).</summary>
public sealed record ResetInductionRequest(string Reason);

/// <summary>A completed or in-flight induction in the company view (MC-15).</summary>
public sealed record InductionSummaryDto(
    Guid Id,
    Guid PersonId,
    string PersonName,
    string TemplateName,
    string Status,
    string? CompletionReference,
    DateTimeOffset? CompletedUtc,
    DateTimeOffset? ExpiresUtc,
    bool ConsentGiven,
    int AttemptCount);
