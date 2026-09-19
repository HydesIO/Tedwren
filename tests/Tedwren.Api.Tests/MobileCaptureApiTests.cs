using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Safety;
using Tedwren.Abstractions.Services;
using Tedwren.Api.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Api.Tests;

/// <summary>
/// Verifies the M5 operative capture surface: the multipart upload sink (<c>/api/mobile/uploads</c>), the ungated
/// generic evidence write (<c>/api/mobile/evidence</c>) and the hse-gated hazard write (<c>/api/mobile/hazards</c>).
/// Confirms token-scoped identity (R15), client-id idempotency (R4/R16), the R9 image boundary (operatives can't
/// read the image route), the module gate (Q2) and that hazards converge with the console register.
/// </summary>
public sealed class MobileCaptureApiTests
{
    // ---- uploads -----------------------------------------------------------------------------------------

    [Fact]
    public async Task Upload_stores_the_image_and_returns_a_reference()
    {
        var (factory, token, _, _) = CreateOperativeHost();
        using (factory)
        {
            var client = Authed(factory, token);

            var response = await client.PostAsync("/api/mobile/uploads", ImagePart(new byte[] { 1, 2, 3, 4 }, "image/png"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<MobileUploadResultDto>();
            Assert.False(string.IsNullOrWhiteSpace(result!.Reference));

            using var scope = factory.Services.CreateScope();
            var stored = await scope.ServiceProvider.GetRequiredService<IImageStore>().GetAsync(result.Reference);
            Assert.NotNull(stored);
            Assert.Equal(4, stored!.Bytes.Length);
        }
    }

    [Fact]
    public async Task Upload_rejects_a_disallowed_content_type()
    {
        var (factory, token, _, _) = CreateOperativeHost();
        using (factory)
        {
            var client = Authed(factory, token);
            var response = await client.PostAsync("/api/mobile/uploads", ImagePart(new byte[] { 1, 2, 3, 4 }, "application/pdf"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [Fact]
    public async Task Upload_is_rejected_for_a_console_token()
    {
        using var factory = new WebApplicationFactory<Program>();
        var response = await factory.CreateClient().PostAsync("/api/mobile/uploads", ImagePart(new byte[] { 1, 2, 3, 4 }, "image/png"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Operative_cannot_read_the_console_image_route() // R9
    {
        var (factory, token, _, _) = CreateOperativeHost();
        using (factory)
        {
            var client = Authed(factory, token);
            var reference = (await (await client.PostAsync("/api/mobile/uploads", ImagePart(new byte[] { 1, 2, 3, 4 }, "image/png")))
                .Content.ReadFromJsonAsync<MobileUploadResultDto>())!.Reference;

            var read = await client.GetAsync($"/api/images/{reference}");
            Assert.Equal(HttpStatusCode.Forbidden, read.StatusCode);
        }
    }

    // ---- generic evidence (ungated) ---------------------------------------------------------------------

    [Fact]
    public async Task Evidence_is_recorded_for_any_operative_without_a_module()
    {
        var (factory, token, companyId, personId) = CreateOperativeHost();
        using (factory)
        {
            var client = Authed(factory, token);
            var clientId = Guid.NewGuid();

            var response = await client.PostAsJsonAsync("/api/mobile/evidence",
                new MobileReportEvidenceRequest(clientId, "Cracked slab", 51.5, -0.1, null, DateTimeOffset.UtcNow));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<EvidenceItemDto>();
            Assert.Equal(clientId, dto!.Id);

            using var scope = factory.Services.CreateScope();
            var mine = await scope.ServiceProvider.GetRequiredService<IEvidenceItemRepository>().GetByPersonAsync(personId);
            Assert.Single(mine);
            Assert.Equal(companyId, mine[0].CompanyId);
        }
    }

    [Fact]
    public async Task Evidence_is_idempotent_on_the_client_id()
    {
        var (factory, token, companyId, _) = CreateOperativeHost();
        using (factory)
        {
            var client = Authed(factory, token);
            var request = new MobileReportEvidenceRequest(Guid.NewGuid(), "Cracked slab", null, null, null, DateTimeOffset.UtcNow);

            await client.PostAsJsonAsync("/api/mobile/evidence", request);
            await client.PostAsJsonAsync("/api/mobile/evidence", request);

            using var scope = factory.Services.CreateScope();
            var all = await scope.ServiceProvider.GetRequiredService<IEvidenceItemRepository>().GetByCompanyAsync(companyId);
            Assert.Single(all);
        }
    }

    [Fact]
    public async Task Evidence_is_rejected_for_a_console_token()
    {
        using var factory = new WebApplicationFactory<Program>();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/mobile/evidence",
            new MobileReportEvidenceRequest(Guid.NewGuid(), "x", null, null, null, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- hazard (hse-gated) -----------------------------------------------------------------------------

    [Fact]
    public async Task Hazard_is_recorded_when_hse_is_enabled_and_converges_with_the_console_register()
    {
        var (factory, token, companyId, _) = CreateOperativeHost();
        using (factory)
        {
            EnableHse(factory, companyId);
            var client = Authed(factory, token);
            var clientId = Guid.NewGuid();

            var response = await client.PostAsJsonAsync("/api/mobile/hazards", Hazard(clientId));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var dto = await response.Content.ReadFromJsonAsync<HazardReportDto>();
            Assert.Equal(clientId, dto!.Id);
            Assert.Equal("Alex Operative", dto.ReportedBy);

            using var scope = factory.Services.CreateScope();
            var register = await scope.ServiceProvider.GetRequiredService<IHazardReportService>().ListAsync(companyId);
            Assert.Contains(register, h => h.Id == clientId);
        }
    }

    [Fact]
    public async Task Hazard_is_idempotent_on_the_client_id()
    {
        var (factory, token, companyId, _) = CreateOperativeHost();
        using (factory)
        {
            EnableHse(factory, companyId);
            var client = Authed(factory, token);
            var request = Hazard(Guid.NewGuid());

            await client.PostAsJsonAsync("/api/mobile/hazards", request);
            await client.PostAsJsonAsync("/api/mobile/hazards", request);

            using var scope = factory.Services.CreateScope();
            var register = await scope.ServiceProvider.GetRequiredService<IHazardReportService>().ListAsync(companyId);
            Assert.Single(register);
        }
    }

    [Fact]
    public async Task Hazard_is_forbidden_without_the_hse_module() // Q2 fail-closed
    {
        var (factory, token, _, _) = CreateOperativeHost();
        using (factory)
        {
            var client = Authed(factory, token); // hse not enabled
            var response = await client.PostAsJsonAsync("/api/mobile/hazards", Hazard(Guid.NewGuid()));
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Hazard_is_rejected_for_a_console_token()
    {
        using var factory = new WebApplicationFactory<Program>();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/mobile/hazards", Hazard(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_can_report_hazards_reflects_the_hse_module()
    {
        var (factory, token, companyId, _) = CreateOperativeHost();
        using (factory)
        {
            var client = Authed(factory, token);

            var before = await client.GetFromJsonAsync<OperativeDashboardDto>("/api/mobile/dashboard");
            Assert.False(before!.CanReportHazards);

            EnableHse(factory, companyId);
            var after = await client.GetFromJsonAsync<OperativeDashboardDto>("/api/mobile/dashboard");
            Assert.True(after!.CanReportHazards);
        }
    }

    // ---- helpers ----------------------------------------------------------------------------------------

    private static MobileReportHazardRequest Hazard(Guid clientId) =>
        new(clientId, "NearMiss", "Scaffold tag missing", "Level 3", 51.5, -0.1, null, "High", null, DateTimeOffset.UtcNow);

    private static MultipartFormDataContent ImagePart(byte[] bytes, string contentType)
    {
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent { { part, "file", "evidence.png" } };
    }

    private static HttpClient Authed(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Enables the hse module for the company in-scope (an operative token cannot call the admin entitlement endpoint).</summary>
    private static void EnableHse(WebApplicationFactory<Program> factory, Guid companyId)
    {
        using var scope = factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IEntitlementService>()
            .SetEnabledAsync(companyId, "hse", true).GetAwaiter().GetResult();
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
