using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Sites;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the sites rules (SF-6/SF-14/SF-25/SF-26) on <see cref="SiteService"/> over a clean in-memory
/// store: recording sites is unlimited, boundaries round-trip, and adding a property makes a scheme dispersed.
/// </summary>
public sealed class SiteServiceTests
{
    private static readonly Guid CompanyId = Guid.NewGuid();

    private static SiteService CreateSut()
    {
        var store = new InMemorySiteStore(seed: false);
        return new SiteService(
            new InMemorySiteRepository(store),
            new InMemorySitePropertyRepository(store),
            new InMemoryAttendanceRepository(new InMemoryAttendanceStore(seed: false)),
            new InMemoryQualificationCardRepository(new InMemoryQualificationStore(seed: false)));
    }

    [Fact] // UAT-011 (MC-21) — a site manager sees only assigned sites; other roles see the whole tenant
    public async Task SiteManager_SeesOnlyAssignedSites_WhileAdministratorSeesAll()
    {
        var store = new InMemorySiteStore(seed: false);
        var props = new InMemorySitePropertyRepository(store);
        var attendance = new InMemoryAttendanceRepository(new InMemoryAttendanceStore(seed: false));
        var cards = new InMemoryQualificationCardRepository(new InMemoryQualificationStore(seed: false));
        var assignments = new InMemorySiteAssignmentRepository(store);
        var company = Guid.NewGuid();
        var managerId = Guid.NewGuid();

        SiteService For(string role, Guid userId) => new(
            new InMemorySiteRepository(store), props, attendance, cards,
            new FakeCurrentUser(new CurrentUserDto("Tester", role, company, UserId: userId)), assignments);

        var admin = For("Administrator", Guid.NewGuid());
        var siteA = await admin.CreateSiteAsync(new CreateSiteRequest(company, "Alpha Site", null, null, null, true, false, null));
        await admin.CreateSiteAsync(new CreateSiteRequest(company, "Beta Site", null, null, null, true, false, null));

        // Manager assigned to only one of the two sites.
        await assignments.ReplaceForUserAsync(managerId, new[] { siteA });
        var manager = For("SiteManager", managerId);

        var managerSites = await manager.GetSitesAsync();
        Assert.Equal("Alpha Site", Assert.Single(managerSites).Name);

        var adminSites = await admin.GetSitesAsync();
        Assert.Equal(2, adminSites.Count);   // administrators are never scoped

        // A manager with no assignments fails open (sees all) rather than an empty screen.
        var unassigned = For("SiteManager", Guid.NewGuid());
        Assert.Equal(2, (await unassigned.GetSitesAsync()).Count);
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        private readonly CurrentUserDto _user;
        public FakeCurrentUser(CurrentUserDto user) => _user = user;
        public Task<CurrentUserDto> GetCurrentAsync(CancellationToken cancellationToken = default) => Task.FromResult(_user);
    }

    [Fact] // SF-14
    public async Task CreateSite_WithBoundary_RoundTrips()
    {
        var service = CreateSut();
        await service.CreateSiteAsync(new CreateSiteRequest(
            CompanyId, "Test Site", "Acme", "London", "1 Test St", HasCompound: true, IsDispersed: false,
            new GeofenceDto(51.5074, -0.1278, 150)));

        var detail = await service.GetSiteAsync("test-site");

        Assert.NotNull(detail);
        Assert.False(detail!.IsDispersed);
        Assert.NotNull(detail.Boundary);
        Assert.Equal(150, detail.Boundary!.RadiusMetres);
    }

    [Fact] // SF-6
    public async Task RecordingSites_IsUnlimited()
    {
        var service = CreateSut();
        for (var i = 0; i < 25; i++)
        {
            await service.CreateSiteAsync(new CreateSiteRequest(
                CompanyId, $"Site {i}", null, null, null, HasCompound: true, IsDispersed: false, Boundary: null));
        }

        var sites = await service.GetSitesAsync();

        Assert.Equal(25, sites.Count);
    }

    [Fact] // SF-25 / SF-26
    public async Task AddProperty_MarksSchemeDispersed_AndIsListed()
    {
        var service = CreateSut();
        var siteId = await service.CreateSiteAsync(new CreateSiteRequest(
            CompanyId, "Riverside Retrofit", "Homes", "Birmingham", null, HasCompound: false, IsDispersed: false, Boundary: null));

        var propertyId = await service.AddPropertyAsync(new AddSitePropertyRequest(
            siteId, "1–12 Riverside Gardens", 12, new GeofenceDto(52.4862, -1.8904, 60)));

        Assert.NotNull(propertyId);
        var detail = await service.GetSiteAsync("riverside-retrofit");
        Assert.True(detail!.IsDispersed);           // adding a property makes it a dispersed scheme (SF-26)
        Assert.False(detail.HasCompound);           // no fixed point of presence (SF-25)
        var property = Assert.Single(detail.Properties);
        Assert.Equal(60, property.Boundary.RadiusMetres);

        var summary = (await service.GetSitesAsync()).Single();
        Assert.True(summary.IsDispersed);
        Assert.Equal(1, summary.PropertyCount);
    }

    [Fact]
    public async Task AddProperty_ToUnknownSite_ReturnsNull()
    {
        var service = CreateSut();

        var result = await service.AddPropertyAsync(new AddSitePropertyRequest(
            Guid.NewGuid(), "Nowhere", 1, new GeofenceDto(0, 0, 1)));

        Assert.Null(result);
    }

    [Fact] // SF-6 edit: updating a site persists its editable fields
    public async Task UpdateSite_PersistsChanges()
    {
        var service = CreateSut();
        var siteId = await service.CreateSiteAsync(new CreateSiteRequest(
            CompanyId, "Old Name", "Old Client", "London", "1 Old St", HasCompound: true, IsDispersed: false, Boundary: null));

        var ok = await service.UpdateSiteAsync(siteId, new UpdateSiteRequest(
            "New Name", "New Client", "Leeds", "2 New St", HasCompound: false, Boundary: null));

        Assert.True(ok);
        var detail = await service.GetSiteAsync("new-name");
        Assert.NotNull(detail);
        Assert.Equal("New Client", detail!.Client);
        Assert.Equal("Leeds", detail.Region);
        Assert.False(detail.HasCompound);
    }

    [Fact] // updating an unknown site returns false
    public async Task UpdateSite_Unknown_ReturnsFalse()
    {
        var service = CreateSut();

        var ok = await service.UpdateSiteAsync(Guid.NewGuid(), new UpdateSiteRequest(
            "Nope", null, null, null, HasCompound: true, Boundary: null));

        Assert.False(ok);
    }

    [Fact] // D4/MC-12: a site's operative count comes from the attendance log, not an invented number
    public async Task SiteOperatives_ComeFromAttendance()
    {
        var siteStore = new InMemorySiteStore(seed: false);
        var attendance = new InMemoryAttendanceRepository(new InMemoryAttendanceStore(seed: false));
        var service = new SiteService(
            new InMemorySiteRepository(siteStore), new InMemorySitePropertyRepository(siteStore),
            attendance, new InMemoryQualificationCardRepository(new InMemoryQualificationStore(seed: false)));

        var siteId = await service.CreateSiteAsync(new CreateSiteRequest(
            CompanyId, "Meridian Tower", null, null, null, HasCompound: true, IsDispersed: false, Boundary: null));

        // No attendance yet → no operatives, pending compliance (never invented).
        Assert.Equal(0, (await service.GetSitesAsync()).Single().Operatives);

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        foreach (var personId in new[] { p1, p2, p1 })   // p1 twice → still one distinct operative
        {
            await attendance.AddAsync(new Tedwren.Domain.Entities.AttendanceRecord
            {
                PersonId = personId,
                SiteId = siteId,
                Type = Tedwren.Domain.Enums.AttendanceEventType.SignIn,
                Outcome = Tedwren.Domain.Enums.AttendanceOutcome.Accepted,
            });
        }

        var summary = (await service.GetSitesAsync()).Single();
        Assert.Equal(2, summary.Operatives);
        Assert.Equal(Tedwren.Abstractions.Common.ComplianceState.Pending, summary.State);  // no cards → pending
    }
}
