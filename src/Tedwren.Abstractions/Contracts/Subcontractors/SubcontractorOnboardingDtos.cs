namespace Tedwren.Abstractions.Contracts.Subcontractors;

/// <summary>
/// One required-document heading selection made in the wizard: the heading and whether it blocks the
/// subcontractor's operatives from starting work until it is present and valid (the Gate 1 set; spec §5).
/// </summary>
public sealed record RequiredDocumentSelection(string Heading, bool RequiredBeforeWork);

/// <summary>
/// Main-contractor request to set up & configure a subcontractor and issue its onboarding link (Subcontractor
/// Onboarding spec Stage 1 / §4). Creating it also creates the subcontractor company record and a tokenised
/// invite the subcontractor uses to upload its documents.
/// </summary>
public sealed record SetupSubcontractorRequest(
    string CompanyName,
    string? Trade,
    string? RegistrationNumber,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    int AccessPeriodMonths,
    IReadOnlyList<RequiredDocumentSelection> RequiredDocuments,
    bool SsstsRequired,
    bool SmstsRequired,
    int InductionValidityDays,
    int InductionPassMark,
    int InductionAttemptLimit,
    int? RamsReviewCycleMonths,
    bool RequirePasscode);

/// <summary>
/// The result of setting up a subcontractor: the shareable onboarding link (token, one-time passcode if one was
/// required, and expiry) plus the created ids. The main contractor shares the link with the subcontractor's
/// primary contact (real email/SMS delivery is PRD-Phase 7; the copyable link is the hand-off today).
/// </summary>
public sealed record SubcontractorOnboardingResultDto(
    string Token,
    string? Passcode,
    DateTimeOffset ExpiresUtc,
    Guid SubcontractorCompanyId,
    Guid TradeInviteId);

/// <summary>
/// One required-before-work document heading's Gate 1 status: whether a valid document satisfies it, and (when
/// not) why (spec §2/§5). <paramref name="Issue"/> is "Not uploaded" or "Expired" when unsatisfied, else null.
/// </summary>
public sealed record Gate1RequirementDto(string Heading, bool Satisfied, string? Issue);

/// <summary>
/// The Gate 1 result for a subcontractor (spec §2): <paramref name="Cleared"/> is true only when every
/// "required before work" document heading is satisfied (present and unexpired). Empty requirements clear
/// vacuously. Cleared unlocks adding operatives.
/// </summary>
public sealed record Gate1StatusDto(bool Cleared, IReadOnlyList<Gate1RequirementDto> Requirements);

/// <summary>A subcontractor's stored onboarding configuration (spec Stage 1 / §4).</summary>
public sealed record SubcontractorOnboardingConfigDto(
    Guid Id,
    Guid SubcontractorCompanyId,
    Guid TradeInviteId,
    int AccessPeriodMonths,
    IReadOnlyList<RequiredDocumentSelection> RequiredDocuments,
    bool SsstsRequired,
    bool SmstsRequired,
    int InductionValidityDays,
    int InductionPassMark,
    int InductionAttemptLimit,
    int? RamsReviewCycleMonths);
