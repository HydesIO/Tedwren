using Tedwren.Abstractions.Contracts.Sites;
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
            new InMemoryAttendanceRepository(new InMemoryAttendanceStore()),
            new InMemoryQualificationCardRepository(new InMemoryQualificationStore(seed: false)));
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

    [Fact] // D4/MC-12: a site's operative count comes from the attendance log, not an invented number
    public async Task SiteOperatives_ComeFromAttendance()
    {
        var siteStore = new InMemorySiteStore(seed: false);
        var attendance = new InMemoryAttendanceRepository(new InMemoryAttendanceStore());
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
