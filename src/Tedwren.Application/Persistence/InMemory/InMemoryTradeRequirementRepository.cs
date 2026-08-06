using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="ITradeRequirementRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryTradeRequirementRepository : ITradeRequirementRepository
{
    private readonly InMemoryQualificationStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryTradeRequirementRepository(InMemoryQualificationStore store) => _store = store;

    /// <summary>Returns the requirements for a trade (case-insensitive).</summary>
    public Task<IReadOnlyList<TradeQualificationRequirement>> GetByTradeAsync(string trade, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TradeQualificationRequirement> requirements = _store.TradeRequirements
            .Where(r => string.Equals(r.Trade, trade, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(requirements);
    }

    /// <summary>Returns every requirement.</summary>
    public Task<IReadOnlyList<TradeQualificationRequirement>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TradeRequirements);

    /// <summary>Not supported on the seeded in-memory store; requirements come from the default library.</summary>
    public Task AddAsync(TradeQualificationRequirement requirement, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("In-memory trade requirements are seeded from the default library.");
}
