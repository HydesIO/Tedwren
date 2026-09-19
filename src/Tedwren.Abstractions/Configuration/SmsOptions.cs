namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// Strongly-typed binding for the "Sms" configuration section. Decides how platform SMS is delivered — the SF-9
/// worker expiry warnings and the onboarding links that are the natural route to a worker's phone. Modeled on
/// <see cref="EmailOptions"/>: it defaults to the no-op outbox so nothing sends until a real provider is
/// configured, and the credentials are secrets that belong in the environment / a secret store, never in source.
/// </summary>
public sealed class SmsOptions
{
    /// <summary>Name of the configuration section this type binds to.</summary>
    public const string SectionName = "Sms";

    /// <summary>Which delivery mechanism to use. Defaults to <see cref="SmsProvider.Outbox"/> so nothing sends until configured.</summary>
    public SmsProvider Provider { get; set; } = SmsProvider.Outbox;

    /// <summary>Base address of the Twilio HTTP API.</summary>
    public string ApiBaseUrl { get; set; } = "https://api.twilio.com";

    /// <summary>Twilio Account SID (the HTTP basic-auth username). Required when <see cref="Provider"/> is <see cref="SmsProvider.Twilio"/>.</summary>
    public string AccountSid { get; set; } = string.Empty;

    /// <summary>Twilio auth token (the HTTP basic-auth password). Required when <see cref="Provider"/> is <see cref="SmsProvider.Twilio"/>; treat as a secret.</summary>
    public string AuthToken { get; set; } = string.Empty;

    /// <summary>The sender phone number (E.164) or messaging-service SID that messages are sent from.</summary>
    public string FromNumber { get; set; } = string.Empty;
}
