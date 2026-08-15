namespace Tedwren.Domain.Entities;

/// <summary>
/// An entry in a lead's activity log (Web Plan §7): what was done, by whom and when. Also records status
/// changes (as an automatic note) so the pipeline history is a single readable timeline. Append-only — notes
/// are never edited or deleted, so the record of what happened stays trustworthy.
/// </summary>
public sealed class LeadNote
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The lead this note belongs to.</summary>
    public required Guid LeadId { get; init; }

    /// <summary>Who logged the note (the acting product owner).</summary>
    public string? Author { get; init; }

    /// <summary>The note body.</summary>
    public required string Body { get; init; }

    /// <summary>An optional short label for a status change this note records (e.g. "Qualified → Proposal").</summary>
    public string? StatusChange { get; init; }

    /// <summary>When the note was logged (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
