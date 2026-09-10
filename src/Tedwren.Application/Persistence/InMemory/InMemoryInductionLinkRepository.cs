using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IInductionLinkRepository"/> (UAT-018).</summary>
public sealed class InMemoryInductionLinkRepository : IInductionLinkRepository
{
    private readonly ConcurrentDictionary<Guid, InductionLink> _links = new();

    /// <summary>Persists a new induction link.</summary>
    public Task AddAsync(InductionLink link, CancellationToken cancellationToken = default)
    {
        _links[link.Id] = link;
        return Task.CompletedTask;
    }

    /// <summary>Returns the link for a token, or null.</summary>
    public Task<InductionLink?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        Task.FromResult(_links.Values.FirstOrDefault(l => l.Token == token));

    /// <summary>Persists changes to an existing link.</summary>
    public Task UpdateAsync(InductionLink link, CancellationToken cancellationToken = default)
    {
        _links[link.Id] = link;
        return Task.CompletedTask;
    }
}
