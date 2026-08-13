namespace Tedwren.Web.Consent;

/// <summary>
/// A visitor's cookie-consent choice (Web Plan §5.5, §8, §12.7). Essential cookies are always allowed;
/// analytics and advertising are separate categories, each off until explicitly granted. <see cref="HasChoice"/>
/// distinguishes "not yet chosen" (show the banner, fire nothing) from an explicit reject.
/// </summary>
/// <param name="Analytics">Whether analytics cookies/scripts are allowed.</param>
/// <param name="Advertising">Whether advertising/cross-site pixels are allowed.</param>
/// <param name="HasChoice">Whether the visitor has made an explicit choice yet.</param>
public sealed record ConsentState(bool Analytics, bool Advertising, bool HasChoice)
{
    /// <summary>The default state before any choice: nothing granted, banner to be shown.</summary>
    public static readonly ConsentState Unset = new(false, false, false);

    /// <summary>Cookie name that stores the consent choice.</summary>
    public const string CookieName = "tedwren_consent";

    /// <summary>Serializes this choice to its cookie value.</summary>
    public string ToCookieValue() => (Analytics, Advertising) switch
    {
        (true, true) => "all",
        (true, false) => "analytics",
        (false, true) => "ads",
        _ => "essential",
    };

    /// <summary>Parses a cookie value into a state. A null/absent value means no choice yet.</summary>
    /// <param name="cookieValue">The stored cookie value, or null when absent.</param>
    public static ConsentState Parse(string? cookieValue) => cookieValue switch
    {
        null => Unset,
        "all" => new ConsentState(true, true, true),
        "analytics" => new ConsentState(true, false, true),
        "ads" => new ConsentState(false, true, true),
        _ => new ConsentState(false, false, true), // "essential" or anything unrecognised → reject
    };
}
