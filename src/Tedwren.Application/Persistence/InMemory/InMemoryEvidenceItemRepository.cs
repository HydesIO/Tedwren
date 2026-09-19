using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IEvidenceItemRepository"/> (M5).</summary>
public sealed class InMemoryEvidenceItemRepository : IEvidenceItemRepository
{
    private readonly ConcurrentDictionary<Guid, EvidenceItem> _items = new();

    public Task AddAsync(EvidenceItem item, CancellationToken cancellationToken = default)
    {
        _items[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task<EvidenceItem?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.TryGetValue(id, out var item) ? item : null);

    public Task<IReadOnlyList<EvidenceItem>> GetByPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EvidenceItem>>(
            _items.Values.Where(i => i.PersonId == personId).OrderByDescending(i => i.CapturedUtc).ToList());

    public Task<IReadOnlyList<EvidenceItem>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<EvidenceItem>>(
            _items.Values.Where(i => i.CompanyId == companyId).OrderByDescending(i => i.CapturedUtc).ToList());
}
