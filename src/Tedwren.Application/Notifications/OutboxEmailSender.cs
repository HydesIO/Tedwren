using Tedwren.Abstractions.Notifications;

namespace Tedwren.Application.Notifications;

/// <summary>
/// Stub <see cref="IEmailSender"/> that records to the <see cref="INotificationOutbox"/> instead of sending a
/// real email. The default sender until a real provider is wired in PRD-Phase 7.
/// </summary>
public sealed class OutboxEmailSender : IEmailSender
{
    private readonly INotificationOutbox _outbox;

    /// <summary>Creates the sender over the outbox.</summary>
    public OutboxEmailSender(INotificationOutbox outbox) => _outbox = outbox;

    /// <summary>Records the email to the outbox.</summary>
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        _outbox.Record(new OutboxMessage("Email", toEmail, subject, body, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }
}
