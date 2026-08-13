namespace Tedwren.Web.Leads;

/// <summary>
/// The hidden anti-bot fields every public form carries (Web Plan §7). A honeypot field that must stay
/// empty catches naive bots, and a render timestamp catches submits that arrive impossibly fast. These
/// sit alongside the antiforgery token and rate limiting, not instead of them.
/// </summary>
public interface IAntiBotFields
{
    /// <summary>Honeypot: hidden from humans via CSS; a non-empty value means a bot filled it.</summary>
    string? Website { get; set; }

    /// <summary>Unix milliseconds the form was rendered, used for the minimum fill-time check.</summary>
    long RenderedAtUnixMs { get; set; }
}
