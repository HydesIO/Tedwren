using System.Collections.Concurrent;
using Tedwren.Application.Auth;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// In-memory <see cref="IEntitlementRepository"/> (API mock mode). Registered as a singleton so overrides
/// persist across requests within the host. Every company gets its <em>product</em> default bundle
/// (<c>ProductModuleBundles</c>, SF-22); the seed here adds a couple of purchased add-on overrides on top,
/// to exercise modules beyond a product's defaults without a purchase step.
/// </summary>
public sealed class InMemoryEntitlementRepository : IEntitlementRepository
{
    private readonly ConcurrentDictionary<(Guid CompanyId, string Key), bool> _overrides = new();

    /// <summary>Creates the store, adding demo add-on overrides on top of each tenant's product bundle.</summary>
    public InMemoryEntitlementRepository()
    {
        // Meridian (main contractor) has bought the Forms Library add-on (off in the MC bundle by default).
        _overrides[(AdminUserSeeder.SeedCompanyId, "forms")] = true;

        // Apex (subcontractor) has bought the Permits add-on, to demonstrate an override beyond the product bundle.
        _overrides[(AdminUserSeeder.SubcontractorSeedCompanyId, "permits")] = true;
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
}
