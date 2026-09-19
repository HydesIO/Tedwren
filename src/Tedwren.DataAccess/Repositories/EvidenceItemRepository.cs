using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IEvidenceItemRepository"/> for operative field evidence captures (M5). Append-only.</summary>
public sealed class EvidenceItemRepository : RepositoryBase, IEvidenceItemRepository
{
    private const string Columns =
        "Id, CompanyId, PersonId, Note, Latitude, Longitude, PhotoReference, CapturedUtc, CreatedUtc";

    /// <summary>Creates the repository over the connection factory.</summary>
    public EvidenceItemRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    public Task AddAsync(EvidenceItem item, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO EvidenceItems (Id, CompanyId, PersonId, Note, Latitude, Longitude, PhotoReference, " +
            "CapturedUtc, CreatedUtc) VALUES " +
            "(@Id, @CompanyId, @PersonId, @Note, @Latitude, @Longitude, @PhotoReference, @CapturedUtc, @CreatedUtc)",
            ToParameters(item), cancellationToken);

    public async Task<EvidenceItem?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM EvidenceItems WHERE Id = @Id", new { Id = id }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    public async Task<IReadOnlyList<EvidenceItem>> GetByPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM EvidenceItems WHERE PersonId = @PersonId ORDER BY CapturedUtc DESC",
            new { PersonId = personId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    public async Task<IReadOnlyList<EvidenceItem>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM EvidenceItems WHERE CompanyId = @CompanyId ORDER BY CapturedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Flattens an evidence item to Dapper parameters.</summary>
    private static object ToParameters(EvidenceItem i) => new
    {
        i.Id,
        i.CompanyId,
        i.PersonId,
        i.Note,
        i.Latitude,
        i.Longitude,
        i.PhotoReference,
        i.CapturedUtc,
        i.CreatedUtc,
    };

    /// <summary>Maps a queried row to the domain entity.</summary>
    private static EvidenceItem ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        PersonId = r.PersonId,
        Note = r.Note,
        Latitude = r.Latitude,
        Longitude = r.Longitude,
        PhotoReference = r.PhotoReference,
        CapturedUtc = r.CapturedUtc,
        CreatedUtc = r.CreatedUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid CompanyId, Guid PersonId, string? Note, double? Latitude, double? Longitude,
        string? PhotoReference, DateTimeOffset CapturedUtc, DateTimeOffset CreatedUtc);
}
