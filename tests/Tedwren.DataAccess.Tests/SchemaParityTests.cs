using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Tedwren.DataAccess.Commercial.Ef;
using Tedwren.DataAccess.Ef;
using Tedwren.DataAccess.Migrations;
using Xunit;

namespace Tedwren.DataAccess.Tests;

/// <summary>
/// CI guard against schema drift between the project's two schema systems: the EF migrations (the authoritative
/// schema owner — see <c>docs/ef-migrations.md</c>) and the idempotent raw SQL scripts under
/// <c>Migrations/Scripts/**</c> that the API actually runs at startup via <see cref="MigrationRunner"/>. It runs
/// with <b>no database</b>, so it always executes in CI (unlike the <c>[SkippableFact]</c> integration tests).
///
/// It catches the exact class of defect that broke "add operative" on a real database: an EF migration that adds a
/// column (e.g. <c>Persons.EmergencyContactName</c>, <c>CompanyDocuments.FileReference</c>) without a matching
/// raw-script counterpart, so a database provisioned by the startup runner alone lacks the column and the Dapper
/// repositories fail at runtime with "Invalid column name". Every EF-mapped column must exist in the raw scripts of
/// <b>both</b> engines (SQL Server and PostgreSQL), since the runtime uses the raw scripts on both.
/// </summary>
public sealed class SchemaParityTests
{
    private const string SqlServerPrefix = "Tedwren.DataAccess.Migrations.Scripts.SqlServer.";
    private const string PostgresPrefix = "Tedwren.DataAccess.Migrations.Scripts.Postgres.";
    private const string CommercialSegment = "Commercial.";

    /// <summary>
    /// EF-mapped "Table.Column" pairs that are deliberately absent from the raw scripts. Add an entry here only
    /// with a written justification — an empty set means the two schema systems must match exactly.
    /// </summary>
    private static readonly HashSet<string> AllowedEfOnly = new(StringComparer.OrdinalIgnoreCase)
    {
        // LaunchSignups.EmailLower is a SQL Server PERSISTED computed column (LOWER(Email)) that backs the
        // case-insensitive dedup unique index. PostgreSQL implements the same dedup with a functional unique index
        // on LOWER(email) and has no such column, and the Dapper LaunchSignupRepository never reads or writes it —
        // so its absence from the Postgres scripts is intentional, not drift.
        "LaunchSignups.EmailLower",
    };

    [Fact] // Every product EF column exists in the raw SQL scripts of both engines.
    public void ProductEfColumns_ExistInRawScripts_BothDialects()
    {
        using var context = new TedwrenDbContext(
            new DbContextOptionsBuilder<TedwrenDbContext>().UseSqlServer("Server=none;Database=none;").Options);
        AssertEfColumnsBackedByScripts(EfColumns(context), commercial: false, plane: "product");
    }

    [Fact] // Every commercial EF column exists in the raw SQL scripts of both engines.
    public void CommercialEfColumns_ExistInRawScripts_BothDialects()
    {
        using var context = new CommercialDbContext(
            new DbContextOptionsBuilder<CommercialDbContext>().UseSqlServer("Server=none;Database=none;").Options);
        AssertEfColumnsBackedByScripts(EfColumns(context), commercial: true, plane: "commercial");
    }

    /// <summary>Asserts every (table, column) the EF model maps is created by the raw scripts of both engines.</summary>
    private static void AssertEfColumnsBackedByScripts(
        IReadOnlyDictionary<string, HashSet<string>> efTables, bool commercial, string plane)
    {
        var sqlServer = ScriptColumns(SqlServerPrefix, commercial);
        var postgres = ScriptColumns(PostgresPrefix, commercial);

        var problems = new List<string>();
        foreach (var (table, columns) in efTables)
        {
            foreach (var column in columns)
            {
                if (AllowedEfOnly.Contains($"{table}.{column}"))
                {
                    continue;
                }

                if (!sqlServer.TryGetValue(table, out var ssColumns) || !ssColumns.Contains(column))
                {
                    problems.Add($"SQL Server: {table}.{column} is in the EF model but no raw script creates it.");
                }

                if (!postgres.TryGetValue(table, out var pgColumns) || !pgColumns.Contains(column))
                {
                    problems.Add($"PostgreSQL: {table}.{column} is in the EF model but no raw script creates it.");
                }
            }
        }

        Assert.True(problems.Count == 0,
            $"EF ↔ raw-script schema drift in the {plane} plane — an EF migration added a column without a matching " +
            $"Migrations/Scripts counterpart, so a database migrated by the startup MigrationRunner alone would be " +
            $"missing it (see docs/ef-migrations.md and TODO 'API & UI review'):\n  " + string.Join("\n  ", problems));
    }

    /// <summary>The (table → column names) the EF model maps, keyed case-insensitively.</summary>
    private static Dictionary<string, HashSet<string>> EfColumns(DbContext context)
    {
        var tables = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var entity in context.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (string.IsNullOrEmpty(table))
            {
                continue;
            }

            var columns = GetOrAdd(tables, table);
            foreach (var property in entity.GetProperties())
            {
                var column = property.GetColumnName();
                if (!string.IsNullOrEmpty(column))
                {
                    columns.Add(column);
                }
            }
        }

        return tables;
    }

    /// <summary>Parses the embedded raw SQL scripts for an engine + area into a (table → column names) map.</summary>
    private static Dictionary<string, HashSet<string>> ScriptColumns(string enginePrefix, bool commercial)
    {
        var assembly = typeof(MigrationRunner).Assembly;
        var commercialPrefix = enginePrefix + CommercialSegment;
        var tables = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.EndsWith(".sql", StringComparison.Ordinal) || !name.StartsWith(enginePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            // Product = under the engine folder but NOT under the "Commercial." segment; commercial = the opposite.
            var isCommercial = name.StartsWith(commercialPrefix, StringComparison.Ordinal);
            if (isCommercial != commercial)
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(name)!;
            using var reader = new StreamReader(stream);
            ParseSql(reader.ReadToEnd(), tables);
        }

        return tables;
    }

    /// <summary>Column-level keywords that begin a table-level constraint/index line rather than a column.</summary>
    private static readonly HashSet<string> NonColumnKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "CONSTRAINT", "PRIMARY", "FOREIGN", "UNIQUE", "CHECK", "INDEX", "KEY",
    };

    /// <summary>
    /// Extracts table/column names from a script. Handles the two shapes the scripts use: <c>CREATE TABLE</c> with
    /// one column per line, and <c>ALTER TABLE … ADD [COLUMN [IF NOT EXISTS]] col …</c>. Robust to the idempotency
    /// guards (<c>IF OBJECT_ID … IS NULL</c> / <c>IF COL_LENGTH … IS NULL</c>) that precede the statements.
    /// </summary>
    private static void ParseSql(string sql, Dictionary<string, HashSet<string>> tables)
    {
        var lines = sql.Replace("\r", string.Empty).Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            // Handles SQL Server (CREATE TABLE dbo.X) and PostgreSQL (CREATE TABLE IF NOT EXISTS x).
            var create = Regex.Match(lines[i],
                @"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?(?:dbo\.)?\[?(?<t>\w+)\]?", RegexOptions.IgnoreCase);
            if (!create.Success)
            {
                continue;
            }

            var columns = GetOrAdd(tables, create.Groups["t"].Value);

            // Advance to the opening parenthesis (it is on the CREATE line or a following line), then read one
            // column per line until the line that closes the table definition.
            var open = i;
            while (open < lines.Length && !lines[open].Contains('('))
            {
                open++;
            }

            for (var k = open + 1; k < lines.Length; k++)
            {
                var trimmed = lines[k].Trim();
                if (trimmed.StartsWith(")"))
                {
                    break;
                }

                var identifier = FirstIdentifier(trimmed);
                if (identifier is not null && !NonColumnKeywords.Contains(identifier))
                {
                    columns.Add(identifier);
                }
            }

            i = open;
        }

        // ALTER TABLE … ADD <column> — one match per added column (skips ADD CONSTRAINT).
        foreach (Match alter in Regex.Matches(
            sql,
            @"ALTER\s+TABLE\s+(?:dbo\.)?\[?(?<t>\w+)\]?\s+ADD\s+(?:COLUMN\s+)?(?:IF\s+NOT\s+EXISTS\s+)?\[?(?<c>\w+)\]?",
            RegexOptions.IgnoreCase))
        {
            var column = alter.Groups["c"].Value;
            if (!NonColumnKeywords.Contains(column))
            {
                GetOrAdd(tables, alter.Groups["t"].Value).Add(column);
            }
        }
    }

    /// <summary>The first bracketed-or-bare identifier at the start of a column-definition fragment, or null.</summary>
    private static string? FirstIdentifier(string fragment)
    {
        var match = Regex.Match(fragment, @"^\[?(?<id>[A-Za-z_]\w*)\]?");
        return match.Success ? match.Groups["id"].Value : null;
    }

    /// <summary>Gets (or creates) the case-insensitive column set for a table.</summary>
    private static HashSet<string> GetOrAdd(Dictionary<string, HashSet<string>> tables, string table)
    {
        if (!tables.TryGetValue(table, out var columns))
        {
            columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            tables[table] = columns;
        }

        return columns;
    }
}
