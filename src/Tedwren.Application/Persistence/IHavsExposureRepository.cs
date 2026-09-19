using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence for hand-arm vibration (HAVs) exposure records (PRD §8.2), scoped by company (R15). Append-only.</summary>
public interface IHavsExposureRepository
{
    /// <summary>Persists a new HAVs exposure record.</summary>
    Task AddAsync(HavsExposureRecord record, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's HAVs exposure records, newest first.</summary>
    Task<IReadOnlyList<HavsExposureRecord>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single HAVs exposure record by id, or null if none exists.</summary>
    Task<HavsExposureRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
