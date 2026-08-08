using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Audit;
using Tedwren.Abstractions.Contracts.Decisions;
using Tedwren.Abstractions.Contracts.Entitlements;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the console-foundation endpoints in the API's default (mock) mode: module
/// entitlements (Q2), the audit trail incl. CSV export (SF-20) and the decision record store (R10).
/// </summary>
public sealed class ConsoleFoundationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses an isolated host so mock state does not leak between tests.</summary>
    public ConsoleFoundationApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact] // Q2 — authoritative, fail-closed entitlement answer
    public async Task Entitlements_ReturnCatalogue_AndFailClosedForUnknownModule()
    {
        var client = CreateClient();
        var company = Guid.NewGuid();

        var modules = await client.GetFromJsonAsync<List<ModuleEntitlementDto>>($"/api/entitlements/{company}");
        Assert.NotEmpty(modules!);
        Assert.Contains(modules!, m => m.Key == "workforce" && m.Enabled);   // foundation module defaults on

        var unknown = await client.GetFromJsonAsync<bool>($"/api/entitlements/{company}/does-not-exist");
        Assert.False(unknown);
    }

    [Fact] // SF-20 — record, search, export
    public async Task Audit_Record_Search_Export()
    {
        var client = CreateClient();
        var company = Guid.NewGuid();
        var record = new RecordAuditRequest(company, "Priya Shah", "Approved timesheet", "Riverside Phase 2", "TS-77", "Time & Attendance");

        var post = await client.PostAsJsonAsync("/api/audit", record);
        Assert.Equal(HttpStatusCode.NoContent, post.StatusCode);

        var results = await client.GetFromJsonAsync<List<AuditEntryDto>>($"/api/audit?companyId={company}&text=riverside");
        var entry = Assert.Single(results!);
        Assert.Equal("Priya Shah", entry.Actor);

        var csv = await client.GetStringAsync($"/api/audit/export?companyId={company}");
        Assert.StartsWith("When,Actor,Action,Entity,Reference,Category", csv);
        Assert.Contains("Riverside Phase 2", csv);
    }

    [Fact] // R10 — decision recorded and reconstructable
    public async Task Decision_Record_ThenReadBack()
    {
        var client = CreateClient();
        var personId = Guid.NewGuid();
        var siteId = Guid.NewGuid();
        var request = new RecordDecisionRequest(personId, siteId, Admitted: false, new[]
        {
            new DecisionCheckDto("Registered", "Passed", null),
            new DecisionCheckDto("Induction valid", "Failed", "Expired"),
        });

        var id = await (await client.PostAsJsonAsync("/api/decisions", request)).Content.ReadFromJsonAsync<Guid>();
        Assert.NotEqual(Guid.Empty, id);

        var forPerson = await client.GetFromJsonAsync<List<SiteEntryDecisionDto>>($"/api/decisions/people/{personId}");
        var decision = Assert.Single(forPerson!);
        Assert.False(decision.Admitted);
        Assert.Equal(2, decision.Checks.Count);
        Assert.Contains(decision.Checks, c => c.Name == "Induction valid" && c.Outcome == "Failed");
    }
}
