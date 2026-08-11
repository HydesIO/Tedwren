using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>
/// Persistence contract for <see cref="QualificationCard"/>. Cards are held against the person (SF-1
/// identity). Renewals add a new record and mark the old superseded — cards are never overwritten (SF-10).
/// </summary>
public interface IQualificationCardRepository
{
    /// <summary>Returns all cards held by a person (including superseded ones, for history).</summary>
    Task<IReadOnlyList<QualificationCard>> GetByPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Returns all cards held by any of the given people, in one read (avoids per-person N+1 in aggregations).</summary>
    Task<IReadOnlyList<QualificationCard>> GetByPersonsAsync(IReadOnlyCollection<Guid> personIds, CancellationToken cancellationToken = default);

    /// <summary>Returns current (non-superseded) cards that carry an expiry date — the expiry engine's input (SF-9).</summary>
    Task<IReadOnlyList<QualificationCard>> GetCurrentWithExpiryAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a card by id, or null.</summary>
    Task<QualificationCard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Counts current (non-superseded) cards per qualification type — the library's "held by" figures.</summary>
    Task<IReadOnlyDictionary<Guid, int>> GetHeldByCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a new card.</summary>
    Task AddAsync(QualificationCard card, CancellationToken cancellationToken = default);

    /// <summary>Persists confirmation/supersession changes to an existing card.</summary>
    Task UpdateAsync(QualificationCard card, CancellationToken cancellationToken = default);
}
