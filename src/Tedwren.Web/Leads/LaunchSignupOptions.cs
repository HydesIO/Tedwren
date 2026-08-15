namespace Tedwren.Web.Leads;

/// <summary>
/// Options for launch-list capture (Web Content Spec §6.9), bound from the "LaunchSignup" configuration
/// section. The marketing site forwards captured emails to the API's anonymous launch-list endpoint at
/// <see cref="ApiBaseUrl"/>; when unset, the sink logs and no-ops so the site still runs standalone.
/// </summary>
public sealed class LaunchSignupOptions
{
    /// <summary>Configuration section name this options type binds from.</summary>
    public const string SectionName = "LaunchSignup";

    /// <summary>Base URL of the Tedwren API (e.g. "https://api.tedwren.co.uk"). Empty disables forwarding.</summary>
    public string? ApiBaseUrl { get; set; }
}
