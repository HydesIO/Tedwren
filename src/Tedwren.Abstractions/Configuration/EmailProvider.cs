namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// Selects how platform notification emails are delivered. <see cref="Outbox"/> is the safe default: it
/// records messages to the in-memory outbox and sends nothing, so a fresh install (and the automated tests)
/// never contacts a real provider. <see cref="Resend"/> delivers real email through the Resend.com API and is
/// only used once an API key is configured.
/// </summary>
public enum EmailProvider
{
    /// <summary>Default: records to the in-memory outbox instead of sending. No real email leaves the system.</summary>
    Outbox = 0,

    /// <summary>Delivers real email through the Resend.com HTTP API (requires an API key and verified sender).</summary>
    Resend = 1,
}
