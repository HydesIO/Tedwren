namespace Tedwren.Domain.Entities;

/// <summary>
/// An operative's field evidence capture (M5) — a photo, a note, and where/when it was taken. Available to every
/// operative with no module gate (unlike a hazard report). Append-only (R4): captures are added, never edited. The
/// photo lives in the image store, never at a permanent public URL (R9); times are captured in UTC (R11); scoped to
/// the reporting company (R15). The <see cref="Id"/> is the device-generated id (idempotency key), so a retried
/// sync never creates a duplicate (R4/R16).
/// </summary>
public sealed class EvidenceItem
{
    /// <summary>Stable identifier — set from the device's client id so a retried sync is idempotent.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company the capture belongs to (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The operative who captured it.</summary>
    public required Guid PersonId { get; init; }

    /// <summary>An optional free-text note describing what was captured.</summary>
    public string? Note { get; init; }

    /// <summary>Latitude captured from the device, if permitted (SF-14).</summary>
    public double? Latitude { get; init; }

    /// <summary>Longitude captured from the device, if permitted (SF-14).</summary>
    public double? Longitude { get; init; }

    /// <summary>Reference to an uploaded photo (via the image store; no permanent public URL, R9).</summary>
    public string? PhotoReference { get; init; }

    /// <summary>When the evidence was captured on the device (UTC; displayed in UK local time, R11).</summary>
    public DateTimeOffset CapturedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the record was written on the server (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
