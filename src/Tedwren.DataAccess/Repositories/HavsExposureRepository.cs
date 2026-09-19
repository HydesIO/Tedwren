using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IHavsExposureRepository"/> for HAVs exposure records (PRD §8.2), scoped by company (R15). Append-only.</summary>
public sealed class HavsExposureRepository : RepositoryBase, IHavsExposureRepository
{
    private const string SelectColumns =
        "SELECT Id, CompanyId, PersonName, ExposureDate, ToolUsagesJson, RecordedBy, RecordedUtc FROM HavsExposureRecords";

    /// <summary>Creates the repository over the connection factory.</summary>
    public HavsExposureRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Persists a new HAVs exposure record.</summary>
    public async Task AddAsync(HavsExposureRecord record, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "INSERT INTO HavsExposureRecords (Id, CompanyId, PersonName, ExposureDate, ToolUsagesJson, RecordedBy, RecordedUtc) " +
            "VALUES (@Id, @CompanyId, @PersonName, @ExposureDate, @ToolUsagesJson, @RecordedBy, @RecordedUtc)",
            ToParams(record), cancellationToken);

    /// <summary>Returns a company's HAVs exposure records, newest first.</summary>
    public async Task<IReadOnlyList<HavsExposureRecord>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            SelectColumns + " WHERE CompanyId = @CompanyId ORDER BY RecordedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(Map).ToList();
    }

    /// <summary>Returns a single HAVs exposure record by id, or null if none exists.</summary>
    public async Task<HavsExposureRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(SelectColumns + " WHERE Id = @Id", new { Id = id }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : Map(row);
    }

    /// <summary>The insert parameter set.</summary>
    private static object ToParams(HavsExposureRecord r) => new
    {
        r.Id,
        r.CompanyId,
        r.PersonName,
        r.ExposureDate,
        r.ToolUsagesJson,
        r.RecordedBy,
        r.RecordedUtc,
    };

    /// <summary>Maps a flat row to a HAVs exposure record entity.</summary>
    private static HavsExposureRecord Map(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        PersonName = r.PersonName,
        ExposureDate = r.ExposureDate,
        ToolUsagesJson = r.ToolUsagesJson,
        RecordedBy = r.RecordedBy,
        RecordedUtc = r.RecordedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id,
        Guid CompanyId,
        string PersonName,
        DateOnly ExposureDate,
        string ToolUsagesJson,
        string RecordedBy,
        DateTimeOffset RecordedUtc);
}
