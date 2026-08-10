namespace Tedwren.Abstractions.Notifications;

/// <summary>
/// An observable in-memory record of the messages the stub senders "sent", so tests and a dev/mock
/// endpoint can see what would have gone out without a real SMS/email provider. Replaced by real delivery
/// tracking in PRD-Phase 7.
/// </summary>
public interface INotificationOutbox
{
    /// <summary>Records a sent message.</summary>
    void Record(OutboxMessage message);

    /// <summary>The messages recorded so far, oldest first.</summary>
    IReadOnlyList<OutboxMessage> Messages { get; }

    /// <summary>Clears the recorded messages (used by tests).</summary>
    void Clear();
}

/// <summary>A message captured by the outbox. <see cref="Channel"/> is "Sms" or "Email".</summary>
public sealed record OutboxMessage(string Channel, string Recipient, string Subject, string Body, DateTimeOffset SentUtc);
