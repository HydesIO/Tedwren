using Tedwren.Abstractions.Notifications;

namespace Tedwren.Application.Notifications;

/// <summary>
/// Stub <see cref="ISmsSender"/> that records to the <see cref="INotificationOutbox"/> instead of sending a
/// real SMS. The default sender until a real provider is wired in PRD-Phase 7.
/// </summary>
public sealed class OutboxSmsSender : ISmsSender
{
    private readonly INotificationOutbox _outbox;

    /// <summary>Creates the sender over the outbox.</summary>
    public OutboxSmsSender(INotificationOutbox outbox) => _outbox = outbox;

    /// <summary>Records the SMS to the outbox.</summary>
    public Task SendAsync(string toNumber, string message, CancellationToken cancellationToken = default)
    {
        _outbox.Record(new OutboxMessage("Sms", toNumber, Subject: "SMS", message, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }
}
