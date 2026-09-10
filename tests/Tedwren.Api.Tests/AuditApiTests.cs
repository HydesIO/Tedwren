using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Audit;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Organisation;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the audit trail (SF-20). The trail was empty because no business operation ever
/// recorded to it (UAT-022); these verify that mutating operations now write searchable audit entries.
/// </summary>
public sealed class AuditApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public AuditApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact] // UAT-022 (SF-20) — adding an operative records a searchable audit entry attributed to the actor
    public async Task AddingAnOperative_RecordsASearchableAuditEntry()
    {
        var client = _factory.CreateClient();
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");
        var companyId = me!.CompanyId!.Value;
        var name = "Audit Case " + Guid.NewGuid().ToString("N")[..6];

        var add = await client.PostAsJsonAsync("/api/organisation/operatives",
            new AddOperativeRequest(companyId, name, "07700900733", "Steel Fixer", null));
        Assert.True(add.IsSuccessStatusCode);

        // The operative's name now resolves an audit entry (the trail was previously always empty).
        var entries = await client.GetFromJsonAsync<List<AuditEntryDto>>($"/api/audit?text={Uri.EscapeDataString(name)}");
        var entry = Assert.Single(entries!, e => e.Entity == name);
        Assert.Equal("Operative added", entry.Action);
        Assert.Equal("Workforce", entry.Category);
        Assert.Equal(me.Name, entry.Actor);   // attributed to the signed-in user, not "System"
    }

    [Fact] // a search that matches nothing returns an empty list, not an error
    public async Task Search_WithNoMatches_ReturnsEmpty()
    {
        var client = _factory.CreateClient();

        var entries = await client.GetFromJsonAsync<List<AuditEntryDto>>(
            $"/api/audit?text=no-such-entity-{Guid.NewGuid():N}");

        Assert.NotNull(entries);
        Assert.Empty(entries!);
    }
}
