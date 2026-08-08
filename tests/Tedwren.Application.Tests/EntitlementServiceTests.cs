using Tedwren.Application.Entitlements;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the authoritative, fail-closed module-entitlement check (Q2, SF-22): catalogue defaults apply
/// where a company has no override, an override wins over the default, and unknown modules are never enabled.
/// </summary>
public sealed class EntitlementServiceTests
{
    private static readonly Guid Company = Guid.NewGuid();

    private static EntitlementService CreateService(out InMemoryEntitlementRepository repository)
    {
        repository = new InMemoryEntitlementRepository();
        return new EntitlementService(repository);
    }

    [Fact]
    public async Task GetForCompany_ReturnsEveryCatalogueModule()
    {
        var service = CreateService(out _);

        var modules = await service.GetForCompanyAsync(Company);

        Assert.Equal(ModuleCatalog.Modules.Count, modules.Count);
    }

    [Fact]
    public async Task IsEnabled_UsesCatalogueDefault_WhenNoOverride()
    {
        var service = CreateService(out _);

        // "workforce" defaults on, "time" defaults off.
        Assert.True(await service.IsEnabledAsync(Company, "workforce"));
        Assert.False(await service.IsEnabledAsync(Company, "time"));
    }

    [Fact]
    public async Task IsEnabled_OverrideWinsOverDefault()
    {
        var service = CreateService(out var repository);
        await repository.SetAsync(Company, "time", enabled: true);
        await repository.SetAsync(Company, "workforce", enabled: false);

        Assert.True(await service.IsEnabledAsync(Company, "time"));
        Assert.False(await service.IsEnabledAsync(Company, "workforce"));
    }

    [Fact]
    public async Task IsEnabled_UnknownModule_FailsClosed()
    {
        var service = CreateService(out _);

        Assert.False(await service.IsEnabledAsync(Company, "does-not-exist"));
    }

    [Fact]
    public async Task Override_IsScopedToItsCompany()
    {
        var service = CreateService(out var repository);
        await repository.SetAsync(Company, "time", enabled: true);

        // A different company still sees the catalogue default (off).
        Assert.False(await service.IsEnabledAsync(Guid.NewGuid(), "time"));
    }
}
