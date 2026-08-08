using System.Data;

namespace Tedwren.DataAccess.Connections;

/// <summary>
/// Creates ADO.NET connections for the configured engine. A factory (rather than a shared connection)
/// keeps repositories stateless and lets Dapper open/close per operation.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Creates a new, unopened connection for the configured provider.</summary>
    IDbConnection Create();
}
