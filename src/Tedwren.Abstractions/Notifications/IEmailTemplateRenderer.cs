namespace Tedwren.Abstractions.Notifications;

/// <summary>
/// Renders the branded, HTML-email-compliant Tedwren template (logo header, content container, private &amp;
/// confidential footer) around message content. It is the single place notification content becomes a full
/// HTML document, so both the existing plain-text jobs (SF-9, SUB-5, R12) and richer component-based emails
/// share one consistent look. The delivery provider (<see cref="IEmailSender"/>) calls this before sending.
/// </summary>
public interface IEmailTemplateRenderer
{
    /// <summary>
    /// Wraps a plain-text <paramref name="bodyText"/> in the branded template, turning blank-line-separated
    /// blocks into paragraphs and single newlines into line breaks. Used for the existing jobs whose bodies
    /// are plain strings.
    /// </summary>
    string RenderPlainText(string subject, string bodyText);

    /// <summary>
    /// Wraps already-composed inner <paramref name="contentHtml"/> (typically built from the component kit) in
    /// the branded template. <paramref name="preheader"/> sets the inbox-preview snippet; when null the subject
    /// is used.
    /// </summary>
    string Render(string subject, string contentHtml, string? preheader = null);
}
