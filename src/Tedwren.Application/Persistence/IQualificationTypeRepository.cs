using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for the <see cref="QualificationType"/> library (SF-12).</summary>
public interface IQualificationTypeRepository
{
    /// <summary>Returns every qualification type, ordered by name.</summary>
    Task<IReadOnlyList<QualificationType>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a type by id, or null.</summary>
    Task<QualificationType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a new qualification type.</summary>
    Task AddAsync(QualificationType type, CancellationToken cancellationToken = default);

    /// <summary>Updates a qualification type's editable fields (name/category/issuer/validity).</summary>
    Task UpdateAsync(QualificationType type, CancellationToken cancellationToken = default);

    /// <summary>Removes a qualification type (guarded by the service to types nothing references).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
