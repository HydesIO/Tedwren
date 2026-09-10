using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for shareable, tokenised induction links (UAT-018, MC-1/MC-2).</summary>
public interface IInductionLinkRepository
{
    /// <summary>Persists a new induction link.</summary>
    Task AddAsync(InductionLink link, CancellationToken cancellationToken = default);

    /// <summary>Returns the link for a token, or null.</summary>
    Task<InductionLink?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing link (status, started session).</summary>
    Task UpdateAsync(InductionLink link, CancellationToken cancellationToken = default);
}
