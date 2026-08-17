using System.Data;
using Microsoft.Data.SqlClient;
using Npgsql;
using Tedwren.Abstractions.Configuration;

namespace Tedwren.DataAccess.Connections;

/// <summary>
/// Creates the correct ADO.NET connection for the <b>Commercial</b> database from
/// <see cref="AdminSqlDataAccessOptions"/>. Mirrors <see cref="DbConnectionFactory"/> but binds to the second
/// connection string, so the commercial plane persists to its own catalogue while the product/compliance data
/// stays in the primary database.
/// </summary>
public sealed class AdminDbConnectionFactory : IAdminDbConnectionFactory
{
    private readonly AdminSqlDataAccessOptions _options;

    /// <summary>Creates the factory from the commercial data-access options.</summary>
    public AdminDbConnectionFactory(AdminSqlDataAccessOptions options) => _options = options;

    /// <summary>Creates a new, unopened connection for the configured provider against the commercial database.</summary>
    public IDbConnection Create() => _options.Provider switch
    {
        DatabaseProvider.PostgreSql => new NpgsqlConnection(_options.ConnectionString),
        _ => new SqlConnection(_options.ConnectionString),
    };
}
