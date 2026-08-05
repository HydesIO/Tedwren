namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// Strongly-typed binding for the "DataSource" configuration section. It decides, at startup,
/// whether the application serves mock data or database-backed data and, when database-backed,
/// which relational engine is used. Holding this decision in configuration is what allows the
/// mock and database implementations to be swapped without changing the UI or business logic.
/// </summary>
public sealed class BackendOptions
{
    /// <summary>Name of the configuration section this type binds to.</summary>
    public const string SectionName = "DataSource";

    /// <summary>Whether the backend serves mock or database-backed data. Defaults to mock.</summary>
    public DataSourceMode Mode { get; set; } = DataSourceMode.Mock;

    /// <summary>The relational engine used when <see cref="Mode"/> is <see cref="DataSourceMode.Database"/>.</summary>
    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;
}
