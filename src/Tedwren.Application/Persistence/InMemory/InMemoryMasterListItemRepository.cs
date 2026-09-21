using System.Collections.Concurrent;
using Tedwren.Application.MasterData;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Test-only in-memory <see cref="IMasterListItemRepository"/> (spec §5–§8), seeded with the shared global lists
/// from <see cref="MasterListSeed"/> so the InMemory data source behaves like a migrated database.
/// </summary>
public sealed class InMemoryMasterListItemRepository : IMasterListItemRepository
{
    private readonly ConcurrentDictionary<Guid, MasterListItem> _items = new();

    /// <summary>Creates the repository and seeds the shared (global) master-list values.</summary>
    public InMemoryMasterListItemRepository()
    {
        foreach (var (listKey, values) in MasterListSeed.Values)
        {
            var order = 0;
            foreach (var value in values)
            {
                var item = new MasterListItem
                {
                    Id = Guid.NewGuid(),
                    ListKey = listKey,
                    CompanyId = null,
                    Value = value,
                    SortOrder = order++,
                    IsActive = true,
                    CreatedUtc = DateTimeOffset.UtcNow,
                };
                _items[item.Id] = item;
            }
        }
    }

    /// <summary>Returns a value by id, or null.</summary>
    public Task<MasterListItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.GetValueOrDefault(id));

    /// <summary>Returns the values for a list visible to a caller (global + own-org), ordered by SortOrder then Value.</summary>
    public Task<IReadOnlyList<MasterListItem>> GetByKeyAsync(
        string listKey, Guid? companyId, bool includeInactive, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MasterListItem> results = _items.Values
            .Where(i => i.ListKey == listKey
                && (i.CompanyId is null || i.CompanyId == companyId)
                && (includeInactive || i.IsActive))
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult(results);
    }

    /// <summary>Persists a new value.</summary>
    public Task AddAsync(MasterListItem item, CancellationToken cancellationToken = default)
    {
        _items[item.Id] = item;
        return Task.CompletedTask;
    }

    /// <summary>Persists changes to an existing value.</summary>
    public Task UpdateAsync(MasterListItem item, CancellationToken cancellationToken = default)
    {
        _items[item.Id] = item;
        return Task.CompletedTask;
    }
}
