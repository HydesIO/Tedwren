using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.MasterData;
using Tedwren.Abstractions.Services;
using Tedwren.Application.MasterData;
using Tedwren.Application.Persistence.InMemory;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the compliance master-data service (Subcontractor Onboarding spec §5–§8): the shipped global lists
/// are served, a main contractor may add org-scoped custom entries but never the shared list, one tenant never
/// sees another's custom entries (R15), and a soft delete hides a value from pickers while a management read
/// still shows it.
/// </summary>
public sealed class MasterDataServiceTests
{
    private static readonly Guid CompanyA = Guid.Parse("55555555-5555-4555-8555-000000000001");
    private static readonly Guid CompanyB = Guid.Parse("66666666-6666-4666-8666-000000000002");
    private const string TestKey = "test-list";

    /// <summary>A fixed-tenant current-user stub so the service scopes to a company and knows platform-admin rights.</summary>
    private sealed class StubCurrentUser : ICurrentUserService
    {
        private readonly Guid? _companyId;
        private readonly bool _isPlatformAdmin;
        public StubCurrentUser(Guid? companyId, bool isPlatformAdmin = false)
        {
            _companyId = companyId;
            _isPlatformAdmin = isPlatformAdmin;
        }

        public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CurrentUserDto("Test User", "Administrator", _companyId, _isPlatformAdmin));
    }

    private static MasterDataService Service(InMemoryMasterListItemRepository repo, Guid? companyId, bool platformAdmin = false) =>
        new(repo, new StubCurrentUser(companyId, platformAdmin));

    [Fact] // The shipped global SSIP list is served in display order.
    public async Task GetList_ServesSeededGlobalValues()
    {
        var service = Service(new InMemoryMasterListItemRepository(), CompanyA);

        var ssip = await service.GetListAsync(MasterListKeys.SsipSchemes);

        Assert.NotEmpty(ssip);
        Assert.Contains(ssip, i => i.Value == "CHAS");
        Assert.All(ssip, i => Assert.True(i.IsGlobal));
        Assert.Equal(ssip.OrderBy(i => i.SortOrder).Select(i => i.Value), ssip.Select(i => i.Value));
    }

    [Fact] // A main contractor's custom entry is org-scoped and invisible to another tenant (R15).
    public async Task CustomEntry_IsScopedToOwningTenant()
    {
        var repo = new InMemoryMasterListItemRepository();
        var a = Service(repo, CompanyA);
        var b = Service(repo, CompanyB);

        await a.CreateAsync(new CreateMasterListItemRequest(TestKey, "Acme Bespoke Scheme", 0, Global: false));

        var seenByA = await a.GetListAsync(TestKey);
        var seenByB = await b.GetListAsync(TestKey);

        var mine = Assert.Single(seenByA);
        Assert.Equal("Acme Bespoke Scheme", mine.Value);
        Assert.Equal(CompanyA, mine.CompanyId);
        Assert.False(mine.IsGlobal);
        Assert.Empty(seenByB);
    }

    [Fact] // A tenant cannot write to the shared national list (spec §9 ownership); a platform admin can, and every tenant then sees it.
    public async Task GlobalEntry_PlatformAdminOnly()
    {
        var repo = new InMemoryMasterListItemRepository();
        var tenant = Service(repo, CompanyA);
        var platform = Service(repo, companyId: null, platformAdmin: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tenant.CreateAsync(new CreateMasterListItemRequest(TestKey, "Nationally Shared", 0, Global: true)));

        await platform.CreateAsync(new CreateMasterListItemRequest(TestKey, "Nationally Shared", 0, Global: true));

        var seenByTenant = await tenant.GetListAsync(TestKey);
        var shared = Assert.Single(seenByTenant);
        Assert.Equal("Nationally Shared", shared.Value);
        Assert.True(shared.IsGlobal);
    }

    [Fact] // A blank value is rejected.
    public async Task Create_BlankValue_Throws()
    {
        var service = Service(new InMemoryMasterListItemRepository(), CompanyA);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync(new CreateMasterListItemRequest(TestKey, "   ", 0, Global: false)));
    }

    [Fact] // A soft delete hides the value from the picker but leaves it visible (inactive) to a management read.
    public async Task Deactivate_HidesFromListButShowsInManagement()
    {
        var repo = new InMemoryMasterListItemRepository();
        var service = Service(repo, CompanyA);
        var id = await service.CreateAsync(new CreateMasterListItemRequest(TestKey, "Temporary Entry", 0, Global: false));

        await service.DeactivateAsync(id);

        Assert.Empty(await service.GetListAsync(TestKey));
        var managed = Assert.Single(await service.GetForManagementAsync(TestKey));
        Assert.False(managed.IsActive);
    }

    [Fact] // R15 — a tenant cannot edit another tenant's custom entry.
    public async Task Update_CrossTenant_Throws()
    {
        var repo = new InMemoryMasterListItemRepository();
        var a = Service(repo, CompanyA);
        var b = Service(repo, CompanyB);
        var id = await a.CreateAsync(new CreateMasterListItemRequest(TestKey, "Owned By A", 0, Global: false));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            b.UpdateAsync(id, new UpdateMasterListItemRequest("Hijacked", 0, true)));
    }

    [Fact] // A tenant cannot edit a shared (global) row — only a platform admin can.
    public async Task Update_GlobalByTenant_Throws()
    {
        var repo = new InMemoryMasterListItemRepository();
        var tenant = Service(repo, CompanyA);
        var seeded = (await tenant.GetListAsync(MasterListKeys.SsipSchemes)).First();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tenant.UpdateAsync(seeded.Id, new UpdateMasterListItemRequest("Renamed", 0, true)));
    }
}
