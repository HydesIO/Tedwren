namespace Tedwren.Abstractions.Notifications;

/// <summary>
/// Sends an email to an administrator/manager (SF-9), ops (R12) or a console user (invites). A provider
/// interface so the delivery mechanism is pluggable: a stub records to an outbox; the real provider is Resend
/// (PRD-Phase 7). Two entry points: a plain-text body (wrapped in the branded template by the provider) and a
/// pre-composed HTML content fragment for richer messages (buttons, tables, 2FA codes).
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends a plain-text email; the provider wraps <paramref name="body"/> in the branded template.</summary>
    Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a richer email whose inner <paramref name="contentHtml"/> (built from the component kit) the
    /// provider wraps in the branded shell. Use this when the message needs buttons/tables/codes rather than
    /// just paragraphs.
    /// </summary>
    Task SendHtmlAsync(string toEmail, string subject, string contentHtml, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a branded HTML email with one or more file attachments (e.g. a completed form's PDF, requirement 6).
    /// The default implementation ignores the attachments and falls back to <see cref="SendHtmlAsync"/>, so
    /// existing senders keep working; providers that support attachments override this.
    /// </summary>
    Task SendHtmlWithAttachmentsAsync(string toEmail, string subject, string contentHtml,
        IReadOnlyList<EmailAttachment> attachments, CancellationToken cancellationToken = default) =>
        SendHtmlAsync(toEmail, subject, contentHtml, cancellationToken);
}

/// <summary>A file attached to an email — its name, MIME type and bytes.</summary>
public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);
