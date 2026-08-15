using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>
/// Persistence contract for direct-debit mandates. Scoped by company (R15); a company has at most one current
/// mandate, but historical (cancelled/failed) rows are retained for the audit trail.
/// </summary>
public interface IMandateRepository
{
    /// <summary>Returns every mandate, most recent first.</summary>
    Task<IReadOnlyList<Mandate>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a mandate by its local id, or null.</summary>
    Task<Mandate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the company's most recent mandate, or null when it has none.</summary>
    Task<Mandate?> GetCurrentForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns the mandate carrying the given GoCardless mandate id, or null.</summary>
    Task<Mandate?> GetByGoCardlessMandateIdAsync(string goCardlessMandateId, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new mandate.</summary>
    Task AddAsync(Mandate mandate, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing mandate.</summary>
    Task UpdateAsync(Mandate mandate, CancellationToken cancellationToken = default);
}
