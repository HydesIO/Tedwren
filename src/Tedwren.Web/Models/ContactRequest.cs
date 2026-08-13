using System.ComponentModel.DataAnnotations;
using Tedwren.Web.Leads;

namespace Tedwren.Web.Models;

/// <summary>
/// The "Contact us" form model (Web Plan §6.9). Server-validated; the <see cref="Reason"/> routes the
/// message to the right inbox. Includes the hidden anti-bot fields. No calendar link (that's the demo
/// form's job).
/// </summary>
public sealed class ContactRequest : IAntiBotFields
{
    /// <summary>Sender's name.</summary>
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Sender's email address.</summary>
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Reason for contact, which determines routing.</summary>
    [Required]
    public ContactReason Reason { get; set; } = ContactReason.General;

    /// <summary>The message body.</summary>
    [Required, StringLength(4000, MinimumLength = 10)]
    public string Message { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? Website { get; set; }

    /// <inheritdoc />
    public long RenderedAtUnixMs { get; set; }
}
