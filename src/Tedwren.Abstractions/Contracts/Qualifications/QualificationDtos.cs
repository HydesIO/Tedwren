using Tedwren.Abstractions.Common;

namespace Tedwren.Abstractions.Contracts.Qualifications;

/// <summary>A qualification-type library row (SF-12) with the count of operatives currently holding it.</summary>
public sealed record QualificationTypeDto(
    Guid Id,
    string Name,
    string? Category,
    string? Issuer,
    int DefaultValidityMonths,
    bool IsCscsVerifiable,
    int HeldBy);

/// <summary>
/// A qualification card held by a person, with the <b>status computed server-side from the expiry
/// date</b> (SF-8) and the assurance state (SF-7). <see cref="State"/>/<see cref="StatusLabel"/> drive
/// the traffic-light pill; <see cref="VerificationState"/>/<see cref="VerificationLabel"/> the distinct
/// read/checked/verified badge.
/// </summary>
public sealed record QualificationCardDto(
    Guid Id,
    Guid PersonId,
    Guid QualificationTypeId,
    string QualificationName,
    string? Issuer,
    string? CardNumber,
    string? HolderName,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    ComplianceState State,
    string StatusLabel,
    CardVerificationState VerificationState,
    string VerificationLabel,
    bool NeedsReview,
    string? ConfirmedBy,
    DateTimeOffset? ConfirmedUtc,
    bool IsSuperseded);

/// <summary>
/// Request to capture a new card for a person (SF-5). <see cref="NeedsReview"/> carries the reading
/// confidence — an uncertain read is captured for confirmation, never accepted silently.
/// </summary>
public sealed record CaptureCardRequest(
    Guid PersonId,
    Guid QualificationTypeId,
    string? CardNumber,
    string? HolderName,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    bool NeedsReview,
    string? ImageReference = null);

/// <summary>Request to confirm a card by a named person (SF-6).</summary>
public sealed record ConfirmCardRequest(Guid CardId, string ConfirmedBy);

/// <summary>Request to renew a card, creating a new record that supersedes the old one (SF-10).</summary>
public sealed record RenewCardRequest(
    Guid CardId,
    string? CardNumber,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn);

/// <summary>The qualifications a person's trade requires but they do not currently hold (SF-11).</summary>
public sealed record CompetencyShortfallDto(
    Guid PersonId,
    string Trade,
    IReadOnlyList<string> MissingQualifications);
