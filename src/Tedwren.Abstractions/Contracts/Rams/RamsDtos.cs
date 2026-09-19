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
    bool Overdue);

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
