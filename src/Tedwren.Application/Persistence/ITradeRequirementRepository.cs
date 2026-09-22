using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for the trade→required-qualification rules (SF-11). Rules are global (CompanyId null) or org-custom (Q21, R15).</summary>
public interface ITradeRequirementRepository
{
    /// <summary>Returns the requirements for a trade (case-insensitive): the global rows plus those owned by <paramref name="companyId"/> when given.</summary>
    Task<IReadOnlyList<TradeQualificationRequirement>> GetByTradeAsync(string trade, Guid? companyId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns every trade requirement across tenants (seeding + name resolution).</summary>
    Task<IReadOnlyList<TradeQualificationRequirement>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the requirements a caller may manage: the global rows plus those owned by <paramref name="companyId"/> (null = platform-admin, global only).</summary>
    Task<IReadOnlyList<TradeQualificationRequirement>> GetForManagementAsync(Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a requirement by id, or null.</summary>
    Task<TradeQualificationRequirement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a new trade requirement.</summary>
    Task AddAsync(TradeQualificationRequirement requirement, CancellationToken cancellationToken = default);

    /// <summary>Updates a requirement's flags (legal-mandatory / client-required).</summary>
    Task UpdateAsync(TradeQualificationRequirement requirement, CancellationToken cancellationToken = default);

    /// <summary>Removes a requirement (the trade→accreditation mapping row).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
