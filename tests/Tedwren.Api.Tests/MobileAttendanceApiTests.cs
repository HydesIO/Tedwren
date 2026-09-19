using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Api.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Api.Tests;

/// <summary>
/// Verifies the M4 operative attendance surface: an operative signs in/out only at their own company's sites
/// (R15), every attempt is recorded including refusals (SF-16), the geofence decides the outcome (SF-14), a
/// worker cannot be present at two sites at once (SF-18), sign-out returns the duration (SF-17), and a console
/// token cannot reach the mobile attendance endpoints.
/// </summary>
public sealed class MobileAttendanceApiTests
{
    // A small London boundary; the centre point is inside, the far point well outside (~11 km).
    private const double SiteLat = 51.5074;
    private const double SiteLng = -0.1278;
    private const double Radius = 150;
    private const double OutsideLat = 51.6000;
    private const double OutsideLng = -0.2000;

    [Fact]
    public async Task Sign_in_inside_the_boundary_is_recorded_present()
    {
        var (factory, token, companyId, _) = CreateOperativeHost();
        using (factory)
        {
            var siteId = SeedSite(factory, companyId, "Riverside Works", new Geofence(SiteLat, SiteLng, Radius));
            var client = Authed(factory, token);

            var response = await client.PostAsJsonAsync("/api/mobile/attendance/sign-in",
                new MobileSignInRequest(siteId, null, SiteLat, SiteLng));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<SignInResult>();
            Assert.True(result!.SignedIn);
            Assert.Equal("Accepted", result.Outcome);
        }
    }

    [Fact]
    public async Task Sign_in_outside_the_boundary_is_refused_but_still_recorded()
    {
        var (factory, token, companyId, personId) = CreateOperativeHost();
        using (factory)
        {
            var siteId = SeedSite(factory, companyId, "Riverside Works", new Geofence(SiteLat, SiteLng, Radius));
            var client = Authed(factory, token);

            var response = await client.PostAsJsonAsync("/api/mobile/attendance/sign-in",
                new MobileSignInRequest(siteId, null, OutsideLat, OutsideLng));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<SignInResult>();
            Assert.False(result!.SignedIn);
            Assert.Equal("Refused", result.Outcome);

            // SF-16: the refused attempt is still written to the append-only log.
            using var scope = factory.Services.CreateScope();
            var records = await scope.ServiceProvider.GetRequiredService<IAttendanceRepository>().GetBySiteAsync(siteId, 10);
            Assert.Contains(records, r => r.PersonId == personId && r.Outcome == AttendanceOutcome.Refused);
        }
    }

    [Fact]
    public async Task Sign_in_at_another_companys_site_is_forbidden()
    {
        var (factory, token, _, _) = CreateOperativeHost();
        using (factory)
        {
            // A site owned by a different company — the token's company scoping must refuse it (R15).
            var otherCompanyId = SeedCompany(factory, "Rival Contractor");
            var otherSiteId = SeedSite(factory, otherCompanyId, "Rival Yard", new Geofence(SiteLat, SiteLng, Radius));
            var client = Authed(factory, token);

            var response = await client.PostAsJsonAsync("/api/mobile/attendance/sign-in",
                new MobileSignInRequest(otherSiteId, null, SiteLat, SiteLng));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Sign_in_while_present_elsewhere_is_refused_and_names_the_other_site()
    {
        var (factory, token, companyId, _) = CreateOperativeHost();
        using (factory)
        {
            var siteA = SeedSite(factory, companyId, "Site Alpha", new Geofence(SiteLat, SiteLng, Radius));
            var siteB = SeedSite(factory, companyId, "Site Bravo", new Geofence(SiteLat, SiteLng, Radius));
            var client = Authed(factory, token);

            await client.PostAsJsonAsync("/api/mobile/attendance/sign-in", new MobileSignInRequest(siteA, null, SiteLat, SiteLng));

            var response = await client.PostAsJsonAsync("/api/mobile/attendance/sign-in",
                new MobileSignInRequest(siteB, null, SiteLat, SiteLng));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<SignInResult>();
            Assert.False(result!.SignedIn);
            Assert.Equal("Site Alpha", result.SignedInElsewhere); // SF-18
        }
    }

    [Fact]
    public async Task Sign_out_returns_the_duration_and_clears_the_current_state()
    {
        var (factory, token, companyId, _) = CreateOperativeHost();
        using (factory)
        {
            var siteId = SeedSite(factory, companyId, "Riverside Works", new Geofence(SiteLat, SiteLng, Radius));
            var client = Authed(factory, token);

            await client.PostAsJsonAsync("/api/mobile/attendance/sign-in", new MobileSignInRequest(siteId, null, SiteLat, SiteLng));

            var current = await client.GetFromJsonAsync<CurrentAttendanceDto>("/api/mobile/attendance/current");
            Assert.Equal(siteId, current!.SiteId);

            var signOut = await client.PostAsJsonAsync("/api/mobile/attendance/sign-out",
                new MobileSignOutRequest(siteId, SiteLat, SiteLng));

            Assert.Equal(HttpStatusCode.OK, signOut.StatusCode);
            var result = await signOut.Content.ReadFromJsonAsync<SignOutResult>();
            Assert.True(result!.SignedOut);
            Assert.NotNull(result.DurationHours);

            // No longer signed in anywhere — /current is 204 No Content (SF-18).
            var after = await client.GetAsync("/api/mobile/attendance/current");
            Assert.Equal(HttpStatusCode.NoContent, after.StatusCode);
        }
    }

    [Fact]
    public async Task Dashboard_reflects_the_live_signed_in_state_after_sign_in()
    {
        var (factory, token, companyId, _) = CreateOperativeHost();
        using (factory)
        {
            var siteId = SeedSite(factory, companyId, "Riverside Works", new Geofence(SiteLat, SiteLng, Radius));
            var client = Authed(factory, token);

            var before = await client.GetFromJsonAsync<OperativeDashboardDto>("/api/mobile/dashboard");
            Assert.False(before!.SignedIn);

            await client.PostAsJsonAsync("/api/mobile/attendance/sign-in", new MobileSignInRequest(siteId, null, SiteLat, SiteLng));

            var after = await client.GetFromJsonAsync<OperativeDashboardDto>("/api/mobile/dashboard");
            Assert.True(after!.SignedIn);
            Assert.Equal(siteId, after.CurrentSiteId);
            Assert.Equal("Riverside Works", after.CurrentSiteName);
        }
    }

    [Fact]
    public async Task Console_admin_cannot_reach_mobile_attendance_endpoints()
    {
        // Default test host authenticates as Administrator (TestBypass) — no Operative role, so RequireOperative rejects it.
        using var factory = new WebApplicationFactory<Program>();
        var response = await factory.CreateClient().GetAsync("/api/mobile/attendance/current");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Builds an HTTP client with the operative bearer token attached.</summary>
    private static HttpClient Authed(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Seeds a company and returns its id.</summary>
    private static Guid SeedCompany(WebApplicationFactory<Program> factory, string name)
    {
        using var scope = factory.Services.CreateScope();
        var company = new Company { Name = name, OrgType = OrgType.Subcontractor };
        scope.ServiceProvider.GetRequiredService<ICompanyRepository>().AddAsync(company).GetAwaiter().GetResult();
        return company.Id;
    }

    /// <summary>Seeds a site owned by the given company (with an optional boundary) and returns its id.</summary>
    private static Guid SeedSite(WebApplicationFactory<Program> factory, Guid companyId, string name, Geofence? boundary)
    {
        using var scope = factory.Services.CreateScope();
        var site = new Site { CompanyId = companyId, Name = name, Boundary = boundary };
        scope.ServiceProvider.GetRequiredService<ISiteRepository>().AddAsync(site).GetAwaiter().GetResult();
        return site.Id;
    }

    /// <summary>
    /// Builds a host with the real JWT scheme (TestBypass off), seeds a company + engaged operative into the
    /// in-memory store, and mints a genuine operative access token for them.
    /// </summary>
    private static (WebApplicationFactory<Program> Factory, string Token, Guid CompanyId, Guid PersonId) CreateOperativeHost()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Auth:TestBypass", "false"));

        Guid companyId;
        Guid personId;
        using (var scope = factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var company = new Company { Name = "Acme Subcontractor", OrgType = OrgType.Subcontractor };
            sp.GetRequiredService<ICompanyRepository>().AddAsync(company).GetAwaiter().GetResult();

            var person = new Person { PhoneNumber = PhoneNumber.Parse("+447700900700") };
            sp.GetRequiredService<IPersonRepository>().AddAsync(person).GetAwaiter().GetResult();

            sp.GetRequiredService<IEngagementRepository>()
                .AddAsync(new Engagement { CompanyId = company.Id, PersonId = person.Id, Name = "Alex Operative" })
                .GetAwaiter().GetResult();

            companyId = company.Id;
            personId = person.Id;
        }

        var token = new JwtOperativeTokenIssuer(new JwtOptions())
            .IssueAccessToken(personId, companyId, "Alex Operative", "device-1").Token;
        return (factory, token, companyId, personId);
    }
}
