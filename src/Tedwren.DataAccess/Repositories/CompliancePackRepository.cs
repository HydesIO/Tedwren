using System.Text.Json;
using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.DataAccess.Repositories;

/// <summary>
/// Dapper <see cref="ICompliancePackRepository"/> (SUB-13–SUB-26). The fixed-at-send snapshot (R7) is stored as
/// JSON (System.Text.Json), matching the "extensible content as JSON" convention; access events are a separate
/// append-only table (SUB-20). SQL is ANSI-portable across both engines.
/// </summary>
public sealed class CompliancePackRepository : RepositoryBase, ICompliancePackRepository
{
    private const string Columns =
        "Id, CompanyId, Title, SiteId, SiteName, FromDate, ToDate, CreatedBy, CreatedUtc, ExpiresUtc, " +
        "Token, PasscodeHash, Status, SupersededByPackId, SubjectsJson";

    /// <summary>Creates the repository over the connection factory.</summary>
    public CompliancePackRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Persists a new pack (with its fixed snapshot serialised to JSON).</summary>
    public Task AddAsync(CompliancePack pack, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            $"INSERT INTO CompliancePacks ({Columns}) VALUES " +
            "(@Id, @CompanyId, @Title, @SiteId, @SiteName, @FromDate, @ToDate, @CreatedBy, @CreatedUtc, @ExpiresUtc, " +
            "@Token, @PasscodeHash, @Status, @SupersededByPackId, @SubjectsJson)",
            ToParameters(pack), cancellationToken);

    /// <summary>Returns a pack by id, scoped to its company, or null (R15).</summary>
    public async Task<CompliancePack?> GetAsync(Guid companyId, Guid packId, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM CompliancePacks WHERE Id = @Id AND CompanyId = @CompanyId",
            new { Id = packId, CompanyId = companyId }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns a pack by its opaque token, or null.</summary>
    public async Task<CompliancePack?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var row = await QuerySingleOrDefaultAsync<Row>(
            $"SELECT {Columns} FROM CompliancePacks WHERE Token = @Token", new { Token = token }, cancellationToken);
        return row is null ? null : ToEntity(row);
    }

    /// <summary>Returns a company's packs, newest first.</summary>
    public async Task<IReadOnlyList<CompliancePack>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            $"SELECT {Columns} FROM CompliancePacks WHERE CompanyId = @CompanyId ORDER BY CreatedUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(ToEntity).ToList();
    }

    /// <summary>Persists status changes (revoke, supersede).</summary>
    public Task UpdateStatusAsync(CompliancePack pack, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "UPDATE CompliancePacks SET Status = @Status, SupersededByPackId = @SupersededByPackId WHERE Id = @Id",
            new { pack.Id, Status = (int)pack.Status, pack.SupersededByPackId }, cancellationToken);

    /// <summary>Appends a recipient access event (SUB-20).</summary>
    public Task AddAccessEventAsync(PackAccessEvent accessEvent, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO PackAccessEvents (Id, PackId, Kind, OccurredUtc) VALUES (@Id, @PackId, @Kind, @OccurredUtc)",
            new { accessEvent.Id, accessEvent.PackId, Kind = (int)accessEvent.Kind, accessEvent.OccurredUtc }, cancellationToken);

    /// <summary>Returns the open/download tallies for a pack (SUB-20).</summary>
    public async Task<(int Opened, int Downloaded)> GetAccessTallyAsync(Guid packId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<TallyRow>(
            "SELECT Kind, COUNT(*) AS Count FROM PackAccessEvents WHERE PackId = @PackId GROUP BY Kind",
            new { PackId = packId }, cancellationToken);
        var opened = rows.FirstOrDefault(r => r.Kind == (int)PackAccessKind.Opened)?.Count ?? 0;
        var downloaded = rows.FirstOrDefault(r => r.Kind == (int)PackAccessKind.Downloaded)?.Count ?? 0;
        return (opened, downloaded);
    }

    /// <summary>Flattens a pack to Dapper parameters (snapshot serialised, enum as int).</summary>
    private static object ToParameters(CompliancePack pack) => new
    {
        pack.Id,
        pack.CompanyId,
        pack.Title,
        pack.SiteId,
        pack.SiteName,
        pack.FromDate,
        pack.ToDate,
        pack.CreatedBy,
        pack.CreatedUtc,
        pack.ExpiresUtc,
        pack.Token,
        pack.PasscodeHash,
        Status = (int)pack.Status,
        pack.SupersededByPackId,
        SubjectsJson = JsonSerializer.Serialize(pack.Subjects),
    };

    /// <summary>Maps a queried row to the domain entity, deserialising the snapshot.</summary>
    private static CompliancePack ToEntity(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        Title = r.Title,
        SiteId = r.SiteId,
        SiteName = r.SiteName,
        FromDate = r.FromDate,
        ToDate = r.ToDate,
        CreatedBy = r.CreatedBy,
        CreatedUtc = r.CreatedUtc,
        ExpiresUtc = r.ExpiresUtc,
        Token = r.Token,
        PasscodeHash = r.PasscodeHash,
        Status = (PackStatus)r.Status,
        SupersededByPackId = r.SupersededByPackId,
        Subjects = string.IsNullOrWhiteSpace(r.SubjectsJson)
            ? Array.Empty<PackSubject>()
            : JsonSerializer.Deserialize<List<PackSubject>>(r.SubjectsJson) ?? new List<PackSubject>(),
    };

    /// <summary>Flat row shape Dapper maps pack query results into.</summary>
    private sealed record Row(
        Guid Id, Guid CompanyId, string Title, Guid? SiteId, string? SiteName, DateOnly? FromDate, DateOnly? ToDate,
        string CreatedBy, DateTimeOffset CreatedUtc, DateTimeOffset ExpiresUtc, string Token, string PasscodeHash,
        int Status, Guid? SupersededByPackId, string? SubjectsJson);

    /// <summary>Flat row shape for the access-tally aggregate.</summary>
    private sealed record TallyRow(int Kind, int Count);
}
