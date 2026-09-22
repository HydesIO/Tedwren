using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Api.Auth;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Api.Tests;

/// <summary>
/// Verifies the operative (mobile) accreditation surface (<c>/api/mobile/cards</c>, Subcontractor Onboarding Stage 4 /
/// Gate 3): the type picker is available to an operative, a captured card lands in the register under the token's
/// PersonId (R15) needing manager confirmation (SF-6), the write is idempotent on the client id (R4/R16), and a
/// console token is rejected (RequireOperative).
/// </summary>
public sealed class MobileCardApiTests
{
    [Fact] // The accreditation-type picker is available to an operative and includes the seeded Gas Safe (spec §2).
    public async Task CardTypes_are_available_to_an_operative()
    {
        var (factory, token, _, _) = CreateOperativeHost();
        using (factory)
        {
            var client = Authed(factory, token);

            var types = await client.GetFromJsonAsync<List<QualificationTypeDto>>("/api/mobile/cards/types");

            Assert.NotNull(types);
            Assert.Contains(types!, t => t.Name == "Gas Safe");
        }
    }

    [Fact] // A captured card lands in the operative's register under the token's PersonId, needing confirmation (SF-6/R15).
    public async Task Card_is_recorded_for_the_operative_from_the_token()
    {
        var (factory, token, _, personId) = CreateOperativeHost();
        using (factory)
        {
            var client = Authed(factory, token);
            var typeId = (await client.GetFromJsonAsync<List<QualificationTypeDto>>("/api/mobile/cards/types"))![0].Id;
            var clientId = Guid.NewGuid();

            var response = await client.PostAsJsonAsync("/api/mobile/cards", new MobileCaptureCardRequest(
                clientId, typeId, "G1", "Alex Operative", null, DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1), "img-1", DateTimeOffset.UtcNow));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var cards = await scope.ServiceProvider.GetRequiredService<IQualificationCardRepository>().GetByPersonAsync(personId);
            var card = Assert.Single(cards);
            Assert.Equal(typeId, card.QualificationTypeId);
            Assert.True(card.NeedsReview);                 // never auto-accepted (SF-6)
            Assert.Equal(clientId, card.CaptureClientId);  // idempotency key persisted
        }
    }

    [Fact] // A retried submit with the same client id never duplicates the card (R4/R16).
    public async Task Card_is_idempotent_on_the_client_id()
    {
        var (factory, token, _, personId) = CreateOperativeHost();
        using (factory)
        {
            var client = Authed(factory, token);
            var typeId = (await client.GetFromJsonAsync<List<QualificationTypeDto>>("/api/mobile/cards/types"))![0].Id;
            var request = new MobileCaptureCardRequest(Guid.NewGuid(), typeId, "G1", null, null, null, null, DateTimeOffset.UtcNow);

            await client.PostAsJsonAsync("/api/mobile/cards", request);
            await client.PostAsJsonAsync("/api/mobile/cards", request);

            using var scope = factory.Services.CreateScope();
            var cards = await scope.ServiceProvider.GetRequiredService<IQualificationCardRepository>().GetByPersonAsync(personId);
            Assert.Single(cards);
        }
    }

    [Fact] // A console token cannot use the operative accreditation surface (RequireOperative).
    public async Task Card_is_rejected_for_a_console_token()
    {
        using var factory = new WebApplicationFactory<Program>();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/mobile/cards",
            new MobileCaptureCardRequest(Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, DateTimeOffset.UtcNow));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ---- helpers ----------------------------------------------------------------------------------------

    private static HttpClient Authed(WebApplicationFactory<Program> factory, string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
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

            var person = new Person { PhoneNumber = PhoneNumber.Parse("+447700900701") };
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
