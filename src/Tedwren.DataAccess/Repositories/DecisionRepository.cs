using System.Text.Json;
using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Dapper <see cref="IDecisionRepository"/> for the append-only site-entry decision store (R10). The checks
/// are stored as JSON (System.Text.Json), matching the "extensible config as JSON" convention.
/// </summary>
public sealed class DecisionRepository : RepositoryBase, IDecisionRepository
{
    private const string Columns = "Id, PersonId, SiteId, Admitted, OccurredUtc, ChecksJson";

    /// <summary>Creates the repository over the connection factory.</summary>
    public DecisionRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Returns a worker's decisions, newest first.</summary>
    public async Task<IReadOnlyList<SiteEntryDecision>> GetByPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM Decisions WHERE PersonId = @PersonId ORDER BY OccurredUtc DESC",
            new { PersonId = personId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Returns a site's decisions, newest first.</summary>
    public async Task<IReadOnlyList<SiteEntryDecision>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM Decisions WHERE SiteId = @SiteId ORDER BY OccurredUtc DESC",
            new { SiteId = siteId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Appends a decision, serialising its checks to JSON.</summary>
    public Task AddAsync(SiteEntryDecision decision, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO Decisions (Id, PersonId, SiteId, Admitted, OccurredUtc, ChecksJson) " +
            "VALUES (@Id, @PersonId, @SiteId, @Admitted, @OccurredUtc, @ChecksJson)",
            new
            {
                decision.Id,
                decision.PersonId,
                decision.SiteId,
                decision.Admitted,
                decision.OccurredUtc,
                ChecksJson = JsonSerializer.Serialize(decision.Checks),
            },
            cancellationToken);

    /// <summary>Maps a queried row to the domain entity, deserialising its checks.</summary>
    private static SiteEntryDecision ToEntity(Row r) => new()
    {
        Id = r.Id,
        PersonId = r.PersonId,
        SiteId = r.SiteId,
        Admitted = r.Admitted,
        OccurredUtc = r.OccurredUtc,
        Checks = string.IsNullOrWhiteSpace(r.ChecksJson)
            ? Array.Empty<DecisionCheck>()
            : JsonSerializer.Deserialize<List<DecisionCheck>>(r.ChecksJson) ?? new List<DecisionCheck>(),
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id, Guid PersonId, Guid SiteId, bool Admitted, DateTimeOffset OccurredUtc, string? ChecksJson);
}
