using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IMasterListItemRepository"/> for the compliance master lists (spec §5–§8).</summary>
public sealed class MasterListItemRepository : RepositoryBase, IMasterListItemRepository
{
    private const string Columns = "Id, ListKey, CompanyId, Value, SortOrder, IsActive, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public MasterListItemRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns a value by id, or null.</summary>
    public async Task<MasterListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM MasterListItems WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the values for a list visible to a caller: global rows (CompanyId IS NULL) plus the caller's own org rows (R15).</summary>
    public async Task<IReadOnlyList<MasterListItem>> GetByKeyAsync(
        string listKey, Guid? companyId, bool includeInactive, CancellationToken cancellationToken = default)
    {
        // Branch the active filter rather than compare a bool parameter to a literal, so the SQL stays portable
        // across SQL Server (BIT) and PostgreSQL (boolean). A null companyId matches only the global rows.
        var activeClause = includeInactive ? string.Empty : " AND IsActive = @Active";
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM MasterListItems " +
            "WHERE ListKey = @ListKey AND (CompanyId IS NULL OR CompanyId = @CompanyId)" + activeClause +
            " ORDER BY SortOrder, Value",
            new { ListKey = listKey, CompanyId = companyId, Active = true }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Inserts a new value.</summary>
    public Task AddAsync(MasterListItem item, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO MasterListItems (Id, ListKey, CompanyId, Value, SortOrder, IsActive, CreatedUtc) " +
            "VALUES (@Id, @ListKey, @CompanyId, @Value, @SortOrder, @IsActive, @CreatedUtc)",
            ToParameters(item), cancellationToken);

    /// <summary>Updates a value's mutable fields (text, order, active flag).</summary>
    public Task UpdateAsync(MasterListItem item, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE MasterListItems SET Value = @Value, SortOrder = @SortOrder, IsActive = @IsActive WHERE Id = @Id",
            ToParameters(item), cancellationToken);

    /// <summary>Flattens a value to Dapper parameters.</summary>
    private static object ToParameters(MasterListItem i) => new
    {
        i.Id,
        i.ListKey,
        i.CompanyId,
        i.Value,
        i.SortOrder,
        i.IsActive,
        i.CreatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static MasterListItem ToEntity(Row r) => new()
    {
        Id = r.Id,
        ListKey = r.ListKey,
        CompanyId = r.CompanyId,
        Value = r.Value,
        SortOrder = r.SortOrder,
        IsActive = r.IsActive,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, string ListKey, Guid? CompanyId, string Value, int SortOrder, bool IsActive, DateTimeOffset CreatedUtc);
}
