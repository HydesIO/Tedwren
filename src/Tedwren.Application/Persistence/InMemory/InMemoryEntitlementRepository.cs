using System.Collections.Concurrent;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// In-memory <see cref="IEntitlementRepository"/> (API mock mode). Registered as a singleton so overrides
/// persist across requests within the host. Every company gets the catalogue defaults, except the well-known
/// demo company, which is seeded with the paid Forms Library module enabled so the mock/demo and the API tests
/// exercise the module without a purchase step.
/// </summary>
public sealed class InMemoryEntitlementRepository : IEntitlementRepository
{
    /// <summary>The seeded demo company (matches the other in-memory stores and the test auth bypass).</summary>
    private static readonly Guid DemoCompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    private readonly ConcurrentDictionary<(Guid CompanyId, string Key), bool> _overrides = new();

    /// <summary>Creates the store, enabling the Forms Library module for the demo company.</summary>
    public InMemoryEntitlementRepository()
    {
        _overrides[(DemoCompanyId, "forms")] = true;
    }

    /// <summary>Returns the company's module overrides.</summary>
    public Task<IReadOnlyDictionary<string, bool>> GetOverridesByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, bool> map = _overrides
            .Where(kvp => kvp.Key.CompanyId == companyId)
            .ToDictionary(kvp => kvp.Key.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
        return Task.FromResult(map);
    }

    /// <summary>Sets a company's entitlement for a module.</summary>
    public Task SetAsync(Guid companyId, string moduleKey, bool enabled, CancellationToken cancellationToken = default)
    {
        _overrides[(companyId, moduleKey)] = enabled;
        return Task.CompletedTask;
    }

    /// <summary>Removes all overrides for a company (demo-data teardown).</summary>
    public Task ClearForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        foreach (var key in _overrides.Keys.Where(k => k.CompanyId == companyId).ToList())
        {
            _overrides.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }
}
