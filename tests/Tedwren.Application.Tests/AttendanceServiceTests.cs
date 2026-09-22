using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Application.Attendance;
using Tedwren.Application.Entitlements;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Application.Rams;
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
        var attendanceStore = new InMemoryAttendanceStore(seed: false);
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

    // ---- Gate 5 (Phase 7): the operative's own sign-in enforces a signed, approved live RAMS -----------------

    /// <summary>
    /// Builds the service with the shared <see cref="RamsGate"/> wired, over in-memory RAMS/config/entitlement stores,
    /// and a site + active engagement under one company (the site's MC). Returns the stores so a test seeds the RAMS
    /// chain. The location is inside the boundary so a sign-in would be Accepted but for the RAMS gate.
    /// </summary>
    private static (AttendanceService Service, Site Site, Guid Company, InMemorySubcontractorOnboardingConfigRepository Configs, InMemoryRamsRepository Rams, InMemoryRamsAcknowledgementRepository Acks)
        CreateRamsSut(bool hse = true)
    {
        var company = Guid.NewGuid();
        var siteStore = new InMemorySiteStore(seed: false);
        var attendanceStore = new InMemoryAttendanceStore(seed: false);
        var orgStore = new InMemoryOrganisationStore(seed: false);
        orgStore.Engagements[Guid.NewGuid()] = new Engagement { CompanyId = company, PersonId = Person, Name = "Alex Operative" };

        var entitlements = new InMemoryEntitlementRepository();
        if (hse)
        {
            entitlements.SetAsync(company, "hse", true).GetAwaiter().GetResult();
        }

        var configs = new InMemorySubcontractorOnboardingConfigRepository();
        var rams = new InMemoryRamsRepository();
        var acks = new InMemoryRamsAcknowledgementRepository();

        var site = new Site { CompanyId = company, Name = "Alpha Site " + Guid.NewGuid(), Boundary = new Geofence(51.5074, -0.1278, 150) };
        siteStore.Sites[site.Id] = site;

        var sites = new InMemorySiteRepository(siteStore);
        var engagements = new InMemoryEngagementRepository(orgStore);
        var ramsGate = new RamsGate(sites, new EntitlementService(entitlements), configs, rams, acks, engagements);
        var service = new AttendanceService(sites, new InMemorySitePropertyRepository(siteStore), new InMemoryAttendanceRepository(attendanceStore), engagements, ramsGate);
        return (service, site, company, configs, rams, acks);
    }

    [Fact] // Gate 5 (block + retry) — an unsigned approved live RAMS refuses the sign-in and returns the RAMS to sign.
    public async Task SignIn_IsBlocked_WhenRamsMustBeSigned()
    {
        var (service, site, company, configs, rams, _) = CreateRamsSut();
        var familyId = Guid.NewGuid();
        await configs.AddAsync(new SubcontractorOnboardingConfig
        {
            Id = Guid.NewGuid(), InviterCompanyId = company, SubcontractorCompanyId = company,
            RamsFamilyId = familyId, CreatedUtc = DateTimeOffset.UtcNow,
        });
        var live = new RamsSubmission
        {
            CompanyId = company, FamilyId = familyId, Version = 1, Reference = "R1",
            ContractorName = "Sub", Title = "RAMS", Status = RamsStatus.Approved, IsLive = true,
        };
        await rams.AddAsync(live);

        var result = await service.SignInAsync(SignIn(site.Id, 51.5074, -0.1278));   // inside boundary

        Assert.False(result.SignedIn);
        Assert.Equal("Refused", result.Outcome);
        Assert.Equal(live.Id, result.RamsToSignId);
        Assert.Single(await service.GetSiteRecordsAsync(site.Id, 10));   // the refusal is recorded (SF-16)
    }

    [Fact] // NotApplicable (no subcontractor RAMS obligation) — sign-in behaves exactly as before (Accepted inside the boundary).
    public async Task SignIn_IsUnchanged_WhenRamsNotApplicable()
    {
        var (service, site, _, _, _, _) = CreateRamsSut();   // hse held, but no config → NotApplicable

        var result = await service.SignInAsync(SignIn(site.Id, 51.5074, -0.1278));

        Assert.True(result.SignedIn);
        Assert.Equal("Accepted", result.Outcome);
        Assert.Null(result.RamsToSignId);
    }

    [Fact] // Signed — once the operative has signed the current live RAMS, their sign-in is admitted.
    public async Task SignIn_IsAdmitted_WhenRamsSigned()
    {
        var (service, site, company, configs, rams, acks) = CreateRamsSut();
        var familyId = Guid.NewGuid();
        await configs.AddAsync(new SubcontractorOnboardingConfig
        {
            Id = Guid.NewGuid(), InviterCompanyId = company, SubcontractorCompanyId = company,
            RamsFamilyId = familyId, CreatedUtc = DateTimeOffset.UtcNow,
        });
        await rams.AddAsync(new RamsSubmission
        {
            CompanyId = company, FamilyId = familyId, Version = 1, Reference = "R1",
            ContractorName = "Sub", Title = "RAMS", Status = RamsStatus.Approved, IsLive = true,
        });
        await acks.AddAsync(new RamsAcknowledgement
        {
            CompanyId = company, PersonId = Person, FamilyId = familyId, Version = 1, SignatureName = "Alex Operative",
        });

        var result = await service.SignInAsync(SignIn(site.Id, 51.5074, -0.1278));

        Assert.True(result.SignedIn);
        Assert.Equal("Accepted", result.Outcome);
        Assert.Null(result.RamsToSignId);
    }
}
