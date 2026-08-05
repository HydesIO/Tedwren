namespace Tedwren.Abstractions.Configuration;

/// <summary>
/// Identifies the relational engine the data-access layer targets. SQL Server is the primary
/// engine; PostgreSQL is supported from the outset so that adding production PostgreSQL support
/// is a validation exercise rather than a restructuring of the data-access layer.
/// </summary>
public enum DatabaseProvider
{
    /// <summary>Microsoft SQL Server (primary engine).</summary>
    SqlServer = 0,

    /// <summary>PostgreSQL (supported ahead of the production launch).</summary>
    PostgreSql = 1,
}
