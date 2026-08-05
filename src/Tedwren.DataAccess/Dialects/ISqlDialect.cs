using Tedwren.Abstractions.Configuration;

namespace Tedwren.DataAccess.Dialects;

/// <summary>
/// Encapsulates the differences between the supported engines. For the Phase 8 slice the repository SQL
/// is ANSI-identical across both engines (unquoted identifiers fold consistently), so the dialect only
/// selects the migration script set and reports the provider. Later phases extend it where the SQL
/// genuinely diverges (JSON columns, upsert, paging).
/// </summary>
public interface ISqlDialect
{
    /// <summary>The engine this dialect targets.</summary>
    DatabaseProvider Provider { get; }

    /// <summary>The embedded migration-script sub-folder for this engine (e.g. "SqlServer", "Postgres").</summary>
    string ScriptFolder { get; }
}

/// <summary>SQL Server dialect.</summary>
public sealed class SqlServerDialect : ISqlDialect
{
    /// <summary>SQL Server.</summary>
    public DatabaseProvider Provider => DatabaseProvider.SqlServer;

    /// <summary>SQL Server migration scripts.</summary>
    public string ScriptFolder => "SqlServer";
}

/// <summary>PostgreSQL dialect.</summary>
public sealed class PostgresDialect : ISqlDialect
{
    /// <summary>PostgreSQL.</summary>
    public DatabaseProvider Provider => DatabaseProvider.PostgreSql;

    /// <summary>PostgreSQL migration scripts.</summary>
    public string ScriptFolder => "Postgres";
}
