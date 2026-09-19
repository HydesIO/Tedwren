using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence for hazard / near-miss reports (PRD §8.2), scoped by company (R15).</summary>
public interface IHazardReportRepository
{
    /// <summary>Persists a new hazard report.</summary>
    Task AddAsync(HazardReport report, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's hazard reports, newest first.</summary>
    Task<IReadOnlyList<HazardReport>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single hazard report by id, or null if none exists.</summary>
    Task<HazardReport?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates a hazard report's triage/close-out state.</summary>
    Task UpdateAsync(HazardReport report, CancellationToken cancellationToken = default);
}
