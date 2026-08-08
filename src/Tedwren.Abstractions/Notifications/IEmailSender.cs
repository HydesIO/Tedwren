namespace Tedwren.Abstractions.Notifications;

/// <summary>
/// Sends an email to an administrator/manager (SF-9) or ops (R12). A provider interface so the delivery
/// mechanism is pluggable: a stub records to an outbox now; a real provider is wired in PRD-Phase 7.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends an email with <paramref name="subject"/> and <paramref name="body"/> to <paramref name="toEmail"/>.</summary>
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}
