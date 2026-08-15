using System.Data.Common;
using System.Reflection;
using Dapper;
using Tedwren.DataAccess.Connections;
using Tedwren.DataAccess.Dialects;

namespace Tedwren.DataAccess.Migrations;

/// <summary>Which database an embedded migration script set targets.</summary>
public enum MigrationArea
{
    /// <summary>The product/compliance database — every script <em>not</em> under the "Commercial" folder.</summary>
    Product,

    /// <summary>The commercial database — scripts under "Scripts/&lt;engine&gt;/Commercial" (billing, launch list, leads, affiliates).</summary>
    Commercial,
}

/// <summary>
/// Applies the embedded, idempotent migration scripts for the configured engine at startup (dev). Each
/// script uses "if not exists" guards so re-running is safe. Scripts are ordered by name. The scripts are
/// split into two areas — <see cref="MigrationArea.Product"/> and <see cref="MigrationArea.Commercial"/> —
/// so each set can be applied to its own database via the matching connection factory.
/// </summary>
public sealed class MigrationRunner
{
    private readonly ISqlDialect _dialect;

    /// <summary>Creates the runner over the dialect (which selects the engine's script folder).</summary>
    public MigrationRunner(ISqlDialect dialect) => _dialect = dialect;

    /// <summary>
    /// Runs every embedded script for the configured engine and the given <paramref name="area"/>, in name
    /// order, against the supplied <paramref name="connectionFactory"/>. Program.cs calls this once per
    /// database: the product factory for <see cref="MigrationArea.Product"/> and the commercial factory for
    /// <see cref="MigrationArea.Commercial"/>.
    /// </summary>
    public async Task RunAsync(
        IDbConnectionFactory connectionFactory, MigrationArea area, CancellationToken cancellationToken = default)
    {
        var assembly = typeof(MigrationRunner).Assembly;
        var prefix = $"Tedwren.DataAccess.Migrations.Scripts.{_dialect.ScriptFolder}.";
        var commercialPrefix = prefix + "Commercial.";

        var scriptNames = assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".sql", StringComparison.Ordinal) && MatchesArea(name, prefix, commercialPrefix, area))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        using var connection = connectionFactory.Create();
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

    /// <summary>
    /// True when a resource belongs to the requested area: commercial scripts live under the "Commercial."
    /// segment; product scripts are everything else under the engine folder.
    /// </summary>
    private static bool MatchesArea(string name, string prefix, string commercialPrefix, MigrationArea area) =>
        area == MigrationArea.Commercial
            ? name.StartsWith(commercialPrefix, StringComparison.Ordinal)
            : name.StartsWith(prefix, StringComparison.Ordinal) && !name.StartsWith(commercialPrefix, StringComparison.Ordinal);

    /// <summary>Reads an embedded script's text.</summary>
    private static async Task<string> ReadResourceAsync(Assembly assembly, string resourceName, CancellationToken cancellationToken)
    {
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Migration script '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
