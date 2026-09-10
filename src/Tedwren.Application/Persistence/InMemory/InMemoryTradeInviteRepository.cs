using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="ITradeInviteRepository"/> (UAT-023).</summary>
public sealed class InMemoryTradeInviteRepository : ITradeInviteRepository
{
    private readonly ConcurrentDictionary<Guid, TradeInvite> _invites = new();

    /// <summary>Persists a new trade invite.</summary>
    public Task AddAsync(TradeInvite invite, CancellationToken cancellationToken = default)
    {
        _invites[invite.Id] = invite;
        return Task.CompletedTask;
    }

    /// <summary>Returns the invite for a token, or null.</summary>
    public Task<TradeInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        Task.FromResult(_invites.Values.FirstOrDefault(i => i.Token == token));

    /// <summary>Returns the invite by id, or null.</summary>
    public Task<TradeInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_invites.GetValueOrDefault(id));

    /// <summary>Returns the invites created by an inviting company, newest first.</summary>
    public Task<IReadOnlyList<TradeInvite>> GetByInviterCompanyAsync(Guid inviterCompanyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TradeInvite> results = _invites.Values
            .Where(i => i.InviterCompanyId == inviterCompanyId)
            .OrderByDescending(i => i.SubmittedUtc ?? i.CreatedUtc)
            .ToList();
        return Task.FromResult(results);
    }

    /// <summary>Persists changes to an existing invite.</summary>
    public Task UpdateAsync(TradeInvite invite, CancellationToken cancellationToken = default)
    {
        _invites[invite.Id] = invite;
        return Task.CompletedTask;
    }
}
