using System.Data;
using Microsoft.Data.SqlClient;
using Npgsql;
using Tedwren.Abstractions.Configuration;

namespace Tedwren.DataAccess.Connections;

/// <summary>
/// Creates the correct ADO.NET connection (<see cref="SqlConnection"/> or <see cref="NpgsqlConnection"/>)
/// for the configured <see cref="DatabaseProvider"/>. This is the single place engine selection happens.
/// </summary>
public sealed class DbConnectionFactory : IDbConnectionFactory
{
    private readonly SqlDataAccessOptions _options;

    /// <summary>Creates the factory from the data-access options.</summary>
    public DbConnectionFactory(SqlDataAccessOptions options) => _options = options;

    /// <summary>Creates a new, unopened connection for the configured provider.</summary>
    public IDbConnection Create() => _options.Provider switch
    {
        DatabaseProvider.PostgreSql => new NpgsqlConnection(_options.ConnectionString),
        _ => new SqlConnection(_options.ConnectionString),
    };
}
