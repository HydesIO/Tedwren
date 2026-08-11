namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for the reference-data option lists (trades, company types, permit types, regions).</summary>
public interface IReferenceDataRepository
{
    /// <summary>Returns the ordered values for a reference list, or an empty list for an unknown key.</summary>
    Task<IReadOnlyList<string>> GetValuesAsync(string listKey, CancellationToken cancellationToken = default);
}
