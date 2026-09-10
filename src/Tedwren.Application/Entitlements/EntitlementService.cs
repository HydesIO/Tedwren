using Tedwren.Abstractions.Contracts.Entitlements;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Entitlements;

/// <summary>
/// The single authoritative entitlement check (Q2). It reads the catalogue and applies the company's
/// overrides; where a module has no override, the <em>product</em> default applies (the strict PRD split in
/// <see cref="ProductModuleBundles"/>, SF-22) — falling back to the catalogue default only when the company
/// has no product. Unknown modules fail closed.
/// </summary>
public sealed class EntitlementService : IEntitlementService
{
    private readonly IEntitlementRepository _entitlements;
    private readonly ICompanyRepository? _companies;

    /// <summary>
    /// Creates the service over the entitlement repository and (optionally) the company repository. The company
    /// repository is what makes defaults product-aware; when it is absent (e.g. a focused unit test), defaults
    /// fall back to the catalogue's own <see cref="ModuleCatalog.CatalogModule.DefaultEnabled"/>.
    /// </summary>
    public EntitlementService(IEntitlementRepository entitlements, ICompanyRepository? companies = null)
    {
        _entitlements = entitlements;
        _companies = companies;
    }

    /// <summary>Returns every catalogue module with the company's effective enabled state.</summary>
    public async Task<IReadOnlyList<ModuleEntitlementDto>> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var overrides = await _entitlements.GetOverridesByCompanyAsync(companyId, cancellationToken);
        var orgType = await ResolveOrgTypeAsync(companyId, cancellationToken);
        return ModuleCatalog.Modules
            .Select(m => new ModuleEntitlementDto(
                m.Key, m.Name, m.Description,
                overrides.TryGetValue(m.Key, out var enabled) ? enabled : DefaultFor(orgType, m)))
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
        if (overrides.TryGetValue(module.Key, out var enabled))
        {
            return enabled;
        }

        var orgType = await ResolveOrgTypeAsync(companyId, cancellationToken);
        return DefaultFor(orgType, module);
    }

    /// <summary>The default enabled state for a module: the product bundle when the company has a product,
    /// else the catalogue default (backward compatible for product-less companies).</summary>
    private static bool DefaultFor(OrgType? orgType, ModuleCatalog.CatalogModule module) =>
        orgType is { } product ? ProductModuleBundles.IsDefaultEnabled(product, module.Key) : module.DefaultEnabled;

    /// <summary>Resolves the company's product, or null when unknown / no company repository is available.</summary>
    private async Task<OrgType?> ResolveOrgTypeAsync(Guid companyId, CancellationToken cancellationToken)
    {
        if (_companies is null)
        {
            return null;
        }

        var company = await _companies.GetByIdAsync(companyId, cancellationToken);
        return company?.OrgType;
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
