using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Contracts.Trades;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for trade self-service onboarding (UAT-023): an authorised manager invites a trade,
/// which then anonymously (token+passcode) views the invitation and uploads its own documents. Covers the
/// token/passcode gate (403) and that inviting a trade creates its company record.
/// </summary>
public sealed class TradeOnboardingApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses an isolated host so invite state does not leak between tests.</summary>
    public TradeOnboardingApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact] // UAT-023 — invite a trade, then anonymously view the link and upload a document
    public async Task InviteTrade_ThenViewAndUploadAnonymously()
    {
        var client = CreateClient();
        var companyName = "Acme Scaffolding " + Guid.NewGuid().ToString("N")[..6];

        // Manager invites the trade (with a passcode). This also creates the trade company.
        var link = await (await client.PostAsJsonAsync("/api/trades/invites",
            new CreateTradeInviteRequest(companyName, "Subcontractor", "Scaffolding", "Sam Taylor", "sam@acme.test", RequirePasscode: true)))
            .Content.ReadFromJsonAsync<TradeInviteLinkDto>();
        Assert.NotNull(link);
        Assert.False(string.IsNullOrWhiteSpace(link!.Token));
        Assert.False(string.IsNullOrWhiteSpace(link.Passcode));

        // The trade company now exists in the organisation register.
        var companies = await client.GetFromJsonAsync<List<CompanySummary>>("/api/organisation/companies");
        Assert.Contains(companies!, c => c.Name == companyName);

        // The trade opens the link (token + passcode): sees who invited them and what's requested, nothing uploaded yet.
        var view = await client.GetFromJsonAsync<TradeInviteViewDto>(
            $"/api/trades/by-link/{link.Token}?passcode={Uri.EscapeDataString(link.Passcode!)}");
        Assert.NotNull(view);
        Assert.Equal(companyName, view!.CompanyName);
        Assert.Equal("Invited", view.Status);
        Assert.Empty(view.Documents);
        Assert.NotEmpty(view.RequestedDocumentTypes);

        // The trade uploads a document with a file (stored privately, R9).
        var fileB64 = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
        var after = await (await client.PostAsJsonAsync(
            $"/api/trades/by-link/{link.Token}/documents?passcode={Uri.EscapeDataString(link.Passcode!)}",
            new SubmitTradeDocumentRequest("Insurance", "Employer's Liability", null, null, fileB64, "application/pdf")))
            .Content.ReadFromJsonAsync<TradeInviteViewDto>();
        var doc = Assert.Single(after!.Documents);
        Assert.Equal("Insurance", doc.Type);
        Assert.True(doc.HasFile);
    }

    [Fact] // UAT-023/R9 — the wrong passcode is refused (403)
    public async Task TradeLink_WithWrongPasscode_Forbidden()
    {
        var client = CreateClient();
        var link = await (await client.PostAsJsonAsync("/api/trades/invites",
            new CreateTradeInviteRequest("Bad Passcode Trade " + Guid.NewGuid().ToString("N")[..6], null, null, null, null, RequirePasscode: true)))
            .Content.ReadFromJsonAsync<TradeInviteLinkDto>();

        var view = await client.GetAsync($"/api/trades/by-link/{link!.Token}?passcode=WRONGONE");
        Assert.Equal(HttpStatusCode.Forbidden, view.StatusCode);
    }

    [Fact] // UAT-023 — an unknown token is refused (403)
    public async Task TradeLink_UnknownToken_Forbidden()
    {
        var client = CreateClient();
        var response = await client.GetAsync($"/api/trades/by-link/{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
