namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// The client-side half of the data-source switch, bound from the Blazor client's
/// <c>wwwroot/appsettings.json</c> "DataSource:Mode". <see cref="Api"/> is the standard mode and the
/// default; <see cref="Mock"/> (in-proc sample data) is deprecated. Switching is a configuration change
/// only — no component or page changes.
/// </summary>
public enum ClientDataSourceMode
{
    /// <summary>Deprecated: in-proc sample data. Not a supported runtime configuration.</summary>
    Mock = 0,

    /// <summary>Data served by the Tedwren Web API over HTTP — the standard, default mode.</summary>
    Api = 1,
}
