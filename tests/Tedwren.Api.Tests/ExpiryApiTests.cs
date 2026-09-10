using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Organisation;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Abstractions.Contracts.Workforce;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the upcoming-expiries read endpoint (<c>/api/expiry/upcoming</c>). The dashboard
/// tile and list read this. Verifies the count is tenant-scoped (R15, UAT-003) and each row names the operative
/// so the list can show who — and link to them (UAT-005).
/// </summary>
public sealed class ExpiryApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public ExpiryApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact] // UAT-003/005 — upcoming expiries are tenant-scoped and name the operative who holds the card
    public async Task Upcoming_IsTenantScoped_AndNamesTheOperative()
    {
        var client = _factory.CreateClient();
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");
        var companyId = me!.CompanyId!.Value;
        var name = "Expiry Case " + Guid.NewGuid().ToString("N")[..6];

        await client.PostAsJsonAsync("/api/organisation/operatives",
            new AddOperativeRequest(companyId, name, "07700900977", "Roofer", null));
        var register = await client.GetFromJsonAsync<List<OperativeListItemDto>>("/api/workforce");
        var personId = register!.Single(o => o.Name == name).PersonId;

        var types = await client.GetFromJsonAsync<List<QualificationTypeDto>>("/api/qualifications/types");
        var cscs = types!.First(t => t.Name == "CSCS Card");
        var soon = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));

        // A card for the tenant's operative, expiring within the window.
        await client.PostAsJsonAsync("/api/qualifications/cards",
            new CaptureCardRequest(personId, cscs.Id, "CS-EXP", name, null, soon, NeedsReview: false));
        // A card for a person NOT engaged by this tenant — must never surface in the tenant's list (R15).
        var outsiderPerson = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/qualifications/cards",
            new CaptureCardRequest(outsiderPerson, cscs.Id, "CS-OUT", "Outsider", null, soon, NeedsReview: false));

        var upcoming = await client.GetFromJsonAsync<List<UpcomingExpiryDto>>("/api/expiry/upcoming?withinDays=30");

        var mine = Assert.Single(upcoming!, e => e.PersonId == personId);
        Assert.Equal(name, mine.PersonName);          // names the operative (was a bare dash on the dashboard)
        Assert.False(string.IsNullOrEmpty(mine.Slug)); // and can link to their profile
        Assert.DoesNotContain(upcoming!, e => e.PersonId == outsiderPerson);   // tenant-scoped (R15)
    }
}
