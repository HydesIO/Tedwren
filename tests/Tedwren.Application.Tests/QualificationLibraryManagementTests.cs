using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Qualifications;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the platform-admin / tenant management of the accreditation library (SF-12 types + SF-11 trade→accreditation
/// map; Q21, R15): a platform admin owns the shared (global) rows, a tenant owns its own org-custom rows and never the
/// shared list, one tenant never sees another's custom rows, a type in use cannot be deleted, and Gate 3 reads the
/// company-scoped map.
/// </summary>
public sealed class QualificationLibraryManagementTests
{
    private static readonly Guid CompanyA = Guid.Parse("55555555-5555-4555-8555-000000000101");
    private static readonly Guid CompanyB = Guid.Parse("66666666-6666-4666-8666-000000000102");

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

    private static QualificationService Service(InMemoryQualificationStore store, Guid? companyId, bool platformAdmin = false) =>
        new(new InMemoryQualificationTypeRepository(store),
            new InMemoryQualificationCardRepository(store),
            new InMemoryTradeRequirementRepository(store),
            new StubCurrentUser(companyId, platformAdmin));

    [Fact] // A platform admin adds a shared (global) accreditation type; every tenant then sees it.
    public async Task CreateType_PlatformAdmin_IsGlobal()
    {
        var store = new InMemoryQualificationStore(seed: false);
        var platform = Service(store, companyId: null, platformAdmin: true);
        var tenant = Service(store, CompanyA);

        var id = await platform.CreateQualificationTypeAsync(new CreateQualificationTypeRequest("Confined Space", "Health & Safety", "City & Guilds", 36, false, Global: true));

        var seenByTenant = Assert.Single(await tenant.GetQualificationTypesForManagementAsync());
        Assert.Equal(id, seenByTenant.Id);
        Assert.True(seenByTenant.IsGlobal);
        Assert.Null(seenByTenant.CompanyId);
    }

    [Fact] // A tenant cannot add to the shared list (spec §9 ownership).
    public async Task CreateType_TenantGlobal_Throws()
    {
        var store = new InMemoryQualificationStore(seed: false);
        var tenant = Service(store, CompanyA);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tenant.CreateQualificationTypeAsync(new CreateQualificationTypeRequest("Nationally Shared", null, null, 0, false, Global: true)));
    }

    [Fact] // A tenant's custom type is org-scoped and invisible to another tenant (R15).
    public async Task CreateType_TenantCustom_IsIsolated()
    {
        var store = new InMemoryQualificationStore(seed: false);
        var a = Service(store, CompanyA);
        var b = Service(store, CompanyB);

        await a.CreateQualificationTypeAsync(new CreateQualificationTypeRequest("Acme Ticket", null, null, 12, false, Global: false));

        var mine = Assert.Single(await a.GetQualificationTypesForManagementAsync());
        Assert.Equal(CompanyA, mine.CompanyId);
        Assert.False(mine.IsGlobal);
        Assert.Empty(await b.GetQualificationTypesForManagementAsync());
    }

    [Fact] // A blank type name is rejected.
    public async Task CreateType_BlankName_Throws()
    {
        var store = new InMemoryQualificationStore(seed: false);
        var platform = Service(store, companyId: null, platformAdmin: true);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            platform.CreateQualificationTypeAsync(new CreateQualificationTypeRequest("   ", null, null, 0, false, Global: true)));
    }

    [Fact] // A tenant cannot edit a shared (global) type — only a platform admin can.
    public async Task UpdateType_GlobalByTenant_Throws()
    {
        var store = new InMemoryQualificationStore(seed: true);   // default library ships global rows
        var tenant = Service(store, CompanyA);
        var globalType = (await tenant.GetQualificationTypesForManagementAsync()).First(t => t.IsGlobal);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tenant.UpdateQualificationTypeAsync(globalType.Id, new UpdateQualificationTypeRequest("Renamed", null, null, 0, false)));
    }

    [Fact] // A type held by an operative's card cannot be deleted (guarded).
    public async Task DeleteType_HeldByCard_Throws()
    {
        var store = new InMemoryQualificationStore(seed: false);
        var platform = Service(store, companyId: null, platformAdmin: true);
        var id = await platform.CreateQualificationTypeAsync(new CreateQualificationTypeRequest("Rope Access", null, null, 24, false, Global: true));
        await platform.CaptureCardAsync(new CaptureCardRequest(Guid.NewGuid(), id, "R1", null, null, null, NeedsReview: false));

        await Assert.ThrowsAsync<InvalidOperationException>(() => platform.DeleteQualificationTypeAsync(id));
    }

    [Fact] // A type mapped to a trade cannot be deleted until the mapping is removed (guarded).
    public async Task DeleteType_MappedToTrade_Throws()
    {
        var store = new InMemoryQualificationStore(seed: false);
        var platform = Service(store, companyId: null, platformAdmin: true);
        var id = await platform.CreateQualificationTypeAsync(new CreateQualificationTypeRequest("Face Fit", null, null, 24, false, Global: true));
        await platform.CreateTradeRequirementAsync(new CreateTradeRequirementRequest("Asbestos Removal", id, LegalMandatory: true, ClientRequired: false, Global: true));

        await Assert.ThrowsAsync<InvalidOperationException>(() => platform.DeleteQualificationTypeAsync(id));
    }

    [Fact] // An unreferenced type deletes cleanly.
    public async Task DeleteType_Unreferenced_Succeeds()
    {
        var store = new InMemoryQualificationStore(seed: false);
        var platform = Service(store, companyId: null, platformAdmin: true);
        var id = await platform.CreateQualificationTypeAsync(new CreateQualificationTypeRequest("Temporary", null, null, 0, false, Global: true));

        await platform.DeleteQualificationTypeAsync(id);

        Assert.Empty(await platform.GetQualificationTypesForManagementAsync());
    }

    [Fact] // A map row resolves its accreditation name and carries its flags (SF-11).
    public async Task CreateRequirement_ResolvesNameAndFlags()
    {
        var store = new InMemoryQualificationStore(seed: false);
        var platform = Service(store, companyId: null, platformAdmin: true);
        var typeId = await platform.CreateQualificationTypeAsync(new CreateQualificationTypeRequest("Gas Safe", null, null, 60, false, Global: true));

        await platform.CreateTradeRequirementAsync(new CreateTradeRequirementRequest("Gas Engineer", typeId, LegalMandatory: true, ClientRequired: false, Global: true));

        var row = Assert.Single(await platform.GetTradeRequirementsAsync());
        Assert.Equal("Gas Engineer", row.Trade);
        Assert.Equal("Gas Safe", row.Accreditation);
        Assert.True(row.LegalMandatory);
        Assert.True(row.IsGlobal);
    }

    [Fact] // Mapping an unknown accreditation type is rejected.
    public async Task CreateRequirement_UnknownType_Throws()
    {
        var store = new InMemoryQualificationStore(seed: false);
        var platform = Service(store, companyId: null, platformAdmin: true);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            platform.CreateTradeRequirementAsync(new CreateTradeRequirementRequest("Gas Engineer", Guid.NewGuid(), true, false, Global: true)));
    }

    [Fact] // Updating a map row changes only its flags.
    public async Task UpdateRequirement_ChangesFlags()
    {
        var store = new InMemoryQualificationStore(seed: false);
        var platform = Service(store, companyId: null, platformAdmin: true);
        var typeId = await platform.CreateQualificationTypeAsync(new CreateQualificationTypeRequest("Gas Safe", null, null, 60, false, Global: true));
        await platform.CreateTradeRequirementAsync(new CreateTradeRequirementRequest("Gas Engineer", typeId, LegalMandatory: false, ClientRequired: false, Global: true));
        var row = Assert.Single(await platform.GetTradeRequirementsAsync());

        await platform.UpdateTradeRequirementAsync(row.Id, new UpdateTradeRequirementRequest(LegalMandatory: true, ClientRequired: true));

        var updated = Assert.Single(await platform.GetTradeRequirementsAsync());
        Assert.True(updated.LegalMandatory);
        Assert.True(updated.ClientRequired);
    }

    [Fact] // Deleting a map row removes it.
    public async Task DeleteRequirement_RemovesMapping()
    {
        var store = new InMemoryQualificationStore(seed: false);
        var platform = Service(store, companyId: null, platformAdmin: true);
        var typeId = await platform.CreateQualificationTypeAsync(new CreateQualificationTypeRequest("Gas Safe", null, null, 60, false, Global: true));
        await platform.CreateTradeRequirementAsync(new CreateTradeRequirementRequest("Gas Engineer", typeId, true, false, Global: true));
        var row = Assert.Single(await platform.GetTradeRequirementsAsync());

        await platform.DeleteTradeRequirementAsync(row.Id);

        Assert.Empty(await platform.GetTradeRequirementsAsync());
    }

    [Fact] // Gate 3: a Gas Engineer without Gas Safe is blocked; a valid Gas Safe card clears the mandatory requirement (spec §2).
    public async Task EvaluateGate3_GasEngineer_BlocksWithoutGasSafe()
    {
        var store = new InMemoryQualificationStore(seed: true);   // seeded map: Gas Engineer → CSCS + Gas Safe (mandatory)
        var service = Service(store, CompanyA);
        var person = Guid.NewGuid();
        var gasSafe = (await service.GetQualificationTypesAsync()).Single(t => t.Name == "Gas Safe").Id;
        var future = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1);

        var before = await service.EvaluateGate3Async(person, "Gas Engineer");
        Assert.False(before.Cleared);
        Assert.Contains(before.Requirements, r => r.Accreditation == "Gas Safe" && r.LegalMandatory && !r.Satisfied);

        await service.CaptureCardAsync(new CaptureCardRequest(person, gasSafe, "G1", null, null, future, NeedsReview: false));
        var after = await service.EvaluateGate3Async(person, "Gas Engineer");

        Assert.True(after.Cleared);   // only the mandatory Gas Safe blocks; CSCS is advisory
    }
}
