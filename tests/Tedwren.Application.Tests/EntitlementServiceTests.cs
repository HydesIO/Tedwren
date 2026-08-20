using Tedwren.Application.Entitlements;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the authoritative, fail-closed module-entitlement check (Q2, SF-22): product-bundle defaults apply
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

    /// <summary>Builds a product-aware service over a company of the given product, returning the company id.</summary>
    private static (EntitlementService Service, Guid CompanyId) CreateForProduct(OrgType orgType)
    {
        var store = new InMemoryOrganisationStore(seed: false);
        var companies = new InMemoryCompanyRepository(store);
        var company = new Company { Name = "Acme", OrgType = orgType };
        companies.AddAsync(company).GetAwaiter().GetResult();
        return (new EntitlementService(new InMemoryEntitlementRepository(), companies), company.Id);
    }

    [Theory] // Strict PRD split: the product decides the default bundle (SF-22, SUB-11).
    [InlineData(OrgType.Subcontractor, "time", true)]
    [InlineData(OrgType.Subcontractor, "inductions", false)]
    [InlineData(OrgType.MainContractor, "inductions", true)]
    [InlineData(OrgType.MainContractor, "time", false)]
    [InlineData(OrgType.Subcontractor, "permits", false)]   // off for both by default
    [InlineData(OrgType.MainContractor, "permits", false)]
    [InlineData(OrgType.Subcontractor, "workforce", true)]  // foundation, on for both
    [InlineData(OrgType.MainContractor, "workforce", true)]
    public async Task Default_FollowsTheProductBundle(OrgType orgType, string module, bool expected)
    {
        var (service, companyId) = CreateForProduct(orgType);
        Assert.Equal(expected, await service.IsEnabledAsync(companyId, module));
    }

    [Fact] // A per-company override still wins over the product default.
    public async Task Override_WinsOverProductDefault()
    {
        var store = new InMemoryOrganisationStore(seed: false);
        var companies = new InMemoryCompanyRepository(store);
        var company = new Company { Name = "Acme", OrgType = OrgType.Subcontractor };
        await companies.AddAsync(company);
        var overrides = new InMemoryEntitlementRepository();
        await overrides.SetAsync(company.Id, "inductions", enabled: true);   // subcontractor default is off
        var service = new EntitlementService(overrides, companies);

        Assert.True(await service.IsEnabledAsync(company.Id, "inductions"));
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
