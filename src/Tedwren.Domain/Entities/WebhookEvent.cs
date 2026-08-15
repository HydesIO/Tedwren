using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A stored inbound GoCardless webhook event. Persisted once per GoCardless event id (deduped) so processing
/// is idempotent and the admin area can show every event and its outcome. The raw JSON is kept so an event
/// can be re-examined or reprocessed. Events are platform-level (not tenant-scoped): the correlation to a
/// company is via the referenced mandate/payment.
/// </summary>
public sealed class WebhookEvent
{
    /// <summary>Stable local identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The GoCardless event id — unique; the dedupe key so the same event is never processed twice.</summary>
    public required string GoCardlessEventId { get; init; }

    /// <summary>The GoCardless resource type the event concerns (e.g. "mandates", "payments").</summary>
    public required string ResourceType { get; init; }

    /// <summary>The GoCardless action (e.g. "active", "failed", "cancelled", "confirmed").</summary>
    public required string Action { get; init; }

    /// <summary>The GoCardless id of the affected resource (mandate/payment), if present.</summary>
    public string? ResourceId { get; init; }

    /// <summary>The processing outcome.</summary>
    public WebhookProcessingOutcome Outcome { get; set; } = WebhookProcessingOutcome.Pending;

    /// <summary>A short note about the outcome (e.g. the mapped status, a failure reason, or why it was ignored).</summary>
    public string? Detail { get; set; }

    /// <summary>The raw event JSON as received, for audit and reprocessing.</summary>
    public required string RawJson { get; init; }

    /// <summary>When the event was received (UTC).</summary>
    public DateTimeOffset ReceivedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the event was processed (UTC), if it has been.</summary>
    public DateTimeOffset? ProcessedUtc { get; set; }
}
