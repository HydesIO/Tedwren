namespace Tedwren.Web.Leads;

/// <summary>
/// Pure anti-bot checks shared by the public forms (Web Plan §7): a honeypot field that must stay empty
/// and a minimum fill-time derived from the render timestamp. Kept side-effect-free so it is unit
/// testable without a web host.
/// </summary>
public static class AntiBot
{
    /// <summary>
    /// Returns true when a submission looks human: the honeypot is empty and the form was on screen for
    /// at least the minimum time (and not for an implausibly long window).
    /// </summary>
    /// <param name="fields">The submitted anti-bot fields.</param>
    /// <param name="nowUnixMs">Current time in Unix milliseconds.</param>
    /// <param name="minSeconds">Minimum plausible fill time in seconds.</param>
    public static bool IsHuman(IAntiBotFields fields, long nowUnixMs, int minSeconds)
    {
        if (!string.IsNullOrEmpty(fields.Website))
        {
            return false; // honeypot filled → bot
        }

        var elapsedMs = nowUnixMs - fields.RenderedAtUnixMs;
        if (elapsedMs < minSeconds * 1000L)
        {
            return false; // submitted too fast → bot
        }

        // Reject a stale render token (older than a day) so a captured form can't be replayed indefinitely.
        const long oneDayMs = 24L * 60 * 60 * 1000;
        return elapsedMs <= oneDayMs;
    }
}
