namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// The client-side half of the data-source switch, bound from the Blazor client's
/// <c>wwwroot/appsettings.json</c> "DataSource:Mode". <see cref="Mock"/> uses in-proc sample data
/// (today's behaviour); <see cref="Api"/> calls the Web API. Switching is a configuration change only
/// — no component or page changes.
/// </summary>
public enum ClientDataSourceMode
{
    /// <summary>In-proc sample data — the default.</summary>
    Mock = 0,

    /// <summary>Data served by the Tedwren Web API over HTTP.</summary>
    Api = 1,
}
