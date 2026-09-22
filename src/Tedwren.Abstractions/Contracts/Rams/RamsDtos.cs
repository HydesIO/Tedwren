namespace Tedwren.Abstractions.Contracts.Rams;

/// <summary>
/// A RAMS submission for the review queue / list (PRD §8.2). <paramref name="AwaitingHours"/> and
/// <paramref name="Overdue"/> support the "awaiting review for more than 48 hours" view.
/// </summary>
public sealed record RamsSubmissionDto(
    Guid Id,
    Guid FamilyId,
    int Version,
    string Reference,
    string ContractorName,
    string Title,
    Guid? SiteId,
    string? SiteName,
    bool HasFile,
    string Status,
    string? ReviewNote,
    string? ReviewedBy,
    DateTimeOffset? ReviewedUtc,
    DateTimeOffset SubmittedUtc,
    double AwaitingHours,
    bool Overdue,
    bool IsLive = false);

/// <summary>Request to submit a RAMS. Set <see cref="FamilyId"/> to resubmit a new version of an existing RAMS.</summary>
public sealed record SubmitRamsRequest(
    string ContractorName,
    string Title,
    Guid? SiteId,
    string? SiteName,
    string? FileBase64,
    string? FileContentType,
    Guid? FamilyId);

/// <summary>Request to reject or return a RAMS, with the required written note (PRD §8.2).</summary>
public sealed record ReviewRamsRequest(string Note);

/// <summary>
/// Registers a RAMS submission from an already-stored document (spec Stage 2→3): the subcontractor uploaded its
/// RAMS on the onboarding link and it is bridged into the review queue, reusing the stored file reference rather
/// than re-uploading it. Set <see cref="FamilyId"/> to resubmit a new version into the same family.
/// </summary>
public sealed record RegisterRamsFromDocumentRequest(
    string ContractorName, string Title, string? FileReference, Guid? FamilyId, Guid? SiteId, string? SiteName);

/// <summary>
/// The current live (approved) RAMS an operative must read and sign before starting (Gate 5). <see cref="HasFile"/>
/// flags a stored document — the bytes are served only through the authorised file route, never a public URL (R9).
/// </summary>
public sealed record LiveRamsDto(
    Guid Id,
    Guid FamilyId,
    int Version,
    string Title,
    string Reference,
    string ContractorName,
    bool HasFile,
    string Status,
    DateTimeOffset SubmittedUtc);

/// <summary>An operative's request to sign a specific live RAMS submission (Gate 5).</summary>
public sealed record SignRamsRequest(Guid RamsSubmissionId, string SignatureName);

/// <summary>The recorded operative RAMS signature (Gate 5); <see cref="ExpiresUtc"/> is the re-sign deadline under the MC's review cycle, or null.</summary>
public sealed record RamsAcknowledgementDto(
    Guid Id,
    Guid FamilyId,
    int Version,
    DateTimeOffset SignedUtc,
    DateTimeOffset? ExpiresUtc);
