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

    /// <summary>Records the HTML email to the outbox (the inner content fragment, unwrapped — this is a test double).</summary>
    public Task SendHtmlAsync(string toEmail, string subject, string contentHtml, CancellationToken cancellationToken = default)
    {
        _outbox.Record(new OutboxMessage("Email", toEmail, subject, contentHtml, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }

    /// <summary>Records the HTML email plus a note of its attachments to the outbox (test double).</summary>
    public Task SendHtmlWithAttachmentsAsync(string toEmail, string subject, string contentHtml,
        IReadOnlyList<EmailAttachment> attachments, CancellationToken cancellationToken = default)
    {
        var names = attachments.Count == 0 ? string.Empty : " [attachments: " + string.Join(", ", attachments.Select(a => a.FileName)) + "]";
        _outbox.Record(new OutboxMessage("Email", toEmail, subject, contentHtml + names, DateTimeOffset.UtcNow));
        return Task.CompletedTask;
    }
}
