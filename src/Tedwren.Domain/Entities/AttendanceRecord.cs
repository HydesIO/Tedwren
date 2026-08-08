using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// One attendance event — a sign-in, sign-out or overnight flag. The attendance log is <b>append-only</b>:
/// records are added to, never edited or deleted, and a correction is a new record referencing the original
/// (R4). Times are stored as unambiguous instants (UTC) and displayed in UK local time (R11). Every attempt
/// is recorded, including refusals (SF-16), so a worker who turned up with a lapsed card or outside the
/// boundary leaves a trace.
/// </summary>
public sealed class AttendanceRecord
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The worker.</summary>
    public required Guid PersonId { get; init; }

    /// <summary>The site.</summary>
    public required Guid SiteId { get; init; }

    /// <summary>The specific property within a dispersed scheme, if applicable (SF-26).</summary>
    public Guid? PropertyId { get; init; }

    /// <summary>The event kind.</summary>
    public required AttendanceEventType Type { get; init; }

    /// <summary>The outcome of the attempt.</summary>
    public required AttendanceOutcome Outcome { get; init; }

    /// <summary>How the worker reached the confirmation (SF-13/SF-25).</summary>
    public SignInMethod Method { get; init; } = SignInMethod.QrScan;

    /// <summary>Captured latitude, if a location was supplied.</summary>
    public double? Latitude { get; init; }

    /// <summary>Captured longitude, if a location was supplied.</summary>
    public double? Longitude { get; init; }

    /// <summary>Whether the captured location was inside the boundary; null when no location was supplied.</summary>
    public bool? WithinBoundary { get; init; }

    /// <summary>Why the attempt was refused or flagged, in words a worker can act on.</summary>
    public string? Reason { get; init; }

    /// <summary>The record this one corrects/references, if it is a correction or flag (R4).</summary>
    public Guid? CorrectsRecordId { get; init; }

    /// <summary>When the event occurred (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset OccurredUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the record was written (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Whether this event contributes to presence (accepted or flagged sign-in/out; refusals do not).</summary>
    public bool CountsForPresence => Outcome is AttendanceOutcome.Accepted or AttendanceOutcome.Flagged;
}
