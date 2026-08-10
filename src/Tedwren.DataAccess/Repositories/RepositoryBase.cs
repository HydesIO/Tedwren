using Dapper;
using Tedwren.DataAccess.Connections;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Shared Dapper helpers for the repositories: each call opens a fresh connection from the factory and
/// lets Dapper manage it. Centralising this keeps the concrete repositories to just their SQL and row
/// mapping (Single Responsibility). SQL here is written to be ANSI-portable across both engines.
/// </summary>
public abstract class RepositoryBase
{
    /// <summary>The connection factory for the configured engine.</summary>
    protected IDbConnectionFactory ConnectionFactory { get; }

    /// <summary>Creates the base with its connection factory.</summary>
    protected RepositoryBase(IDbConnectionFactory connectionFactory) => ConnectionFactory = connectionFactory;

    /// <summary>Runs a query and returns all rows mapped to <typeparamref name="T"/>.</summary>
    protected async Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param, CancellationToken cancellationToken)
    {
        using var connection = ConnectionFactory.Create();
        var rows = await connection.QueryAsync<T>(new CommandDefinition(sql, param, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    /// <summary>Runs a query expected to return a single row, or the type default when none match.</summary>
    protected async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? param, CancellationToken cancellationToken)
    {
        using var connection = ConnectionFactory.Create();
        return await connection.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, param, cancellationToken: cancellationToken));
    }

    /// <summary>Runs a scalar query (e.g. COUNT) and returns the value.</summary>
    protected async Task<T?> ExecuteScalarAsync<T>(string sql, object? param, CancellationToken cancellationToken)
    {
        using var connection = ConnectionFactory.Create();
        return await connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, param, cancellationToken: cancellationToken));
    }

    /// <summary>Runs a non-query statement and returns the number of affected rows.</summary>
    protected async Task<int> ExecuteAsync(string sql, object? param, CancellationToken cancellationToken)
    {
        using var connection = ConnectionFactory.Create();
        return await connection.ExecuteAsync(new CommandDefinition(sql, param, cancellationToken: cancellationToken));
    }
}
