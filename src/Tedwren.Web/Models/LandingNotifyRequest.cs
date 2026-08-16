using System.ComponentModel.DataAnnotations;
using Tedwren.Web.Leads;

namespace Tedwren.Web.Models;

/// <summary>
/// The pre-launch landing page's email-capture ("notify me") form. Minimal by design — just an email —
/// server-validated and carrying the shared hidden anti-bot fields (honeypot + render timestamp).
/// </summary>
public sealed class LandingNotifyRequest : IAntiBotFields
{
    /// <summary>The email address to notify at launch.</summary>
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? Website { get; set; }

    /// <inheritdoc />
    public long RenderedAtUnixMs { get; set; }
}
