namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// Selects which implementation of the shared service interfaces the backend resolves at runtime. This is
/// the server-side half of the data-source switch. <see cref="Database"/> is the standard mode and the
/// default; <see cref="Mock"/> is deprecated and retained only as a test double. Switching is a
/// configuration change only — no UI or business-logic code changes.
/// </summary>
public enum DataSourceMode
{
    /// <summary>Deprecated: deterministic in-memory data. Retained only for automated tests, not runtime use.</summary>
    Mock = 0,

    /// <summary>Live data served through the configured relational database — the standard, default mode.</summary>
    Database = 1,
}
