using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Notifications;

namespace Tedwren.Application.Notifications;

/// <summary>
/// Real <see cref="IEmailSender"/> that delivers platform notifications through the Resend.com HTTP API
/// (PRD-Phase 7). It renders the plain-text body into the branded HTML template first, so every notification
/// (SF-9 expiry warnings, SUB-5 digest, R12 ops alerts) is sent as a consistent, professional email without
/// any change to the calling jobs. Registered as a typed <c>HttpClient</c> whose base address and bearer token
/// are configured by the composition root from <see cref="EmailOptions"/>.
/// </summary>
public sealed class ResendEmailSender : IEmailSender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly EmailOptions _options;

    /// <summary>Creates the sender over the typed HTTP client, the template renderer and the email options.</summary>
    public ResendEmailSender(HttpClient http, IEmailTemplateRenderer renderer, EmailOptions options)
    {
        _http = http;
        _renderer = renderer;
        _options = options;
    }

    /// <summary>Renders <paramref name="body"/> into the branded template and POSTs it to Resend for delivery.</summary>
    public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default) =>
        DeliverAsync(toEmail, subject, _renderer.RenderPlainText(subject, body), cancellationToken);

    /// <summary>Wraps the composed <paramref name="contentHtml"/> in the branded shell and POSTs it to Resend.</summary>
    public Task SendHtmlAsync(string toEmail, string subject, string contentHtml, CancellationToken cancellationToken = default) =>
        DeliverAsync(toEmail, subject, _renderer.Render(subject, contentHtml), cancellationToken);

    /// <summary>POSTs a fully-rendered HTML email to Resend, throwing on a non-success response.</summary>
    private async Task DeliverAsync(string toEmail, string subject, string html, CancellationToken cancellationToken)
    {
        var request = new ResendEmailRequest(
            From: FormatFrom(),
            To: new[] { toEmail },
            Subject: subject,
            Html: html,
            ReplyTo: string.IsNullOrWhiteSpace(_options.ReplyToEmail) ? null : _options.ReplyToEmail);

        using var response = await _http.PostAsJsonAsync("emails", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Formats the "from" header as <c>Name &lt;email&gt;</c>, or just the address when no name is set.</summary>
    private string FormatFrom() =>
        string.IsNullOrWhiteSpace(_options.FromName)
            ? _options.FromEmail
            : $"{_options.FromName} <{_options.FromEmail}>";

    /// <summary>The JSON payload Resend's <c>POST /emails</c> expects (snake_case <c>reply_to</c>).</summary>
    private sealed record ResendEmailRequest(
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string[] To,
        [property: JsonPropertyName("subject")] string Subject,
        [property: JsonPropertyName("html")] string Html,
        [property: JsonPropertyName("reply_to")] string? ReplyTo);
}
