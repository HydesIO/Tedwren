using Tedwren.Abstractions.Configuration;

namespace Tedwren.DataAccess;

/// <summary>
/// Options for the Dapper data-access layer, supplied by the composition root from the "DataSource"
/// and "ConnectionStrings" configuration. Kept as a plain object so this layer takes no dependency on
/// the configuration system.
/// </summary>
public sealed class SqlDataAccessOptions
{
    /// <summary>The relational engine to connect to.</summary>
    public DatabaseProvider Provider { get; init; } = DatabaseProvider.SqlServer;

    /// <summary>The connection string for <see cref="Provider"/>.</summary>
    public string ConnectionString { get; init; } = string.Empty;
}
