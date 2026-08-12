using System.Text;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Notifications;

namespace Tedwren.Application.Notifications.Email;

/// <summary>
/// Default <see cref="IEmailTemplateRenderer"/>: composes message content into the branded Tedwren shell via
/// <see cref="EmailLayout"/>. Branding (logo URL, footer legal text) is supplied by the injected
/// <see cref="EmailOptions"/>, so the same renderer serves every deployment from configuration.
/// </summary>
public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly EmailOptions _options;

    /// <summary>Creates the renderer over the resolved email/branding options.</summary>
    public EmailTemplateRenderer(EmailOptions options) => _options = options;

    /// <summary>Wraps plain text in the branded template, splitting blank-line blocks into paragraphs.</summary>
    public string RenderPlainText(string subject, string bodyText)
    {
        var content = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(subject))
        {
            content.Append(EmailComponents.Heading(subject));
        }

        foreach (var block in SplitIntoBlocks(bodyText))
        {
            content.Append(EmailComponents.Paragraph(block));
        }

        return EmailLayout.Render(_options, subject, content.ToString());
    }

    /// <summary>Wraps already-composed content HTML in the branded template.</summary>
    public string Render(string subject, string contentHtml, string? preheader = null) =>
        EmailLayout.Render(_options, subject, contentHtml, preheader);

    /// <summary>Splits a plain-text body into paragraph blocks on blank lines; single newlines stay within a block.</summary>
    private static IEnumerable<string> SplitIntoBlocks(string? bodyText)
    {
        if (string.IsNullOrWhiteSpace(bodyText))
        {
            yield break;
        }

        var normalised = bodyText.Replace("\r\n", "\n").Trim();
        foreach (var block in normalised.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return block;
        }
    }
}
