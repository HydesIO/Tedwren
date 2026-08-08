namespace Tedwren.Abstractions.Contracts.Expiry;

/// <summary>The outcome of an expiry-warning scan (SF-9): how many cards were evaluated and warnings sent.</summary>
public sealed record ExpiryScanResultDto(int CardsEvaluated, int NotificationsSent);

/// <summary>The outcome of a weekly digest run (SUB-5): companies processed and digest emails sent.</summary>
public sealed record DigestResultDto(int CompaniesProcessed, int EmailsSent);

/// <summary>The outcome of a job heartbeat check (R12): how many jobs were flagged as overdue.</summary>
public sealed record HeartbeatResultDto(int AlertsRaised);

/// <summary>A card approaching or past expiry, for the upcoming-expiries read endpoint.</summary>
public sealed record UpcomingExpiryDto(
    Guid CardId,
    Guid PersonId,
    string QualificationName,
    DateOnly? ExpiresOn,
    int DaysUntilExpiry,
    string StatusLabel);

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
