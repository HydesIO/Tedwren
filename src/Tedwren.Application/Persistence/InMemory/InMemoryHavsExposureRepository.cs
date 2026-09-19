using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IHavsExposureRepository"/>.</summary>
public sealed class InMemoryHavsExposureRepository : IHavsExposureRepository
{
    private readonly ConcurrentDictionary<Guid, HavsExposureRecord> _records = new();

    /// <summary>Persists a new HAVs exposure record.</summary>
    public Task AddAsync(HavsExposureRecord record, CancellationToken cancellationToken = default)
    {
        _records[record.Id] = record;
        return Task.CompletedTask;
    }

    /// <summary>Returns a company's HAVs exposure records, newest first.</summary>
    public Task<IReadOnlyList<HavsExposureRecord>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HavsExposureRecord> rows = _records.Values
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.RecordedUtc)
            .ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns a single HAVs exposure record by id, or null if none exists.</summary>
    public Task<HavsExposureRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_records.TryGetValue(id, out var record) ? record : null);
}
