using Tedwren.Application.Persistence;
using Tedwren.DataAccess.Connections;
using Tedwren.Domain.Entities;

namespace Tedwren.DataAccess.Repositories;

/// <summary>Dapper <see cref="IDocumentDistributionRepository"/> for document distribution + acknowledgement (PRD §8.2), scoped by company (R15).</summary>
public sealed class DocumentDistributionRepository : RepositoryBase, IDocumentDistributionRepository
{
    private const string SelectDistribution =
        "SELECT Id, CompanyId, Title, Category, Audience, FileReference, SentBy, SentUtc FROM DocumentDistributions";

    private const string SelectAck =
        "SELECT Id, DistributionId, CompanyId, RecipientName, PersonId, AcknowledgedUtc FROM DocumentAcknowledgements";

    /// <summary>Creates the repository over the connection factory.</summary>
    public DocumentDistributionRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory)
    {
    }

    /// <summary>Persists a new distribution and its recipient acknowledgement rows.</summary>
    public async Task AddAsync(DocumentDistribution distribution, IReadOnlyList<DocumentAcknowledgement> acknowledgements, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            "INSERT INTO DocumentDistributions (Id, CompanyId, Title, Category, Audience, FileReference, SentBy, SentUtc) " +
            "VALUES (@Id, @CompanyId, @Title, @Category, @Audience, @FileReference, @SentBy, @SentUtc)",
            new
            {
                distribution.Id,
                distribution.CompanyId,
                distribution.Title,
                distribution.Category,
                distribution.Audience,
                distribution.FileReference,
                distribution.SentBy,
                distribution.SentUtc,
            }, cancellationToken);

        foreach (var acknowledgement in acknowledgements)
        {
            await ExecuteAsync(
                "INSERT INTO DocumentAcknowledgements (Id, DistributionId, CompanyId, RecipientName, PersonId, AcknowledgedUtc) " +
                "VALUES (@Id, @DistributionId, @CompanyId, @RecipientName, @PersonId, @AcknowledgedUtc)",
                new
                {
                    acknowledgement.Id,
                    acknowledgement.DistributionId,
                    acknowledgement.CompanyId,
                    acknowledgement.RecipientName,
                    acknowledgement.PersonId,
                    acknowledgement.AcknowledgedUtc,
                }, cancellationToken);
        }
    }

    /// <summary>Returns a company's distributions, newest first.</summary>
    public async Task<IReadOnlyList<DocumentDistribution>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<DistributionRow>(
            SelectDistribution + " WHERE CompanyId = @CompanyId ORDER BY SentUtc DESC",
            new { CompanyId = companyId }, cancellationToken);
        return rows.Select(MapDistribution).ToList();
    }

    /// <summary>Returns a single distribution by id, or null if none exists.</summary>
    public async Task<DocumentDistribution?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<DistributionRow>(SelectDistribution + " WHERE Id = @Id", new { Id = id }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : MapDistribution(row);
    }

    /// <summary>Returns the acknowledgement rows for a distribution.</summary>
    public async Task<IReadOnlyList<DocumentAcknowledgement>> GetAcknowledgementsAsync(Guid distributionId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<AckRow>(SelectAck + " WHERE DistributionId = @DistributionId", new { DistributionId = distributionId }, cancellationToken);
        return rows.Select(MapAck).ToList();
    }

    /// <summary>Returns the acknowledgement rows for a company.</summary>
    public async Task<IReadOnlyList<DocumentAcknowledgement>> GetAcknowledgementsForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<AckRow>(SelectAck + " WHERE CompanyId = @CompanyId", new { CompanyId = companyId }, cancellationToken);
        return rows.Select(MapAck).ToList();
    }

    /// <summary>Returns a single acknowledgement by id, or null if none exists.</summary>
    public async Task<DocumentAcknowledgement?> GetAcknowledgementAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rows = await QueryAsync<AckRow>(SelectAck + " WHERE Id = @Id", new { Id = id }, cancellationToken);
        var row = rows.FirstOrDefault();
        return row is null ? null : MapAck(row);
    }

    /// <summary>Updates an acknowledgement row's signed timestamp.</summary>
    public async Task UpdateAcknowledgementAsync(DocumentAcknowledgement acknowledgement, CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            "UPDATE DocumentAcknowledgements SET AcknowledgedUtc = @AcknowledgedUtc WHERE Id = @Id",
            new { acknowledgement.Id, acknowledgement.AcknowledgedUtc }, cancellationToken);

    /// <summary>Maps a distribution row to the entity.</summary>
    private static DocumentDistribution MapDistribution(DistributionRow r) => new()
    {
        Id = r.Id,
        CompanyId = r.CompanyId,
        Title = r.Title,
        Category = r.Category,
        Audience = r.Audience,
        FileReference = r.FileReference,
        SentBy = r.SentBy,
        SentUtc = r.SentUtc,
    };

    /// <summary>Maps an acknowledgement row to the entity.</summary>
    private static DocumentAcknowledgement MapAck(AckRow r) => new()
    {
        Id = r.Id,
        DistributionId = r.DistributionId,
        CompanyId = r.CompanyId,
        RecipientName = r.RecipientName,
        PersonId = r.PersonId,
        AcknowledgedUtc = r.AcknowledgedUtc,
    };

    /// <summary>Flat distribution row.</summary>
    private sealed record DistributionRow(
        Guid Id, Guid CompanyId, string Title, string? Category, string? Audience, string? FileReference,
        string SentBy, DateTimeOffset SentUtc);

    /// <summary>Flat acknowledgement row.</summary>
    private sealed record AckRow(
        Guid Id, Guid DistributionId, Guid CompanyId, string RecipientName, Guid? PersonId, DateTimeOffset? AcknowledgedUtc);
}
