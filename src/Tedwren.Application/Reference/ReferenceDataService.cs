using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;

namespace Tedwren.Application.Reference;

/// <summary>Store-agnostic reference-data service: serves the fixed form option lists from the repository.</summary>
public sealed class ReferenceDataService : IReferenceDataService
{
    private readonly IReferenceDataRepository _repository;

    /// <summary>Creates the service over the reference-data repository.</summary>
    public ReferenceDataService(IReferenceDataRepository repository) => _repository = repository;

    /// <summary>Returns the ordered values for a reference list, or an empty list for an unknown key.</summary>
    public async Task<IReadOnlyList<string>> GetListAsync(string listKey, CancellationToken cancellationToken = default) =>
        await _repository.GetValuesAsync(listKey, cancellationToken);
}
