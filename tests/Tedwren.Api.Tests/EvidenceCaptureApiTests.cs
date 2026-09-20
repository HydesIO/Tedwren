using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Evidence;
using Tedwren.Api.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Api.Tests;

/// <summary>
/// Verifies the M7 manager evidence-review surface (<c>/api/evidence-captures</c>): a manager reads only their own
/// company's field-evidence captures (R15) with the capturer's engagement name, and neither an operative token nor
/// an anonymous request can reach it.
/// </summary>
public sealed class EvidenceCaptureApiTests
{
    [Fact]
    public async Task List_returns_the_companys_captures_with_the_capturer_name()
    {
        var (factory, token, _, _, captureId) = CreateManagerHost();
        using (factory)
        {
            var response = await Authed(factory, token).GetAsync("/api/evidence-captures");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var list = await response.Content.ReadFromJsonAsync<List<EvidenceCaptureDto>>();
            var dto = Assert.Single(list!);
            Assert.Equal(captureId, dto.Id);
            Assert.Equal("Alex Operative", dto.PersonName);
        }
    }

    [Fact]
    public async Task Get_by_id_returns_the_capture()
    {
        var (factory, token, _, _, captureId) = CreateManagerHost();
        using (factory)
        {
            var response = await Authed(factory, token).GetAsync($"/api/evidence-captures/{captureId}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<EvidenceCaptureDto>();
            Assert.Equal(captureId, dto!.Id);
        }
    }

    [Fact]
    public async Task Another_companys_capture_is_not_found() // R15
    {
        var (factory, token, _, _, _) = CreateManagerHost();
        using (factory)
        {
            // A capture owned by a different company must read as not found for this manager.
            var otherCapture = Guid.NewGuid();
            using (var scope = factory.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<IEvidenceItemRepository>()
                    .AddAsync(new EvidenceItem { Id = otherCapture, CompanyId = Guid.NewGuid(), PersonId = Guid.NewGuid() });
            }

            var response = await Authed(factory, token).GetAsync($"/api/evidence-captures/{otherCapture}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task List_is_forbidden_for_an_operative_token()
    {
        var (factory, _, companyId, personId, _) = CreateManagerHost();
        using (factory)
        {
            var opToken = new JwtOperativeTokenIssuer(new JwtOptions())
                .IssueAccessToken(personId, companyId, "Alex Operative", "device-1").Token;

            var response = await Authed(factory, opToken).GetAsync("/api/evidence-captures");
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task List_is_unauthorized_without_a_token()
    {
        var (factory, _, _, _, _) = CreateManagerHost();
        using (factory)
        {
            var response = await factory.CreateClient().GetAsync("/api/evidence-captures");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    // ---- helpers ----------------------------------------------------------------------------------------

    private static HttpClient Authed(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Builds a real-JWT host (TestBypass off), seeds a company + engaged operative + one evidence capture, and
    /// mints a genuine console (manager) token. Returns the seeded capture id for assertions.
    /// </summary>
    private static (WebApplicationFactory<Program> Factory, string Token, Guid CompanyId, Guid PersonId, Guid CaptureId) CreateManagerHost()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("Auth:TestBypass", "false"));

        Guid companyId;
        Guid personId;
        Guid captureId;
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

            var capture = new EvidenceItem
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                PersonId = person.Id,
                Note = "Cracked slab",
                Latitude = 51.5,
                Longitude = -0.1,
                PhotoReference = "img-ref",
            };
            sp.GetRequiredService<IEvidenceItemRepository>().AddAsync(capture).GetAwaiter().GetResult();

            companyId = company.Id;
            personId = person.Id;
            captureId = capture.Id;
        }

        var user = new User { CompanyId = companyId, Name = "Morgan Manager", Email = "morgan@acme.test", Role = AccessRole.SiteManager };
        var token = new JwtTokenIssuer(new JwtOptions()).Issue(user).Token;
        return (factory, token, companyId, personId, captureId);
    }
}
