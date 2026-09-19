using Tedwren.Application.Mobile;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies <see cref="MobileAttendanceService.GetCurrentAsync"/> (M4): it reports the operative's current open
/// sign-in (site + since when, SF-18) and null when they are not signed in anywhere.
/// </summary>
public sealed class MobileAttendanceServiceTests
{
    private static readonly Guid Person = Guid.NewGuid();

    /// <summary>Builds the service over clean in-memory stores and returns them for seeding.</summary>
    private static (MobileAttendanceService Service, InMemorySiteStore Sites, InMemoryAttendanceStore Attendance) CreateSut()
    {
        var siteStore = new InMemorySiteStore(seed: false);
        var attendanceStore = new InMemoryAttendanceStore(seed: false);
        var service = new MobileAttendanceService(
            new InMemoryAttendanceRepository(attendanceStore),
            new InMemorySiteRepository(siteStore));
        return (service, siteStore, attendanceStore);
    }

    [Fact]
    public async Task GetCurrent_WhenNotSignedIn_ReturnsNull()
    {
        var (service, _, _) = CreateSut();

        Assert.Null(await service.GetCurrentAsync(Person));
    }

    [Fact]
    public async Task GetCurrent_WhenSignedIn_ReturnsSiteNameAndSince()
    {
        var (service, sites, attendance) = CreateSut();
        var site = new Site { CompanyId = Guid.NewGuid(), Name = "Riverside Works", Boundary = null };
        sites.Sites[site.Id] = site;
        var since = DateTimeOffset.UtcNow.AddHours(-1);
        var open = new AttendanceRecord
        {
            PersonId = Person,
            SiteId = site.Id,
            Type = AttendanceEventType.SignIn,
            Outcome = AttendanceOutcome.Accepted,
            OccurredUtc = since,
        };
        attendance.Records[open.Id] = open;

        var current = await service.GetCurrentAsync(Person);

        Assert.NotNull(current);
        Assert.Equal(site.Id, current!.SiteId);
        Assert.Equal("Riverside Works", current.SiteName);
        Assert.Equal(since, current.SinceUtc);
    }
}
