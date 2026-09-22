namespace Tedwren.Abstractions.Contracts.Expiry;

/// <summary>The outcome of an expiry-warning scan (SF-9): how many expiry items (across cards, company documents and
/// inductions) were evaluated and warnings sent.</summary>
public sealed record ExpiryScanResultDto(int ItemsEvaluated, int NotificationsSent);

/// <summary>The outcome of a weekly digest run (SUB-5): companies processed and digest emails sent.</summary>
public sealed record DigestResultDto(int CompaniesProcessed, int EmailsSent);

/// <summary>The outcome of a job heartbeat check (R12): how many jobs were flagged as overdue.</summary>
public sealed record HeartbeatResultDto(int AlertsRaised);

/// <summary>An item approaching or past expiry, for the upcoming-expiries read endpoint — a qualification card, a
/// company document (SUB-4) or an induction (MC-7). <see cref="SubjectId"/> is the underlying record; <see cref="Label"/>
/// is its display name (card type / document name / "Site induction"); <see cref="SourceLabel"/> is the human register
/// name ("Card" / "Company document" / "Induction") for a filterable badge. <see cref="PersonName"/> and
/// <see cref="Slug"/> identify the operative who holds a card/induction so a row can name the person and link to their
/// profile; both are null for a company-level document or when the query runs unscoped.</summary>
public sealed record UpcomingExpiryDto(
    Guid SubjectId,
    Guid? PersonId,
    string Label,
    string SourceLabel,
    DateOnly? ExpiresOn,
    int DaysUntilExpiry,
    string StatusLabel,
    string? PersonName = null,
    string? Slug = null);

/// <summary>A scheduled-job run, for the job-runs read endpoint (SF-21).</summary>
public sealed record JobRunDto(
    Guid Id,
    string JobName,
    DateTimeOffset StartedUtc,
    DateTimeOffset? FinishedUtc,
    string Status,
    int ItemsProcessed,
    int NotificationsSent,
    string? Error);
