using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A hazard / near-miss report raised from site (PRD §8.2). A worker reports what they saw — with an optional photo
/// and location — and a responsible person triages it (assign → close). Scoped to the reporting company (R15);
/// closed reports are retained as leading-indicator evidence (append-only close-out, R4/R16).
/// </summary>
public sealed class HazardReport
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company the report belongs to (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>Human-readable reference given at report time.</summary>
    public required string Reference { get; init; }

    /// <summary>What kind of observation this is (near-miss, hazard, unsafe act/condition).</summary>
    public HazardKind Kind { get; init; } = HazardKind.NearMiss;

    /// <summary>What was observed.</summary>
    public required string Description { get; init; }

    /// <summary>Free-text location (e.g. "Level 3, east core"); the phone may also capture coordinates (SF-14).</summary>
    public string? Location { get; init; }

    /// <summary>Latitude captured from the reporter's device, if permitted.</summary>
    public double? Latitude { get; init; }

    /// <summary>Longitude captured from the reporter's device, if permitted.</summary>
    public double? Longitude { get; init; }

    /// <summary>Reference to an uploaded photo (via the image store; no permanent public URL, R9).</summary>
    public string? PhotoReference { get; init; }

    /// <summary>The reporter's assessed severity/potential.</summary>
    public SafetySeverity Severity { get; init; } = SafetySeverity.Medium;

    /// <summary>An optional category (e.g. "Working at height", "Housekeeping").</summary>
    public string? Category { get; set; }

    /// <summary>The triage state.</summary>
    public HazardStatus Status { get; set; } = HazardStatus.Open;

    /// <summary>Who the report is assigned to for action.</summary>
    public string? AssignedTo { get; set; }

    /// <summary>Who reported it.</summary>
    public required string ReportedBy { get; init; }

    /// <summary>When it was reported (UTC; displayed in UK local time, R11).</summary>
    public DateTimeOffset ReportedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When it was closed out (UTC), if closed.</summary>
    public DateTimeOffset? ClosedUtc { get; set; }

    /// <summary>The closure note recording what was done.</summary>
    public string? ClosureNote { get; set; }
}
