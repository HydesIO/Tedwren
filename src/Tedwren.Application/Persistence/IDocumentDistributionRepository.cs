using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence for document distribution + acknowledgement (PRD §8.2). Reads are scoped by company (R15).</summary>
public interface IDocumentDistributionRepository
{
    /// <summary>Persists a new distribution and its recipient acknowledgement rows.</summary>
    Task AddAsync(DocumentDistribution distribution, IReadOnlyList<DocumentAcknowledgement> acknowledgements, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's distributions, newest first.</summary>
    Task<IReadOnlyList<DocumentDistribution>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single distribution by id, or null if none exists.</summary>
    Task<DocumentDistribution?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the acknowledgement rows for a distribution.</summary>
    Task<IReadOnlyList<DocumentAcknowledgement>> GetAcknowledgementsAsync(Guid distributionId, CancellationToken cancellationToken = default);

    /// <summary>Returns the acknowledgement rows for a set of distributions (for list completion counts).</summary>
    Task<IReadOnlyList<DocumentAcknowledgement>> GetAcknowledgementsForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single acknowledgement by id, or null if none exists.</summary>
    Task<DocumentAcknowledgement?> GetAcknowledgementAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates an acknowledgement row (its signed timestamp).</summary>
    Task UpdateAcknowledgementAsync(DocumentAcknowledgement acknowledgement, CancellationToken cancellationToken = default);
}
