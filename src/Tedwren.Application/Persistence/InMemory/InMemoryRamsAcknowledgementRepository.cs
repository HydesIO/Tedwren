using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IRamsAcknowledgementRepository"/>.</summary>
public sealed class InMemoryRamsAcknowledgementRepository : IRamsAcknowledgementRepository
{
    private readonly ConcurrentDictionary<Guid, RamsAcknowledgement> _acks = new();

    /// <summary>Persists a new RAMS acknowledgement.</summary>
    public Task AddAsync(RamsAcknowledgement acknowledgement, CancellationToken cancellationToken = default)
    {
        _acks[acknowledgement.Id] = acknowledgement;
        return Task.CompletedTask;
    }

    /// <summary>Returns the operative's most recent acknowledgement for a family, or null when they have never signed it.</summary>
    public Task<RamsAcknowledgement?> GetLatestForPersonAsync(Guid companyId, Guid personId, Guid familyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_acks.Values
            .Where(a => a.CompanyId == companyId && a.PersonId == personId && a.FamilyId == familyId)
            .OrderByDescending(a => a.SignedUtc)
            .FirstOrDefault());
}
