namespace Tedwren.Mobile.Core.Configuration;

/// <summary>App configuration for reaching the Tedwren Web API from the mobile client.</summary>
public sealed class TedwrenApiOptions
{
    /// <summary>
    /// The API root URL (e.g. <c>https://api.tedwren.example/</c>). A trailing slash is required so relative
    /// request URIs ("api/auth/login") resolve correctly against the <see cref="System.Net.Http.HttpClient"/>
    /// base address.
    /// </summary>
    public required string BaseUrl { get; init; }
}
