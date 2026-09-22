using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="ITradeRequirementRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryTradeRequirementRepository : ITradeRequirementRepository
{
    private readonly InMemoryQualificationStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryTradeRequirementRepository(InMemoryQualificationStore store) => _store = store;

    /// <summary>Returns the requirements for a trade (case-insensitive): global rows plus those owned by the company (null = global only).</summary>
    public Task<IReadOnlyList<TradeQualificationRequirement>> GetByTradeAsync(string trade, Guid? companyId = null, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TradeQualificationRequirement> requirements = _store.TradeRequirements.Values
            .Where(r => string.Equals(r.Trade, trade, StringComparison.OrdinalIgnoreCase) && (r.CompanyId is null || r.CompanyId == companyId))
            .ToList();
        return Task.FromResult(requirements);
    }

    /// <summary>Returns every requirement.</summary>
    public Task<IReadOnlyList<TradeQualificationRequirement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TradeQualificationRequirement> requirements = _store.TradeRequirements.Values.ToList();
        return Task.FromResult(requirements);
    }

    /// <summary>Returns the requirements a caller may manage: global rows plus those owned by the company (null = platform admin, global only).</summary>
    public Task<IReadOnlyList<TradeQualificationRequirement>> GetForManagementAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TradeQualificationRequirement> requirements = _store.TradeRequirements.Values
            .Where(r => r.CompanyId is null || r.CompanyId == companyId)
            .ToList();
        return Task.FromResult(requirements);
    }

    /// <summary>Returns a requirement by id, or null.</summary>
    public Task<TradeQualificationRequirement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TradeRequirements.GetValueOrDefault(id));

    /// <summary>Adds a requirement to the store.</summary>
    public Task AddAsync(TradeQualificationRequirement requirement, CancellationToken cancellationToken = default)
    {
        _store.TradeRequirements[requirement.Id] = requirement;
        return Task.CompletedTask;
    }

    /// <summary>Updates a requirement in the store.</summary>
    public Task UpdateAsync(TradeQualificationRequirement requirement, CancellationToken cancellationToken = default)
    {
        _store.TradeRequirements[requirement.Id] = requirement;
        return Task.CompletedTask;
    }

    /// <summary>Removes a requirement from the store.</summary>
    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _store.TradeRequirements.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
