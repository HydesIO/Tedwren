using System.Text;

namespace Tedwren.Application.Notifications.Email;

/// <summary>
/// A small fluent builder that assembles <see cref="EmailComponents"/> fragments into the HTML that fills the
/// content container of the branded template. It keeps callers declarative — <c>.Heading(..).Paragraph(..)
/// .Button(..)</c> — and hands the finished inner HTML to <see cref="IEmailTemplateRenderer"/>.
/// </summary>
public sealed class EmailContentBuilder
{
    private readonly StringBuilder _sb = new();

    /// <summary>Appends a section heading (see <see cref="EmailComponents.Heading"/>).</summary>
    public EmailContentBuilder Heading(string text, int level = 2) => Append(EmailComponents.Heading(text, level));

    /// <summary>Appends a body paragraph (see <see cref="EmailComponents.Paragraph"/>).</summary>
    public EmailContentBuilder Paragraph(string text) => Append(EmailComponents.Paragraph(text));

    /// <summary>Appends a call-to-action button (see <see cref="EmailComponents.Button"/>).</summary>
    public EmailContentBuilder Button(string label, string url) => Append(EmailComponents.Button(label, url));

    /// <summary>Appends a verification / 2FA code block (see <see cref="EmailComponents.VerificationCode"/>).</summary>
    public EmailContentBuilder VerificationCode(string code, string? caption = null) =>
        Append(EmailComponents.VerificationCode(code, caption));

    /// <summary>Appends a highlighted callout box (see <see cref="EmailComponents.Callout"/>).</summary>
    public EmailContentBuilder Callout(string text, string? title = null) => Append(EmailComponents.Callout(text, title));

    /// <summary>Appends a data table (see <see cref="EmailComponents.Table"/>).</summary>
    public EmailContentBuilder Table(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows) =>
        Append(EmailComponents.Table(headers, rows));

    /// <summary>Appends a label/value list (see <see cref="EmailComponents.KeyValueRows"/>).</summary>
    public EmailContentBuilder KeyValueRows(IReadOnlyList<KeyValuePair<string, string>> pairs) =>
        Append(EmailComponents.KeyValueRows(pairs));

    /// <summary>Appends a bulleted list (see <see cref="EmailComponents.BulletList"/>).</summary>
    public EmailContentBuilder BulletList(IReadOnlyList<string> items) => Append(EmailComponents.BulletList(items));

    /// <summary>Appends a horizontal divider (see <see cref="EmailComponents.Divider"/>).</summary>
    public EmailContentBuilder Divider() => Append(EmailComponents.Divider());

    /// <summary>Appends vertical whitespace (see <see cref="EmailComponents.Spacer"/>).</summary>
    public EmailContentBuilder Spacer(int heightPx = 16) => Append(EmailComponents.Spacer(heightPx));

    /// <summary>Appends an already-rendered, trusted HTML fragment (advanced use).</summary>
    public EmailContentBuilder Raw(string html) => Append(html);

    /// <summary>Returns the assembled inner HTML for the content container.</summary>
    public string Build() => _sb.ToString();

    /// <summary>Appends a fragment and returns this builder for chaining.</summary>
    private EmailContentBuilder Append(string fragment)
    {
        _sb.Append(fragment);
        return this;
    }
}
