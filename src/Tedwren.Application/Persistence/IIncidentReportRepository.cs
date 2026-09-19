using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence for accident / incident records (PRD §8.2), scoped by company (R15).</summary>
public interface IIncidentReportRepository
{
    /// <summary>Persists a new incident record.</summary>
    Task AddAsync(IncidentReport report, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's incident records, newest first.</summary>
    Task<IReadOnlyList<IncidentReport>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single incident record by id, or null if none exists.</summary>
    Task<IncidentReport?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates an incident record's investigation/close-out state.</summary>
    Task UpdateAsync(IncidentReport report, CancellationToken cancellationToken = default);
}
