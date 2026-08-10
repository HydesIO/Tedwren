using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for the trade→required-qualification rules (SF-11).</summary>
public interface ITradeRequirementRepository
{
    /// <summary>Returns the requirements for a trade (case-insensitive match).</summary>
    Task<IReadOnlyList<TradeQualificationRequirement>> GetByTradeAsync(string trade, CancellationToken cancellationToken = default);

    /// <summary>Returns every trade requirement.</summary>
    Task<IReadOnlyList<TradeQualificationRequirement>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a new trade requirement.</summary>
    Task AddAsync(TradeQualificationRequirement requirement, CancellationToken cancellationToken = default);
}
