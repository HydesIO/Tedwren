using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Notifications;

namespace Tedwren.Application.Notifications;

/// <summary>
/// Real <see cref="ISmsSender"/> that delivers worker SMS (SF-9 expiry warnings and onboarding links) through the
/// Twilio HTTP API. Registered as a typed <c>HttpClient</c> whose base address and HTTP basic-auth credentials
/// are configured by the composition root from <see cref="SmsOptions"/> — mirroring how <see cref="ResendEmailSender"/>
/// is wired. The default remains the no-op <see cref="OutboxSmsSender"/> until Twilio is configured, so the calling
/// jobs are identical whether or not a provider is present.
/// </summary>
public sealed class TwilioSmsSender : ISmsSender
{
    private readonly HttpClient _http;
    private readonly SmsOptions _options;

    /// <summary>Creates the sender over the typed HTTP client and the SMS options.</summary>
    public TwilioSmsSender(HttpClient http, SmsOptions options)
    {
        _http = http;
        _options = options;
    }

    /// <summary>POSTs the message to Twilio's <c>Messages</c> endpoint (form-encoded), throwing on a non-success response.</summary>
    public async Task SendAsync(string toNumber, string message, CancellationToken cancellationToken = default)
    {
        var path = $"2010-04-01/Accounts/{_options.AccountSid}/Messages.json";
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("To", toNumber),
            new KeyValuePair<string, string>("From", _options.FromNumber),
            new KeyValuePair<string, string>("Body", message),
        });
        using var response = await _http.PostAsync(path, content, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
