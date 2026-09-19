using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence for operative field evidence captures (M5), scoped by company (R15). Append-only (R4).</summary>
public interface IEvidenceItemRepository
{
    /// <summary>Persists a new evidence item.</summary>
    Task AddAsync(EvidenceItem item, CancellationToken cancellationToken = default);

    /// <summary>Returns a single evidence item by id, or null (used for the idempotency check).</summary>
    Task<EvidenceItem?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns an operative's own evidence items, newest first.</summary>
    Task<IReadOnlyList<EvidenceItem>> GetByPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's evidence items, newest first.</summary>
    Task<IReadOnlyList<EvidenceItem>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
