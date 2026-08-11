using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IReferenceDataRepository"/> serving the seeded form option lists, in display order.</summary>
public sealed class ReferenceDataRepository : RepositoryBase, IReferenceDataRepository
{
    /// <summary>Creates the repository over the connection factory.</summary>
    public ReferenceDataRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns the ordered values for a reference list, or an empty list for an unknown key.</summary>
    public async Task<IReadOnlyList<string>> GetValuesAsync(string listKey, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<string>(
            "SELECT Value FROM ReferenceValues WHERE ListKey = @ListKey ORDER BY SortOrder, Value",
            new { ListKey = listKey }, cancellationToken);
        return rows.ToList();
    }
}
