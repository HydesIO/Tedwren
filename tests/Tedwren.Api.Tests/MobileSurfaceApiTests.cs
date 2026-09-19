using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Workforce;
using Tedwren.Api.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Api.Tests;

/// <summary>
/// Verifies the M3 operative read surface and the console/operative plane separation: an operative token reads
/// only its own data via <c>/api/mobile/*</c>, cannot reach console endpoints, and a console token cannot reach
/// the mobile endpoints.
/// </summary>
public sealed class MobileSurfaceApiTests
{
    [Fact]
    public async Task Console_admin_cannot_reach_mobile_endpoints()
    {
        // Default test host authenticates every request as an Administrator (TestBypass); it lacks the Operative
        // role + device claim, so RequireOperative rejects it.
        using var factory = new WebApplicationFactory<Program>();
        var response = await factory.CreateClient().GetAsync("/api/mobile/me");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Operative_can_read_own_profile_hours_dashboard_and_sites()
    {
        var (factory, token, _, personId) = CreateOperativeHost();
        using (factory)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var me = await client.GetAsync("/api/mobile/me");
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);
            var detail = await me.Content.ReadFromJsonAsync<OperativeDetailDto>();
            Assert.Equal(personId, detail!.PersonId);

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/mobile/my-hours")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/mobile/sites")).StatusCode);

            var dashboard = await client.GetAsync("/api/mobile/dashboard");
            Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
            var dto = await dashboard.Content.ReadFromJsonAsync<OperativeDashboardDto>();
            Assert.Equal("Alex Operative", dto!.Name);
        }
    }

    [Fact]
    public async Task Operative_token_cannot_reach_console_endpoints()
    {
        var (factory, token, _, _) = CreateOperativeHost();
        using (factory)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // The console register endpoint only relies on the fallback policy, now console-role-scoped (M3).
            var response = await client.GetAsync("/api/workforce");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
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
