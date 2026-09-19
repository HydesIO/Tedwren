namespace Tedwren.Domain.Entities;

/// <summary>
/// A person's hand-arm vibration (HAVs) exposure for a single day (PRD §8.2). The individual tool usages that make
/// up the day are held as JSON (<see cref="ToolUsagesJson"/>) — the daily A(8) exposure, points and band are always
/// derived from them, never stored. Scoped to the company (R15); records are retained as evidence (append-only).
/// </summary>
public sealed class HavsExposureRecord
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company the record belongs to (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The exposed person's name (free text; a roster link is a later refinement).</summary>
    public required string PersonName { get; init; }

    /// <summary>The day the exposure relates to (date-only, R11).</summary>
    public required DateOnly ExposureDate { get; init; }

    /// <summary>The day's tool usages as a JSON array of {ToolName, MagnitudeMs2, TriggerMinutes} (System.Text.Json).</summary>
    public required string ToolUsagesJson { get; init; }

    /// <summary>Who recorded the exposure.</summary>
    public required string RecordedBy { get; init; }

    /// <summary>When it was recorded (UTC; displayed in UK local time, R11).</summary>
    public DateTimeOffset RecordedUtc { get; init; } = DateTimeOffset.UtcNow;
}
