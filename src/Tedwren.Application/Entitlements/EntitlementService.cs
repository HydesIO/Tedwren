using Tedwren.Abstractions.Contracts.Entitlements;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;

namespace Tedwren.Application.Entitlements;

/// <summary>
/// The single authoritative entitlement check (Q2). It reads the catalogue and applies the company's
/// overrides; where a module has no override, the catalogue default applies. Unknown modules fail closed.
/// </summary>
public sealed class EntitlementService : IEntitlementService
{
    private readonly IEntitlementRepository _entitlements;

    /// <summary>Creates the service over the entitlement repository.</summary>
    public EntitlementService(IEntitlementRepository entitlements) => _entitlements = entitlements;

    /// <summary>Returns every catalogue module with the company's effective enabled state.</summary>
    public async Task<IReadOnlyList<ModuleEntitlementDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var overrides = await _entitlements.GetOverridesByCompanyAsync(companyId, cancellationToken);
        return ModuleCatalog.Modules
            .Select(m => new ModuleEntitlementDto(
                m.Key, m.Name, m.Description,
                overrides.TryGetValue(m.Key, out var enabled) ? enabled : m.DefaultEnabled))
            .ToList();
    }

    /// <summary>Whether a module is enabled for the company. Unknown modules are not enabled (fails closed, Q2).</summary>
    public async Task<bool> IsEnabledAsync(Guid companyId, string moduleKey, CancellationToken cancellationToken = default)
    {
        var module = ModuleCatalog.Find(moduleKey);
        if (module is null)
        {
            return false;
        }

        var overrides = await _entitlements.GetOverridesByCompanyAsync(companyId, cancellationToken);
        return overrides.TryGetValue(module.Key, out var enabled) ? enabled : module.DefaultEnabled;
    }

    /// <summary>Sets a company's entitlement for a known module; unknown modules are ignored (fails closed, Q2).</summary>
    public async Task SetEnabledAsync(Guid companyId, string moduleKey, bool enabled, CancellationToken cancellationToken = default)
    {
        var module = ModuleCatalog.Find(moduleKey);
        if (module is null)
        {
            return;
        }

        await _entitlements.SetAsync(companyId, module.Key, enabled, cancellationToken);
    }
}
