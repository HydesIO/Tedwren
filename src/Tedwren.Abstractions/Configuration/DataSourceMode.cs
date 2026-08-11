namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// Selects which implementation of the shared service interfaces the API resolves at runtime.
/// <see cref="Database"/> is the only supported runtime mode and the default. <see cref="InMemory"/> exists
/// solely as a fast, isolated test double for the automated API tests (no SQL Server required) and must not
/// be used for a real deployment.
/// </summary>
public enum DataSourceMode
{
    /// <summary>Live data served through the configured relational database — the standard, default mode.</summary>
    Database = 0,

    /// <summary>Test-only: deterministic in-memory data. Selected by the API test host, never in production.</summary>
    InMemory = 1,
}
