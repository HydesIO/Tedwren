namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// Strongly-typed binding for the "GoCardless" configuration section — the direct-debit provider used to
/// collect the metered SaaS fee (proposed PRD §9 addition). Modeled on <see cref="EmailOptions"/>: mutable
/// properties with safe defaults, overridden from configuration. When no <see cref="AccessToken"/> is set the
/// integration is inactive: read surfaces still work from the local database, but collection actions report
/// that GoCardless is not configured rather than calling out.
/// </summary>
/// <remarks>
/// The access token and webhook secret are secrets. They are read from configuration for convenience, but a
/// real deployment must source them from a secret store / environment variable — never commit a live token.
/// The default <see cref="ApiBaseUrl"/> points at the GoCardless <b>sandbox</b>, so nothing touches live money
/// until it is deliberately switched to the live host with live credentials.
/// </remarks>
public sealed class GoCardlessOptions
{
    /// <summary>Name of the configuration section this type binds to.</summary>
    public const string SectionName = "GoCardless";

    /// <summary>The GoCardless access token (Bearer). Empty leaves the integration inactive.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Base address of the GoCardless HTTP API. Defaults to the sandbox host.</summary>
    public string ApiBaseUrl { get; set; } = "https://api-sandbox.gocardless.com";

    /// <summary>The GoCardless API version header value sent on every request.</summary>
    public string ApiVersion { get; set; } = "2015-07-06";

    /// <summary>Shared secret used to verify the <c>Webhook-Signature</c> HMAC on inbound webhooks (Phase C).</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Where GoCardless returns the payer after the hosted mandate-authorisation flow completes.</summary>
    public string RedirectUri { get; set; } = "https://localhost:11379/admin/billing";

    /// <summary>Where GoCardless returns the payer if they exit the hosted flow without completing it.</summary>
    public string ExitUri { get; set; } = "https://localhost:11379/admin/billing";
}
