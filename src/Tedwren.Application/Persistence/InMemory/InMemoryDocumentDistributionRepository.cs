using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IDocumentDistributionRepository"/>.</summary>
public sealed class InMemoryDocumentDistributionRepository : IDocumentDistributionRepository
{
    private readonly ConcurrentDictionary<Guid, DocumentDistribution> _distributions = new();
    private readonly ConcurrentDictionary<Guid, DocumentAcknowledgement> _acknowledgements = new();

    /// <summary>Persists a new distribution and its recipient acknowledgement rows.</summary>
    public Task AddAsync(DocumentDistribution distribution, IReadOnlyList<DocumentAcknowledgement> acknowledgements, CancellationToken cancellationToken = default)
    {
        _distributions[distribution.Id] = distribution;
        foreach (var acknowledgement in acknowledgements)
        {
            _acknowledgements[acknowledgement.Id] = acknowledgement;
        }

        return Task.CompletedTask;
    }

    /// <summary>Returns a company's distributions, newest first.</summary>
    public Task<IReadOnlyList<DocumentDistribution>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DocumentDistribution> rows = _distributions.Values
            .Where(d => d.CompanyId == companyId)
            .OrderByDescending(d => d.SentUtc)
            .ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns a single distribution by id, or null if none exists.</summary>
    public Task<DocumentDistribution?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_distributions.TryGetValue(id, out var distribution) ? distribution : null);

    /// <summary>Returns the acknowledgement rows for a distribution.</summary>
    public Task<IReadOnlyList<DocumentAcknowledgement>> GetAcknowledgementsAsync(Guid distributionId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DocumentAcknowledgement> rows = _acknowledgements.Values.Where(a => a.DistributionId == distributionId).ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns the acknowledgement rows for a company (for list completion counts).</summary>
    public Task<IReadOnlyList<DocumentAcknowledgement>> GetAcknowledgementsForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DocumentAcknowledgement> rows = _acknowledgements.Values.Where(a => a.CompanyId == companyId).ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns a single acknowledgement by id, or null if none exists.</summary>
    public Task<DocumentAcknowledgement?> GetAcknowledgementAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_acknowledgements.TryGetValue(id, out var acknowledgement) ? acknowledgement : null);

    /// <summary>Updates an acknowledgement row.</summary>
    public Task UpdateAcknowledgementAsync(DocumentAcknowledgement acknowledgement, CancellationToken cancellationToken = default)
    {
        _acknowledgements[acknowledgement.Id] = acknowledgement;
        return Task.CompletedTask;
    }
}
