namespace Tedwren.Domain.Entities;

/// <summary>
/// A document distributed to a workforce audience for acknowledgement (PRD §8.2) — a toolbox talk, safety alert,
/// site rules, method statement or policy. Each recipient's receipt is recorded as a
/// <see cref="DocumentAcknowledgement"/>, and the completion matrix (who has / has not signed) becomes a further
/// proof point in the compliance pack. Scoped to the distributing company (R15).
/// </summary>
public sealed class DocumentDistribution
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company that distributed the document.</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The document title.</summary>
    public required string Title { get; set; }

    /// <summary>The document category (e.g. Toolbox Talk, Safety Alert, Site Rules, Policy).</summary>
    public string? Category { get; set; }

    /// <summary>A description of who it was sent to (e.g. "All operatives", "Site: Meridian", "Trade: Groundworkers").</summary>
    public string? Audience { get; set; }

    /// <summary>Reference to the uploaded document (via the image/file store; no permanent public URL, R9).</summary>
    public string? FileReference { get; set; }

    /// <summary>Who sent it.</summary>
    public required string SentBy { get; set; }

    /// <summary>When it was sent (UTC; displayed in UK local time, R11).</summary>
    public DateTimeOffset SentUtc { get; init; } = DateTimeOffset.UtcNow;
}
