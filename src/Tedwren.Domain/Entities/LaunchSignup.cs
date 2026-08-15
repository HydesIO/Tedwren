namespace Tedwren.Domain.Entities;

/// <summary>
/// A launch-list signup: an interested visitor who entered their email on the marketing site's landing page
/// before launch (Web Content Spec §6.9). Held so the product team can announce launch to everyone who asked
/// to be told. Not a tenant record — it belongs to the commercial plane and has no account behind it until the
/// person actually signs up. Deduplicated on <see cref="Email"/> so a repeat submission does not create a
/// second row or a second announcement.
/// </summary>
public sealed class LaunchSignup
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The email address to announce launch to (stored lower-cased for dedupe).</summary>
    public required string Email { get; init; }

    /// <summary>Where the signup came from (e.g. "landing"), for attribution.</summary>
    public string? Source { get; init; }

    /// <summary>Captured UTM source, if the visitor arrived via a campaign link.</summary>
    public string? UtmSource { get; init; }

    /// <summary>Captured UTM medium.</summary>
    public string? UtmMedium { get; init; }

    /// <summary>Captured UTM campaign.</summary>
    public string? UtmCampaign { get; init; }

    /// <summary>When the signup was captured (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Whether the launch announcement has been sent to this address.</summary>
    public bool Notified { get; set; }

    /// <summary>When the launch announcement was sent, if it has been.</summary>
    public DateTimeOffset? NotifiedUtc { get; set; }
}
