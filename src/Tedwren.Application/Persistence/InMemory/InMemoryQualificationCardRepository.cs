using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IQualificationCardRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryQualificationCardRepository : IQualificationCardRepository
{
    private readonly InMemoryQualificationStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryQualificationCardRepository(InMemoryQualificationStore store) => _store = store;

    /// <summary>Returns all cards held by a person (including superseded ones), newest first.</summary>
    public Task<IReadOnlyList<QualificationCard>> GetByPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QualificationCard> cards = _store.Cards.Values
            .Where(c => c.PersonId == personId)
            .OrderByDescending(c => c.CreatedUtc)
            .ToList();
        return Task.FromResult(cards);
    }

    /// <summary>Returns current (non-superseded) cards that carry an expiry date.</summary>
    public Task<IReadOnlyList<QualificationCard>> GetCurrentWithExpiryAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QualificationCard> cards = _store.Cards.Values
            .Where(c => c.SupersededByCardId is null && c.ExpiresOn is not null)
            .ToList();
        return Task.FromResult(cards);
    }

    /// <summary>Returns a card by id, or null.</summary>
    public Task<QualificationCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Cards.GetValueOrDefault(id));

    /// <summary>Counts current (non-superseded) cards per qualification type.</summary>
    public Task<IReadOnlyDictionary<Guid, int>> GetHeldByCountsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<Guid, int> counts = _store.Cards.Values
            .Where(c => c.SupersededByCardId is null)
            .GroupBy(c => c.QualificationTypeId)
            .ToDictionary(g => g.Key, g => g.Count());
        return Task.FromResult(counts);
    }

    /// <summary>Adds a card to the store.</summary>
    public Task AddAsync(QualificationCard card, CancellationToken cancellationToken = default)
    {
        _store.Cards[card.Id] = card;
        return Task.CompletedTask;
    }

    /// <summary>Updates a card in the store (same instance is already referenced; kept for parity with Dapper).</summary>
    public Task UpdateAsync(QualificationCard card, CancellationToken cancellationToken = default)
    {
        _store.Cards[card.Id] = card;
        return Task.CompletedTask;
    }
}
