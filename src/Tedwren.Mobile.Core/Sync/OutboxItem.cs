namespace Tedwren.Mobile.Core.Sync;

/// <summary>The lifecycle state of an outbox item (M5).</summary>
public enum OutboxItemStatus
{
    /// <summary>Captured, waiting for its first sync attempt.</summary>
    Pending = 0,

    /// <summary>A transient failure; will be retried after <see cref="OutboxItem.NextAttemptUtc"/>.</summary>
    Failed = 1,

    /// <summary>A permanent failure or the retry limit was hit — surfaced to the operative, never silently dropped.</summary>
    NeedsAttention = 2,

    /// <summary>Successfully synced.</summary>
    Done = 3,
}

/// <summary>
/// One queued offline capture (M5) — an append-only unit of work the sync engine drains in order. Held in the
/// encrypted local store so the photo bytes are encrypted at rest (R13-adjacent; the device is UK-held). The
/// <see cref="Id"/> is the device-generated client id that doubles as the server idempotency key (R4/R16);
/// <see cref="UploadedImageReference"/> is the upload checkpoint so a retry after a successful photo upload never
/// re-uploads. Times are UTC (R11).
/// </summary>
public sealed class OutboxItem
{
    /// <summary>Client-generated id / server idempotency key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Monotonic append order, assigned by the store; the drain is ordered by this.</summary>
    public long Sequence { get; set; }

    /// <summary>Handler discriminator (e.g. "evidence", "hazard-report").</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>The serialized typed request (photo bytes excluded — those ride in <see cref="PhotoBytes"/>).</summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>The captured photo, if any (encrypted at rest). Cleared once uploaded.</summary>
    public byte[]? PhotoBytes { get; set; }

    /// <summary>The captured photo's content type.</summary>
    public string? PhotoContentType { get; set; }

    /// <summary>The image-store reference once the photo is uploaded — the checkpoint that makes a retry skip re-upload.</summary>
    public string? UploadedImageReference { get; set; }

    /// <summary>The current lifecycle state.</summary>
    public OutboxItemStatus Status { get; set; } = OutboxItemStatus.Pending;

    /// <summary>How many sync attempts have been made.</summary>
    public int AttemptCount { get; set; }

    /// <summary>The earliest time the next attempt may run (backoff); null means "as soon as possible".</summary>
    public DateTimeOffset? NextAttemptUtc { get; set; }

    /// <summary>The last error message, for the "needs attention" surface.</summary>
    public string? LastError { get; set; }

    /// <summary>When the capture was queued (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When it successfully synced (UTC), if done.</summary>
    public DateTimeOffset? CompletedUtc { get; set; }
}
