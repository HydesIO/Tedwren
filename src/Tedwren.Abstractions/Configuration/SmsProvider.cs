namespace Tedwren.Abstractions.Configuration;

/// <summary>How platform SMS (SF-9 worker expiry warnings and onboarding links) is delivered.</summary>
public enum SmsProvider
{
    /// <summary>Record to the notification outbox; nothing is sent. The default until a real provider is configured.</summary>
    Outbox = 0,

    /// <summary>Deliver through the Twilio HTTP API.</summary>
    Twilio = 1,
}
