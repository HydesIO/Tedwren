using Tedwren.Abstractions.Configuration;

namespace Tedwren.DataAccess;

/// <summary>
/// Options for the <b>Commercial</b> Dapper data-access layer, supplied by the composition root from the
/// "DataSource" provider and the "ConnectionStrings:*Commercial" connection string. A distinct type (rather
/// than reusing <see cref="SqlDataAccessOptions"/>) so dependency injection can hand the correct connection
/// string to <see cref="Connections.AdminDbConnectionFactory"/> without ambiguity.
/// </summary>
public sealed class AdminSqlDataAccessOptions
{
    /// <summary>The relational engine to connect to (shared with the product database).</summary>
    public DatabaseProvider Provider { get; init; } = DatabaseProvider.SqlServer;

    /// <summary>The connection string for the commercial database.</summary>
    public string ConnectionString { get; init; } = string.Empty;
}
