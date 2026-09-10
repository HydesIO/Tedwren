using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Inductions;
using Tedwren.Application.Auth;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the shareable, tokenised induction link (UAT-018): an admin creates a link, an
/// operative opens it anonymously and starts the induction without a console account, and re-opening resumes the
/// same session. Also covers the token/passcode gate (403) and the R15 guard that a link can only target one of
/// the caller's own company templates.
/// </summary>
public sealed class InductionLinkApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Matches the API mock seed (DefaultInductionTemplate) — the main-contractor tenant (Meridian, MC-3).
    private static readonly Guid CompanyId = AdminUserSeeder.SeedCompanyId;
    private static readonly Guid TemplateId = Guid.Parse("66666666-6666-4666-8666-000000000010");

    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses an isolated host so induction/link state does not leak between tests.</summary>
    public InductionLinkApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact] // UAT-018 — create a link, view it by token, and start (then resume) the induction anonymously
    public async Task Link_ViewThenStart_RunsInductionAnonymously()
    {
        var client = CreateClient();

        // Admin creates a shareable link (with a passcode) for the seeded template.
        var link = await (await client.PostAsJsonAsync("/api/inductions/links",
            new CreateInductionLinkRequest(CompanyId, TemplateId, "Sam Taylor", RequirePasscode: true)))
            .Content.ReadFromJsonAsync<InductionLinkDto>();
        Assert.NotNull(link);
        Assert.False(string.IsNullOrWhiteSpace(link!.Token));
        Assert.False(string.IsNullOrWhiteSpace(link.Passcode));

        // The operative opens the link (token + passcode): they see who is inducting them and which induction.
        var view = await client.GetFromJsonAsync<InductionLinkViewDto>(
            $"/api/inductions/by-link/{link.Token}?passcode={Uri.EscapeDataString(link.Passcode!)}");
        Assert.NotNull(view);
        Assert.False(string.IsNullOrWhiteSpace(view!.CompanyName));
        Assert.False(string.IsNullOrWhiteSpace(view.TemplateName));
        Assert.Equal("Sam Taylor", view.Name);

        // Starting from the link yields a device session (steps + quiz, no answers, R5) — no console account needed.
        var session = await (await client.PostAsJsonAsync($"/api/inductions/by-link/{link.Token}/session",
            new StartInductionFromLinkRequest(link.Passcode, "Sam Taylor")))
            .Content.ReadFromJsonAsync<InductionSessionDto>();
        Assert.NotNull(session);
        Assert.Equal(view.TemplateName, session!.TemplateName);
        Assert.Equal("Sam Taylor", session.PersonName);
        Assert.NotEmpty(session.Steps);

        // Re-opening the link resumes the same session rather than starting a new one.
        var resumed = await (await client.PostAsJsonAsync($"/api/inductions/by-link/{link.Token}/session",
            new StartInductionFromLinkRequest(link.Passcode, "Sam Taylor")))
            .Content.ReadFromJsonAsync<InductionSessionDto>();
        Assert.Equal(session.Id, resumed!.Id);
    }

    [Fact] // UAT-018 — a link created without a passcode opens with none
    public async Task Link_WithoutPasscode_OpensAndStarts()
    {
        var client = CreateClient();

        var link = await (await client.PostAsJsonAsync("/api/inductions/links",
            new CreateInductionLinkRequest(CompanyId, TemplateId, null, RequirePasscode: false)))
            .Content.ReadFromJsonAsync<InductionLinkDto>();
        Assert.Null(link!.Passcode);

        var view = await client.GetAsync($"/api/inductions/by-link/{link.Token}");
        Assert.Equal(HttpStatusCode.OK, view.StatusCode);

        var session = await (await client.PostAsJsonAsync($"/api/inductions/by-link/{link.Token}/session",
            new StartInductionFromLinkRequest(null, "Jo Bloggs")))
            .Content.ReadFromJsonAsync<InductionSessionDto>();
        Assert.Equal("Jo Bloggs", session!.PersonName);
    }

    [Fact] // UAT-018/R9 — the wrong passcode is refused (403), never silently opened
    public async Task Link_WithWrongPasscode_Forbidden()
    {
        var client = CreateClient();
        var link = await (await client.PostAsJsonAsync("/api/inductions/links",
            new CreateInductionLinkRequest(CompanyId, TemplateId, null, RequirePasscode: true)))
            .Content.ReadFromJsonAsync<InductionLinkDto>();

        var view = await client.GetAsync($"/api/inductions/by-link/{link!.Token}?passcode=WRONGONE");
        Assert.Equal(HttpStatusCode.Forbidden, view.StatusCode);

        var start = await client.PostAsJsonAsync($"/api/inductions/by-link/{link.Token}/session",
            new StartInductionFromLinkRequest("WRONGONE", "Sam Taylor"));
        Assert.Equal(HttpStatusCode.Forbidden, start.StatusCode);
    }

    [Fact] // UAT-018 — an unknown token is refused (403)
    public async Task Link_UnknownToken_Forbidden()
    {
        var client = CreateClient();
        var response = await client.GetAsync($"/api/inductions/by-link/{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // UAT-018/R15 — a link can only target one of the caller's own company templates
    public async Task Link_ToAnotherCompanysTemplate_Rejected()
    {
        var client = CreateClient();

        // Seed a template owned by a different company.
        var otherCompany = Guid.NewGuid();
        var created = await client.PostAsJsonAsync("/api/inductions/templates",
            new CreateInductionTemplateRequest(otherCompany, "Other Co Induction", 100, 2));
        var otherTemplateId = (await created.Content.ReadFromJsonAsync<CreatedId>())!.Id;

        // Requesting a link for CompanyId but pointing at the other company's template is rejected (400).
        var bad = await client.PostAsJsonAsync("/api/inductions/links",
            new CreateInductionLinkRequest(CompanyId, otherTemplateId, null, RequirePasscode: false));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    /// <summary>Shape of the create-template response body.</summary>
    private sealed record CreatedId(Guid Id);
}
