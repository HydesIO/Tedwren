using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A structured accident / incident record (PRD §8.2). Captures what happened, any injury, the investigation
/// (causes and corrective actions), a <see cref="RiddorReportable"/> flag with its category, and close-out. Scoped
/// to the company (R15); retained as evidence (append-only close-out, R4/R16).
/// </summary>
public sealed class IncidentReport
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company the record belongs to (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>Human-readable reference given at report time.</summary>
    public required string Reference { get; init; }

    /// <summary>Accident, incident, or dangerous occurrence.</summary>
    public IncidentKind Kind { get; init; } = IncidentKind.Accident;

    /// <summary>What happened.</summary>
    public required string Description { get; init; }

    /// <summary>Where it happened (free text).</summary>
    public string? Location { get; init; }

    /// <summary>When the event occurred (UTC; may pre-date the report).</summary>
    public DateTimeOffset OccurredUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>The injured person's name, if anyone was injured.</summary>
    public string? InjuredPersonName { get; init; }

    /// <summary>The nature of any injury (e.g. "Laceration to left hand").</summary>
    public string? InjuryDetail { get; init; }

    /// <summary>The severity of the event.</summary>
    public SafetySeverity Severity { get; init; } = SafetySeverity.Medium;

    /// <summary>The immediate cause established during investigation.</summary>
    public string? ImmediateCause { get; set; }

    /// <summary>The underlying/root cause established during investigation.</summary>
    public string? RootCause { get; set; }

    /// <summary>The corrective actions agreed to prevent recurrence.</summary>
    public string? CorrectiveActions { get; set; }

    /// <summary>The lifecycle state.</summary>
    public IncidentStatus Status { get; set; } = IncidentStatus.Reported;

    /// <summary>Whether the event is reportable to the HSE under RIDDOR.</summary>
    public bool RiddorReportable { get; set; }

    /// <summary>The RIDDOR category (e.g. "Specified injury", "Over-7-day incapacitation").</summary>
    public string? RiddorCategory { get; set; }

    /// <summary>Who reported it.</summary>
    public required string ReportedBy { get; init; }

    /// <summary>When it was reported (UTC).</summary>
    public DateTimeOffset ReportedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Who investigated it.</summary>
    public string? InvestigatedBy { get; set; }

    /// <summary>When the investigation was closed out (UTC), if closed.</summary>
    public DateTimeOffset? ClosedUtc { get; set; }
}
