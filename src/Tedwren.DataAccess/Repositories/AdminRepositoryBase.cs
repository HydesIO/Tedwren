using Tedwren.DataAccess.Connections;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Base for repositories that persist to the <b>Commercial</b> database. Reuses every Dapper helper on
/// <see cref="RepositoryBase"/> (query/execute/scalar) but supplies the <see cref="IAdminDbConnectionFactory"/>,
/// so a commercial repository is written exactly like a product one yet opens its connections against the
/// second catalogue. SQL stays ANSI-portable across both engines, as elsewhere.
/// </summary>
public abstract class AdminRepositoryBase : RepositoryBase
{
    /// <summary>Creates the base over the commercial connection factory.</summary>
    protected AdminRepositoryBase(IAdminDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }
}
