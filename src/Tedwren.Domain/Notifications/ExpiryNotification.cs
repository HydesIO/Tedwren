namespace Tedwren.Domain.Notifications;

/// <summary>
/// A record that a specific expiry warning has been sent — the idempotency log that stops a warning being
/// sent twice (SF-9: "running the check twice in a day does not send it twice"). Uniqueness is by
/// (<see cref="CardId"/>, <see cref="Stage"/>, <see cref="Channel"/>, <see cref="Recipient"/>).
/// </summary>
public sealed class ExpiryNotification
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The card the warning is about.</summary>
    public required Guid CardId { get; init; }

    /// <summary>The schedule stage this warning represents.</summary>
    public required ExpiryWarningStage Stage { get; init; }

    /// <summary>The channel it was sent on.</summary>
    public required NotificationChannel Channel { get; init; }

    /// <summary>The recipient (worker mobile or administrator email).</summary>
    public required string Recipient { get; init; }

    /// <summary>When it was sent (UTC).</summary>
    public DateTimeOffset SentUtc { get; init; } = DateTimeOffset.UtcNow;
}
