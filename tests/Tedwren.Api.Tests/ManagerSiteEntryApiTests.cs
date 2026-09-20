using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Api.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Api.Tests;

/// <summary>
/// Verifies the M7 authenticated manager site-entry surface (<c>/api/manager/*</c>): a manager reads the live
/// muster and runs an entry decision + override only for their own company's sites (R15); the override is
/// admitted and recorded (MC-11); a read-only Auditor cannot decide (RequireWrite, SF-23); and an operative
/// token cannot reach the console plane.
/// </summary>
public sealed class ManagerSiteEntryApiTests
{
    [Fact]
    public async Task Muster_for_an_own_site_is_returned()
    {
        var (factory, token, _, _, siteId) = CreateManagerHost();
        using (factory)
        {
            var response = await Authed(factory, token).GetAsync($"/api/manager/muster/{siteId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var muster = await response.Content.ReadFromJsonAsync<MusterDto>();
            Assert.Equal(siteId, muster!.SiteId);
        }
    }

    [Fact]
    public async Task Muster_for_another_companys_site_is_forbidden() // R15
    {
        var (factory, token, _, _, _) = CreateManagerHost();
        using (factory)
        {
            var otherCompany = SeedCompany(factory, "Rival Contractor");
            var otherSite = SeedSite(factory, otherCompany, "Rival Yard");

            var response = await Authed(factory, token).GetAsync($"/api/manager/muster/{otherSite}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Muster_is_forbidden_for_an_operative_token()
    {
        var (factory, _, companyId, personId, siteId) = CreateManagerHost();
        using (factory)
        {
            var opToken = new JwtOperativeTokenIssuer(new JwtOptions())
                .IssueAccessToken(personId, companyId, "Alex Operative", "device-1").Token;

            var response = await Authed(factory, opToken).GetAsync($"/api/manager/muster/{siteId}");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Muster_is_unauthorized_without_a_token()
    {
        var (factory, _, _, _, siteId) = CreateManagerHost();
        using (factory)
        {
            var response = await factory.CreateClient().GetAsync($"/api/manager/muster/{siteId}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task Decide_with_an_override_is_admitted_and_recorded() // MC-11
    {
        var (factory, token, _, personId, siteId) = CreateManagerHost();
        using (factory)
        {
            var response = await Authed(factory, token).PostAsJsonAsync("/api/manager/entry/decide",
                new ManagerDecideRequest(siteId, personId, null, "Escorted on site for a supervised task"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<EntryDecisionResultDto>();
            Assert.True(result!.Admitted);
            Assert.True(result.WasOverridden);
            Assert.Contains(result.Checks, c => c.Name == "Manager override");
        }
    }

    [Fact]
    public async Task Decide_at_another_companys_site_is_forbidden() // R15
    {
        var (factory, token, _, personId, _) = CreateManagerHost();
        using (factory)
        {
            var otherCompany = SeedCompany(factory, "Rival Contractor");
            var otherSite = SeedSite(factory, otherCompany, "Rival Yard");

            var response = await Authed(factory, token).PostAsJsonAsync("/api/manager/entry/decide",
                new ManagerDecideRequest(otherSite, personId, null, null));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Decide_is_forbidden_for_a_read_only_auditor() // RequireWrite / SF-23
    {
        var (factory, token, _, personId, siteId) = CreateManagerHost(AccessRole.Auditor);
        using (factory)
        {
            var response = await Authed(factory, token).PostAsJsonAsync("/api/manager/entry/decide",
                new ManagerDecideRequest(siteId, personId, null, "reason"));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Decide_is_forbidden_for_an_operative_token()
    {
        var (factory, _, companyId, personId, siteId) = CreateManagerHost();
        using (factory)
        {
            var opToken = new JwtOperativeTokenIssuer(new JwtOptions())
                .IssueAccessToken(personId, companyId, "Alex Operative", "device-1").Token;

            var response = await Authed(factory, opToken).PostAsJsonAsync("/api/manager/entry/decide",
                new ManagerDecideRequest(siteId, personId, null, "reason"));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    // ---- helpers ----------------------------------------------------------------------------------------

    private static HttpClient Authed(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static Guid SeedCompany(WebApplicationFactory<Program> factory, string name)
    {
        using var scope = factory.Services.CreateScope();
        var company = new Company { Name = name, OrgType = OrgType.MainContractor };
        scope.ServiceProvider.GetRequiredService<ICompanyRepository>().AddAsync(company).GetAwaiter().GetResult();
        return company.Id;
    }

    private static Guid SeedSite(WebApplicationFactory<Program> factory, Guid companyId, string name)
    {
        using var scope = factory.Services.CreateScope();
        var site = new Site { CompanyId = companyId, Name = name };
        scope.ServiceProvider.GetRequiredService<ISiteRepository>().AddAsync(site).GetAwaiter().GetResult();
        return site.Id;
    }

    /// <summary>
    /// Builds a real-JWT host (TestBypass off), seeds a company + engaged operative + a site the company owns, and
    /// mints a genuine console (manager) token for the given role.
    /// </summary>
    private static (WebApplicationFactory<Program> Factory, string Token, Guid CompanyId, Guid PersonId, Guid SiteId) CreateManagerHost(
        AccessRole role = AccessRole.SiteManager)
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Auth:TestBypass", "false"));

        Guid companyId;
        Guid personId;
        Guid siteId;
        using (var scope = factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var company = new Company { Name = "Acme Contractor", OrgType = OrgType.MainContractor };
            sp.GetRequiredService<ICompanyRepository>().AddAsync(company).GetAwaiter().GetResult();

            var person = new Person { PhoneNumber = PhoneNumber.Parse("+447700900700") };
            sp.GetRequiredService<IPersonRepository>().AddAsync(person).GetAwaiter().GetResult();
            sp.GetRequiredService<IEngagementRepository>()
                .AddAsync(new Engagement { CompanyId = company.Id, PersonId = person.Id, Name = "Alex Operative" })
                .GetAwaiter().GetResult();

            var site = new Site { CompanyId = company.Id, Name = "Riverside Works" };
            sp.GetRequiredService<ISiteRepository>().AddAsync(site).GetAwaiter().GetResult();

            companyId = company.Id;
            personId = person.Id;
            siteId = site.Id;
        }

        var user = new User { CompanyId = companyId, Name = "Morgan Manager", Email = "morgan@acme.test", Role = role };
        var token = new JwtTokenIssuer(new JwtOptions()).Issue(user).Token;
        return (factory, token, companyId, personId, siteId);
    }
}
