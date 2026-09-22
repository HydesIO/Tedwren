using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IRamsAcknowledgementRepository"/> — the Gate-5 operative RAMS signatures (append-only), scoped by company (R15).</summary>
public sealed class RamsAcknowledgementRepository : RepositoryBase, IRamsAcknowledgementRepository
{
    private const string SelectColumns =
        "SELECT Id, CompanyId, PersonId, FamilyId, Version, SignatureName, SignedUtc, ExpiresUtc FROM RamsAcknowledgements";

    /// <summary>Creates the repository over the connection factory.</summary>
    public RamsAcknowledgementRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Persists a new RAMS acknowledgement.</summary>
    public async Task AddAsync(RamsAcknowledgement acknowledgement, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "INSERT INTO RamsAcknowledgements (Id, CompanyId, PersonId, FamilyId, Version, SignatureName, SignedUtc, ExpiresUtc) " +
            "VALUES (@Id, @CompanyId, @PersonId, @FamilyId, @Version, @SignatureName, @SignedUtc, @ExpiresUtc)",
            new
            {
                acknowledgement.Id,
                acknowledgement.CompanyId,
                acknowledgement.PersonId,
                acknowledgement.FamilyId,
                acknowledgement.Version,
                acknowledgement.SignatureName,
                acknowledgement.SignedUtc,
                acknowledgement.ExpiresUtc,
            },
            cancellationToken);

    /// <summary>Returns the operative's most recent acknowledgement for a family, or null when they have never signed it.</summary>
    public async Task<RamsAcknowledgement?> GetLatestForPersonAsync(Guid companyId, Guid personId, Guid familyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<Row>(
            SelectColumns + " WHERE CompanyId = @CompanyId AND PersonId = @PersonId AND FamilyId = @FamilyId ORDER BY SignedUtc DESC",
            new { CompanyId = companyId, PersonId = personId, FamilyId = familyId }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : Map(row);
    }

    /// <summary>Maps a flat row to a RAMS acknowledgement entity.</summary>
    private static RamsAcknowledgement Map(Row r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        PersonId = r.PersonId,
        FamilyId = r.FamilyId,
        Version = r.Version,
        SignatureName = r.SignatureName,
        SignedUtc = r.SignedUtc,
        ExpiresUtc = r.ExpiresUtc,
    };

    /// <summary>Flat row shape Dapper maps query results into.</summary>
    private sealed record Row(
        Guid Id,
        Guid CompanyId,
        Guid PersonId,
        Guid FamilyId,
        int Version,
        string SignatureName,
        DateTimeOffset SignedUtc,
        DateTimeOffset? ExpiresUtc);
}
