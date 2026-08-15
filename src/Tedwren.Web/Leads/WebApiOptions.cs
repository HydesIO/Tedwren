namespace Tedwren.Web.Leads;

/// <summary>
/// Points the marketing site at the Tedwren API, bound from the "Api" configuration section. Used by the
/// server-side seams that forward captured leads to the API's anonymous capture endpoint. When
/// <see cref="BaseUrl"/> is empty the forwarding seams log and no-op, so the site still runs standalone.
/// </summary>
public sealed class WebApiOptions
{
    /// <summary>Configuration section name this options type binds from.</summary>
    public const string SectionName = "Api";

    /// <summary>Base URL of the Tedwren API (e.g. "https://api.tedwren.co.uk"). Empty disables forwarding.</summary>
    public string? BaseUrl { get; set; }
}
