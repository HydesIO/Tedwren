namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// Selects which implementation of the shared service interfaces the backend resolves at
/// runtime. This is the server-side half of the mock/database switch: <see cref="Mock"/>
/// serves deterministic in-memory data, while <see cref="Database"/> serves data through the
/// Dapper repositories. Switching between the two is a configuration change only — no UI or
/// business-logic code changes.
/// </summary>
public enum DataSourceMode
{
    /// <summary>Deterministic in-memory data — the default, used for demos and automated tests.</summary>
    Mock = 0,

    /// <summary>Live data served through the configured relational database.</summary>
    Database = 1,
}
