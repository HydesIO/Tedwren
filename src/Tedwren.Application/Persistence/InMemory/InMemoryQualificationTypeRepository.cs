using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IQualificationTypeRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryQualificationTypeRepository : IQualificationTypeRepository
{
    private readonly InMemoryQualificationStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryQualificationTypeRepository(InMemoryQualificationStore store) => _store = store;

    /// <summary>Returns all types ordered by name.</summary>
    public Task<IReadOnlyList<QualificationType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<QualificationType> types = _store.Types.Values.OrderBy(t => t.Name).ToList();
        return Task.FromResult(types);
    }

    /// <summary>Returns a type by id, or null.</summary>
    public Task<QualificationType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Types.GetValueOrDefault(id));

    /// <summary>Adds a type to the store.</summary>
    public Task AddAsync(QualificationType type, CancellationToken cancellationToken = default)
    {
        _store.Types[type.Id] = type;
        return Task.CompletedTask;
    }
}
