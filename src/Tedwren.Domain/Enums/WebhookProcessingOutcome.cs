namespace Tedwren.Domain.Enums;

/// <summary>
/// The result of processing an inbound GoCardless webhook event. Stored on the event so the admin area can
/// show what happened to each one, and so reconciliation/reprocessing can find events that did not apply.
/// </summary>
public enum WebhookProcessingOutcome
{
    /// <summary>Received and stored, not yet processed.</summary>
    Pending = 0,

    /// <summary>Applied — the referenced mandate/payment was updated.</summary>
    Applied = 1,

    /// <summary>Ignored — not relevant, or the referenced resource is unknown locally.</summary>
    Ignored = 2,

    /// <summary>Processing failed and may be retried by reconciliation.</summary>
    Failed = 3,
}
