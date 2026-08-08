using System.Collections.Concurrent;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// In-memory <see cref="IEntitlementRepository"/> (API mock mode). Registered as a singleton so overrides
/// persist across requests within the host. Starts empty — every company gets the catalogue defaults.
/// </summary>
public sealed class InMemoryEntitlementRepository : IEntitlementRepository
{
    private readonly ConcurrentDictionary<(Guid CompanyId, string Key), bool> _overrides = new();

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
