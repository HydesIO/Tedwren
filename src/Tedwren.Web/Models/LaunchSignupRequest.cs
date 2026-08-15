using System.ComponentModel.DataAnnotations;
using Tedwren.Web.Leads;

namespace Tedwren.Web.Models;

/// <summary>
/// The launch-list signup form model (Web Content Spec §6.9): a visitor leaves just their email to be told
/// when Tedwren launches. Server-validated and carries the hidden anti-bot fields, like the other public
/// forms. The captured email is forwarded to the API's launch-list endpoint via <see cref="ILaunchSignupSink"/>.
/// </summary>
public sealed class LaunchSignupRequest : IAntiBotFields
{
    /// <summary>The email address to add to the launch list.</summary>
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    /// <inheritdoc />
    public string? Website { get; set; }

    /// <inheritdoc />
    public long RenderedAtUnixMs { get; set; }
}
