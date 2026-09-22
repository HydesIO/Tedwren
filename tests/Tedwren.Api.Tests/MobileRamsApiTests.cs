using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Rams;
using Tedwren.Api.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Api.Tests;

/// <summary>
/// Verifies the operative (mobile) RAMS surface (<c>/api/mobile/rams</c>, Subcontractor Onboarding Gate 5): the live
/// approved RAMS is served to the operative, a signature is recorded under the token's PersonId (R15), a stale
/// submission id is a 409, and a console token is rejected (RequireOperative).
/// </summary>
public sealed class MobileRamsApiTests
{
    [Fact] // The operative's current live approved RAMS is served for reading + signing.
    public async Task Live_is_served_to_an_operative()
    {
        var (factory, token, companyId, _) = CreateOperativeHost();
        using (factory)
        {
            SeedApprovedLiveRams(factory, companyId, out _, out _);
            var client = Authed(factory, token);

            var live = await client.GetFromJsonAsync<LiveRamsDto>("/api/mobile/rams/live");

            Assert.NotNull(live);
            Assert.Equal("Groundworks RAMS", live!.Title);
        }
    }

    [Fact] // Signing records an acknowledgement under the token's PersonId (R15), never the body.
    public async Task Sign_records_the_acknowledgement_for_the_operative_from_the_token()
    {
        var (factory, token, companyId, personId) = CreateOperativeHost();
        using (factory)
        {
            SeedApprovedLiveRams(factory, companyId, out var liveId, out var familyId);
            var client = Authed(factory, token);

            var response = await client.PostAsJsonAsync("/api/mobile/rams/sign", new SignRamsRequest(liveId, "Alex Operative"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var scope = factory.Services.CreateScope();
            var ack = await scope.ServiceProvider.GetRequiredService<IRamsAcknowledgementRepository>()
                .GetLatestForPersonAsync(companyId, personId, familyId);
            Assert.NotNull(ack);
            Assert.Equal(1, ack!.Version);
        }
    }

    [Fact] // A submission id that is not the current live approved version is a 409 (the app re-reads /live).
    public async Task Sign_is_conflict_for_an_unknown_submission()
    {
        var (factory, token, companyId, _) = CreateOperativeHost();
        using (factory)
        {
            SeedApprovedLiveRams(factory, companyId, out _, out _);
            var client = Authed(factory, token);

            var response = await client.PostAsJsonAsync("/api/mobile/rams/sign", new SignRamsRequest(Guid.NewGuid(), "Alex Operative"));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }
    }

    [Fact] // No live approved RAMS applies to the operative → 204 (nothing to sign).
    public async Task Live_is_no_content_when_none_applies()
    {
        var (factory, token, _, _) = CreateOperativeHost();
        using (factory)
        {
            var client = Authed(factory, token);

            var response = await client.GetAsync("/api/mobile/rams/live");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    [Fact] // A console token cannot use the operative RAMS surface (RequireOperative).
    public async Task Sign_is_rejected_for_a_console_token()
    {
        using var factory = new WebApplicationFactory<Program>();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/mobile/rams/sign", new SignRamsRequest(Guid.NewGuid(), "Alex Operative"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- helpers ----------------------------------------------------------------------------------------

    private static HttpClient Authed(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Seeds an approved live RAMS for the company + a subcontractor config pointing at its family (the operative's company is the sub).</summary>
    private static void SeedApprovedLiveRams(WebApplicationFactory<Program> factory, Guid companyId, out Guid liveId, out Guid familyId)
    {
        familyId = Guid.NewGuid();
        var live = new RamsSubmission
        {
            CompanyId = companyId, FamilyId = familyId, Version = 1, Reference = "RAMS-1",
            ContractorName = "Groundworks Co", Title = "Groundworks RAMS", Status = RamsStatus.Approved, IsLive = true,
        };
        liveId = live.Id;

        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        sp.GetRequiredService<IRamsRepository>().AddAsync(live).GetAwaiter().GetResult();
        sp.GetRequiredService<ISubcontractorOnboardingConfigRepository>().AddAsync(new SubcontractorOnboardingConfig
        {
            Id = Guid.NewGuid(), InviterCompanyId = companyId, SubcontractorCompanyId = companyId,
            RamsFamilyId = familyId, CreatedUtc = DateTimeOffset.UtcNow,
        }).GetAwaiter().GetResult();
    }

    /// <summary>Real-JWT host (TestBypass off) with a seeded company + engaged operative and a genuine operative token.</summary>
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

            var person = new Person { PhoneNumber = PhoneNumber.Parse("+447700900702") };
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
