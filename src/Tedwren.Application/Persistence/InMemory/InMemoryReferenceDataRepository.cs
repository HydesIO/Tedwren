using Tedwren.Abstractions.Services;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// Test-only in-memory <see cref="IReferenceDataRepository"/> seeded with the canonical option lists. The
/// same values are seeded into the database by migration script 012; this double keeps the suite DB-free.
/// </summary>
public sealed class InMemoryReferenceDataRepository : IReferenceDataRepository
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Lists =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [ReferenceListKeys.CompanyTypes] = new[]
            {
                "Main Contractor", "Subcontractor", "Labour Agency", "Consultant",
            },
            [ReferenceListKeys.Trades] = new[]
            {
                "General Build", "Groundworks", "Mechanical & Electrical", "Scaffolding",
                "Fit-Out", "Civil Engineering", "Roofing", "Demolition", "Cladding",
            },
            [ReferenceListKeys.PermitTypes] = new[]
            {
                "Hot Works", "Confined Space", "Working at Height", "Excavation",
                "Electrical Isolation", "Lifting Operation",
            },
            [ReferenceListKeys.Regions] = new[]
            {
                "London", "Manchester", "Leeds", "Bristol", "Birmingham", "Glasgow", "Cardiff",
            },
        };

    /// <summary>Returns the seeded values for a reference list, or an empty list for an unknown key.</summary>
    public Task<IReadOnlyList<string>> GetValuesAsync(string listKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(Lists.TryGetValue(listKey, out var values) ? values : Array.Empty<string>());
}
