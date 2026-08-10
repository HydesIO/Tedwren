using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Application.Attendance;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;
using Xunit;
using SignInMethod = Tedwren.Abstractions.Common.SignInMethod;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the sign-in / sign-out rules (SF-13–SF-18) on <see cref="AttendanceService"/>: boundary
/// verification, the location policy, recording every attempt, no double-site, and sign-out duration.
/// </summary>
public sealed class AttendanceServiceTests
{
    private static readonly Guid Person = Guid.NewGuid();

    /// <summary>Builds the service over clean in-memory stores and returns them for seeding/assertions.</summary>
    private static (AttendanceService Service, InMemorySiteStore Sites, InMemoryAttendanceStore Attendance) CreateSut()
    {
        var siteStore = new InMemorySiteStore(seed: false);
        var attendanceStore = new InMemoryAttendanceStore();
        var service = new AttendanceService(
            new InMemorySiteRepository(siteStore),
            new InMemorySitePropertyRepository(siteStore),
            new InMemoryAttendanceRepository(attendanceStore));
        return (service, siteStore, attendanceStore);
    }

    private static Site AddSite(InMemorySiteStore store, Geofence? boundary, LocationPolicy policy = LocationPolicy.RecordAndFlag)
    {
        var site = new Site { CompanyId = Guid.NewGuid(), Name = "Alpha Site " + Guid.NewGuid(), Boundary = boundary, LocationPolicy = policy };
        store.Sites[site.Id] = site;
        return site;
    }

    private static SignInRequest SignIn(Guid siteId, double? lat, double? lng, Guid? propertyId = null) =>
        new(Person, siteId, propertyId, lat, lng, SignInMethod.QrScan);

    [Fact] // SF-14
    public async Task InsideBoundary_IsAccepted_AndPresent()
    {
        var (service, sites, _) = CreateSut();
        var site = AddSite(sites, new Geofence(51.5074, -0.1278, 150));

        var result = await service.SignInAsync(SignIn(site.Id, 51.5074, -0.1278));

        Assert.True(result.SignedIn);
        Assert.Equal("Accepted", result.Outcome);
        Assert.Single(await service.GetOnSiteAsync(site.Id));
    }

    [Fact] // SF-14 + SF-16
    public async Task OutsideBoundary_IsRefused_ButRecorded()
    {
        var (service, sites, _) = CreateSut();
        var site = AddSite(sites, new Geofence(51.5074, -0.1278, 150));

        var result = await service.SignInAsync(SignIn(site.Id, 51.5100, -0.1278)); // ~289 m away

        Assert.False(result.SignedIn);
        Assert.Equal("Refused", result.Outcome);
        Assert.Empty(await service.GetOnSiteAsync(site.Id));
        Assert.Single(await service.GetSiteRecordsAsync(site.Id, 10));   // the refusal is recorded (SF-16)
    }

    [Fact] // SF-15 default
    public async Task NoLocation_RecordAndFlag_IsFlaggedAndPresent()
    {
        var (service, sites, _) = CreateSut();
        var site = AddSite(sites, new Geofence(51.5074, -0.1278, 150), LocationPolicy.RecordAndFlag);

        var result = await service.SignInAsync(SignIn(site.Id, null, null));

        Assert.True(result.SignedIn);
        Assert.Equal("Flagged", result.Outcome);
    }

    [Fact] // SF-15 refuse
    public async Task NoLocation_Refuse_IsRefused()
    {
        var (service, sites, _) = CreateSut();
        var site = AddSite(sites, new Geofence(51.5074, -0.1278, 150), LocationPolicy.Refuse);

        var result = await service.SignInAsync(SignIn(site.Id, null, null));

        Assert.False(result.SignedIn);
        Assert.Equal("Refused", result.Outcome);
    }

    [Fact] // SF-18
    public async Task SignInAtSecondSite_WhilePresentElsewhere_IsRefused_NamingTheOther()
    {
        var (service, sites, _) = CreateSut();
        var alpha = AddSite(sites, new Geofence(51.5074, -0.1278, 150));
        var beta = AddSite(sites, new Geofence(52.4862, -1.8904, 150));

        await service.SignInAsync(SignIn(alpha.Id, 51.5074, -0.1278));
        var second = await service.SignInAsync(SignIn(beta.Id, 52.4862, -1.8904));

        Assert.False(second.SignedIn);
        Assert.Equal("Refused", second.Outcome);
        Assert.Equal(alpha.Name, second.SignedInElsewhere);
    }

    [Fact] // SF-17
    public async Task SignOut_RecordsDuration_AndClearsPresence()
    {
        var (service, sites, attendance) = CreateSut();
        var site = AddSite(sites, new Geofence(51.5074, -0.1278, 150));
        // A sign-in two hours ago, inserted directly to give a measurable duration.
        var signIn = new AttendanceRecord
        {
            PersonId = Person,
            SiteId = site.Id,
            Type = AttendanceEventType.SignIn,
            Outcome = AttendanceOutcome.Accepted,
            OccurredUtc = DateTimeOffset.UtcNow.AddHours(-2),
        };
        attendance.Records[signIn.Id] = signIn;

        var result = await service.SignOutAsync(new SignOutRequest(Person, site.Id, null, null, SignInMethod.QrScan));

        Assert.True(result.SignedOut);
        Assert.NotNull(result.DurationHours);
        Assert.InRange(result.DurationHours!.Value, 1.9, 2.1);
        Assert.Empty(await service.GetOnSiteAsync(site.Id));
    }

    [Fact] // SF-26 property boundary
    public async Task DispersedProperty_UsesPropertyBoundary()
    {
        var (service, sites, _) = CreateSut();
        var site = AddSite(sites, boundary: null);
        site.IsDispersed = true;
        var property = new SiteProperty { SiteId = site.Id, Address = "1 High St", Boundary = new Geofence(52.4862, -1.8904, 60) };
        sites.Properties[property.Id] = property;

        var result = await service.SignInAsync(SignIn(site.Id, 52.4862, -1.8904, property.Id));

        Assert.True(result.SignedIn);
        Assert.Equal("Accepted", result.Outcome);
    }
}
