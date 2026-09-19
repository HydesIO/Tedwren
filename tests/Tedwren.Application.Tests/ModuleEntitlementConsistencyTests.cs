using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tedwren.Application.DemoData;
using Tedwren.Application.Entitlements;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Guards the module-entitlement system's internal consistency (SF-22, Q2). Two properties must hold so the gated
/// module selection actually works and stays complete as modules are added:
/// <list type="bullet">
/// <item>every module key the platform gates/defaults on must be a real <see cref="ModuleCatalog"/> key — a typo'd
/// or renamed key silently fails closed (<c>IsEnabledAsync</c> returns false for unknown keys), so the gate never
/// fires (this is what made the site-entry RAMS check and two demo overrides dead);</item>
/// <item>the admin "Modules &amp; entitlements" list is built from <see cref="EntitlementService.GetForCompanyAsync"/>,
/// which must surface every catalogue module so all modules — new and old — are toggleable.</item>
/// </list>
/// A module newly added to the catalogue is covered automatically.
/// </summary>
public sealed class ModuleEntitlementConsistencyTests
{
    private static readonly HashSet<string> CatalogKeys =
        ModuleCatalog.Modules.Select(m => m.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

    [Fact] // The admin toggle list == the full catalogue, so every module (new and old) is accessible there.
    public async Task AdminModuleList_ContainsEveryCatalogueModule()
    {
        var service = new EntitlementService(new InMemoryEntitlementRepository());

        var listed = (await service.GetForCompanyAsync(Guid.NewGuid()))
            .Select(m => m.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(CatalogKeys, listed);
    }

    [Fact] // A product default bundle keyed on a non-catalogue module is a dead default that never resolves.
    public void ProductBundleKeys_AreAllInTheCatalogue()
    {
        var unknown = ProductModuleBundles.AllConfiguredKeys.Where(k => !CatalogKeys.Contains(k)).ToList();

        Assert.True(unknown.Count == 0,
            "Product bundle module key(s) not in ModuleCatalog (dead default): " + string.Join(", ", unknown));
    }

    [Fact] // The demo seed switching on a non-catalogue module is a dead write (was "timesheets"/"compliance-packs").
    public void DemoSeedModuleKeys_AreAllInTheCatalogue()
    {
        var demoKeys = DemoDataPlanBuilder.Build().EnabledModules.Select(e => e.ModuleKey).Distinct();

        var unknown = demoKeys.Where(k => !CatalogKeys.Contains(k)).ToList();

        Assert.True(unknown.Count == 0,
            "Demo seed module key(s) not in ModuleCatalog (dead override): " + string.Join(", ", unknown));
    }
}
