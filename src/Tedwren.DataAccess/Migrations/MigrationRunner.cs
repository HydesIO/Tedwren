using System.Data.Common;
using System.Reflection;
using Dapper;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Dialects;

namespace Tedwren.DataAccess.Migrations;

/// <summary>
/// Applies the embedded, idempotent migration scripts for the configured engine at startup (dev). Each
/// script uses "if not exists" guards so re-running is safe. Scripts are ordered by name.
/// </summary>
public sealed class MigrationRunner
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISqlDialect _dialect;

    /// <summary>Creates the runner over the connection factory and dialect.</summary>
    public MigrationRunner(IDbConnectionFactory connectionFactory, ISqlDialect dialect)
    {
        _connectionFactory = connectionFactory;
        _dialect = dialect;
    }

    /// <summary>Runs every embedded script for the configured engine, in name order.</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var assembly = typeof(MigrationRunner).Assembly;
        var prefix = $"Tedwren.DataAccess.Migrations.Scripts.{_dialect.ScriptFolder}.";
        var scriptNames = assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(prefix, StringComparison.Ordinal) && name.EndsWith(".sql", StringComparison.Ordinal))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        using var connection = _connectionFactory.Create();
        if (connection is DbConnection dbConnection)
        {
            await dbConnection.OpenAsync(cancellationToken);
        }
        else
        {
            connection.Open();
        }

        foreach (var scriptName in scriptNames)
        {
            var sql = await ReadResourceAsync(assembly, scriptName, cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
        }
    }

    /// <summary>Reads an embedded script's text.</summary>
    private static async Task<string> ReadResourceAsync(Assembly assembly, string resourceName, CancellationToken cancellationToken)
    {
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Migration script '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
