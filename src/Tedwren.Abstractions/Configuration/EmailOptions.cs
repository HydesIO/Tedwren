namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// Strongly-typed binding for the "Email" configuration section (PRD-Phase 7 email delivery). It decides how
/// platform notifications (SF-9 expiry warnings, SUB-5 weekly digest, R12 ops alerts) are delivered and holds
/// the branding used by the shared HTML email template (logo URL, footer legal text). Modeled on
/// <see cref="JwtOptions"/>: mutable properties with safe defaults, overridden from configuration.
/// </summary>
/// <remarks>
/// The API key is read from configuration (<c>appsettings.json</c> "for now" per the requirement); treat it as a
/// secret in any real deployment and move it to a secret store / environment variable before launch.
/// </remarks>
public sealed class EmailOptions
{
    /// <summary>Name of the configuration section this type binds to.</summary>
    public const string SectionName = "Email";

    /// <summary>Which delivery mechanism to use. Defaults to <see cref="EmailProvider.Outbox"/> so nothing sends until Resend is configured.</summary>
    public EmailProvider Provider { get; set; } = EmailProvider.Outbox;

    /// <summary>The Resend API key (Bearer token). Required when <see cref="Provider"/> is <see cref="EmailProvider.Resend"/>.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Base address of the Resend HTTP API.</summary>
    public string ApiBaseUrl { get; set; } = "https://api.resend.com";

    /// <summary>The verified "from" address emails are sent from (e.g. <c>notifications@tedwren.co.uk</c>).</summary>
    public string FromEmail { get; set; } = "notifications@tedwren.co.uk";

    /// <summary>The display name shown alongside <see cref="FromEmail"/>.</summary>
    public string FromName { get; set; } = "Tedwren";

    /// <summary>Optional reply-to address; when empty no reply-to header is set.</summary>
    public string ReplyToEmail { get; set; } = string.Empty;

    /// <summary>
    /// Public base URL of this API as reachable by email clients, used to build the absolute logo URL
    /// (<c>{PublicBaseUrl}/api/email-assets/logo.png</c>). Email clients cannot resolve relative URLs, so this
    /// must be an absolute, internet-reachable origin in production.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "https://localhost:7192";

    /// <summary>Legal entity name shown in the confidential footer.</summary>
    public string CompanyLegalName { get; set; } = "Tedwren Ltd";

    /// <summary>Postal / registration line shown under the legal name in the footer (optional).</summary>
    public string CompanyAddress { get; set; } = string.Empty;

    /// <summary>The private-and-confidential notice shown in the footer of every email.</summary>
    public string ConfidentialityNotice { get; set; } =
        "This email and any attachments are private and confidential and intended solely for the named recipient. " +
        "If you have received it in error, please notify us and delete it — you must not copy, distribute or act on its contents.";
}
