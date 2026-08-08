using Tedwren.Abstractions.Common;

namespace Tedwren.Abstractions.Contracts.CompliancePacks;

/// <summary>One card in a pack snapshot (fixed at send, R7).</summary>
public sealed record PackCardDto(
    string QualificationName,
    string StatusLabel,
    ComplianceState State,
    DateOnly? ExpiresOn,
    string VerificationLabel);

/// <summary>One operative in a pack snapshot.</summary>
public sealed record PackSubjectDto(Guid PersonId, string Name, IReadOnlyList<PackCardDto> Cards);

/// <summary>A readiness problem surfaced before send (SUB-14).</summary>
public sealed record ReadinessIssueDto(string SubjectName, string QualificationName, bool Blocking, string Detail);

/// <summary>A request to build/send a pack. Contents are chosen, not automatic (SUB-13).</summary>
public sealed record BuildPackRequest(
    Guid CompanyId,
    string Title,
    string CreatedBy,
    Guid? SiteId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    IReadOnlyList<Guid> OperativeIds,
    string? Passcode,
    int? ExpiryDays,
    bool AcknowledgeIssues,
    Guid? SupersedesPackId);

/// <summary>The result of a build attempt: either blocked by unacknowledged readiness issues, or sent (SUB-14).</summary>
public sealed record BuildPackResult(
    bool Sent,
    Guid? PackId,
    string? Token,
    string? Passcode,
    IReadOnlyList<ReadinessIssueDto> Issues);

/// <summary>A pack in the sender's list, with its status and open/download tallies (SUB-20).</summary>
public sealed record PackListItemDto(
    Guid Id,
    string Title,
    string? SiteName,
    string Status,
    string CreatedBy,
    DateTimeOffset CreatedUtc,
    DateTimeOffset ExpiresUtc,
    int SubjectCount,
    int OpenedCount,
    int DownloadedCount);

/// <summary>The recipient-facing pack view — the fixed snapshot, served with no account (R8).</summary>
public sealed record CompliancePackViewDto(
    Guid Id,
    string Title,
    string? SiteName,
    DateOnly? FromDate,
    DateOnly? ToDate,
    DateTimeOffset ExpiresUtc,
    IReadOnlyList<PackSubjectDto> Subjects);

/// <summary>The outcome of a recipient access attempt (view/download): granted with a reason when refused.</summary>
public sealed record PackAccessResult(bool Granted, string? Reason, CompliancePackViewDto? Pack);
