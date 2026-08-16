using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IEngagementRepository"/>. All reads are company-scoped (SF-2, R15).</summary>
public sealed class EngagementRepository : RepositoryBase, IEngagementRepository
{
    private const string Columns =
        "Id, CompanyId, PersonId, Name, Trade, InternalReference, Status, CreatedUtc, ArchivedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public EngagementRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Deletes an engagement by id (demo-data teardown).</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync("DELETE FROM Engagements WHERE Id = @Id", new { Id = id }, cancellationToken);

    /// <summary>Returns the active engagements for a company, ordered by name.</summary>
    public async Task<IReadOnlyList<Engagement>> GetActiveByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<EngagementRow>(
            $"SELECT {Columns} FROM Engagements WHERE CompanyId = @CompanyId AND Status = @Status ORDER BY Name",
            new { CompanyId = companyId, Status = (int)EngagementStatus.Active }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns the active engagements of a person across all companies.</summary>
    public async Task<IReadOnlyList<Engagement>> GetActiveByPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<EngagementRow>(
            $"SELECT {Columns} FROM Engagements WHERE PersonId = @PersonId AND Status = @Status",
            new { PersonId = personId, Status = (int)EngagementStatus.Active }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a specific engagement owned by the company, or null (tenant-scoped, R15).</summary>
    public async Task<Engagement?> GetAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<EngagementRow>(
            $"SELECT {Columns} FROM Engagements WHERE Id = @Id AND CompanyId = @CompanyId",
            new { Id = engagementId, CompanyId = companyId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns the engagement (any status) of a person within a company, or null.</summary>
    public async Task<Engagement?> GetByCompanyAndPersonAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<EngagementRow>(
            $"SELECT {Columns} FROM Engagements WHERE CompanyId = @CompanyId AND PersonId = @PersonId",
            new { CompanyId = companyId, PersonId = personId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Counts the active engagements for a company.</summary>
    public async Task<int> CountActiveByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var count = await ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM Engagements WHERE CompanyId = @CompanyId AND Status = @Status",
            new { CompanyId = companyId, Status = (int)EngagementStatus.Active }, cancellationToken);
        return (int)count;
    }

    /// <summary>Inserts a new engagement.</summary>
    public Task AddAsync(Engagement engagement, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Engagements (Id, CompanyId, PersonId, Name, Trade, InternalReference, Status, CreatedUtc, ArchivedUtc) " +
            "VALUES (@Id, @CompanyId, @PersonId, @Name, @Trade, @InternalReference, @Status, @CreatedUtc, @ArchivedUtc)",
            ToParameters(engagement), cancellationToken);

    /// <summary>Updates an existing engagement's mutable fields (status/archived) within its company.</summary>
    public Task UpdateAsync(Engagement engagement, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE Engagements SET Name = @Name, Trade = @Trade, InternalReference = @InternalReference, " +
            "Status = @Status, ArchivedUtc = @ArchivedUtc WHERE Id = @Id AND CompanyId = @CompanyId",
            ToParameters(engagement), cancellationToken);

    /// <summary>Builds the parameter object shared by insert and update.</summary>
    private static object ToParameters(Engagement e) => new
    {
        e.Id,
        e.CompanyId,
        e.PersonId,
        e.Name,
        e.Trade,
        e.InternalReference,
        Status = (int)e.Status,
        e.CreatedUtc,
        e.ArchivedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static Engagement ToEntity(EngagementRow r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        PersonId = r.PersonId,
        Name = r.Name,
        Trade = r.Trade,
        InternalReference = r.InternalReference,
        Status = (EngagementStatus)r.Status,
        CreatedUtc = r.CreatedUtc,
        ArchivedUtc = r.ArchivedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record EngagementRow(
        Guid Id, Guid CompanyId, Guid PersonId, string Name, string? Trade, string? InternalReference,
        int Status, DateTimeOffset CreatedUtc, DateTimeOffset? ArchivedUtc);
}
