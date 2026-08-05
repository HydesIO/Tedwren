using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>
/// Persistence contract for <see cref="Engagement"/>. All reads are scoped by company id so one
/// company can never see another's engagements (SF-2, R15).
/// </summary>
public interface IEngagementRepository
{
    /// <summary>Returns the active engagements for a company.</summary>
    Task<IReadOnlyList<Engagement>> GetActiveByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a specific engagement owned by the company, or null (tenant-scoped, R15).</summary>
    Task<Engagement?> GetAsync(Guid companyId, Guid engagementId, CancellationToken cancellationToken = default);

    /// <summary>Returns the engagement (any status) of a person within a company, or null (SF-2/SF-3 check).</summary>
    Task<Engagement?> GetByCompanyAndPersonAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Counts the active engagements for a company (register/billing headcount).</summary>
    Task<int> CountActiveByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new engagement.</summary>
    Task AddAsync(Engagement engagement, CancellationToken cancellationToken = default);

    /// <summary>Persists status/field changes to an existing engagement (append-only history is Phase 9+).</summary>
    Task UpdateAsync(Engagement engagement, CancellationToken cancellationToken = default);
}
