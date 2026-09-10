using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for trade self-service onboarding invitations (UAT-023, SUB-4/MC-27).</summary>
public interface ITradeInviteRepository
{
    /// <summary>Persists a new trade invite.</summary>
    Task AddAsync(TradeInvite invite, CancellationToken cancellationToken = default);

    /// <summary>Returns the invite for a token, or null.</summary>
    Task<TradeInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Returns the invite by id, or null.</summary>
    Task<TradeInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns the invites created by an inviting (main-contractor) company — the review queue scope (R15).</summary>
    Task<IReadOnlyList<TradeInvite>> GetByInviterCompanyAsync(Guid inviterCompanyId, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing invite (status, submission, decision).</summary>
    Task UpdateAsync(TradeInvite invite, CancellationToken cancellationToken = default);
}
