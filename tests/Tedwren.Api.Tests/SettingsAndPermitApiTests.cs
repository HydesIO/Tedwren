using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Permits;
using Tedwren.Abstractions.Contracts.Settings;
using Tedwren.Application.Auth;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the per-company settings (<c>/api/settings</c>) and permits (<c>/api/permits</c>)
/// endpoints.
/// </summary>
public sealed class SettingsAndPermitApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public SettingsAndPermitApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Settings_UnsetCompany_ReturnsDefaults()
    {
        var client = _factory.CreateClient();

        var settings = await client.GetFromJsonAsync<GeneralSettingsDto>($"/api/settings/{Guid.NewGuid()}");

        Assert.NotNull(settings);
        Assert.Equal("Europe/London", settings!.TimeZone);
    }

    [Fact]
    public async Task Settings_Saved_AreReturnedOnGet()
    {
        var client = _factory.CreateClient();
        var companyId = Guid.NewGuid();
        var saved = new GeneralSettingsDto("Acme Scaffolding", "UTC", false, false, true, true);

        var put = await client.PutAsJsonAsync($"/api/settings/{companyId}", saved);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var read = await client.GetFromJsonAsync<GeneralSettingsDto>($"/api/settings/{companyId}");
        Assert.Equal(saved, read);
    }

    [Fact]
    public async Task Permit_Issued_AppearsInCompanyList()
    {
        var client = _factory.CreateClient();
        var companyId = Guid.NewGuid();
        // An in-force valid period (relative to today) so the time-bound status stays "Issued", not "Expired".
        var request = new CreatePermitRequest(
            companyId, "Hot Works", "Meridian Tower", "M. Adeyemi",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            "Welding to level 4", true, true, Issue: true);

        var create = await client.PostAsJsonAsync("/api/permits", request);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var permits = await client.GetFromJsonAsync<List<PermitDto>>($"/api/permits/company/{companyId}");
        var permit = Assert.Single(permits!);
        Assert.Equal("Hot Works", permit.PermitType);
        Assert.Equal("Issued", permit.Status);
        Assert.True(permit.HighRisk);
    }

    [Fact] // PRD §8.2
    public async Task Permit_ApproveThenClose_ReflectedInList_AndRejectsInvalidTransition()
    {
        var client = _factory.CreateClient();
        var companyId = AdminUserSeeder.SeedCompanyId; // the test-bypass caller's company (R15)
        var create = await client.PostAsJsonAsync("/api/permits", new CreatePermitRequest(
            companyId, "Confined Space", "Basement", "R. Okoro", null, null, null, true, true, Issue: true));
        var id = await create.Content.ReadFromJsonAsync<Guid>();

        var approve = await client.PostAsync($"/api/permits/{id}/approve", content: null);
        Assert.Equal(HttpStatusCode.OK, approve.StatusCode);
        var afterApprove = await client.GetFromJsonAsync<List<PermitDto>>($"/api/permits/company/{companyId}");
        Assert.Equal("Approved", afterApprove!.Single(p => p.Id == id).Status);

        var close = await client.PostAsJsonAsync($"/api/permits/{id}/close", new ClosePermitRequest("Work complete"));
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);
        var afterClose = await client.GetFromJsonAsync<List<PermitDto>>($"/api/permits/company/{companyId}");
        Assert.Equal("Closed", afterClose!.Single(p => p.Id == id).Status);

        // A closed permit cannot be approved again (409, not a silent success).
        var reapprove = await client.PostAsync($"/api/permits/{id}/approve", content: null);
        Assert.Equal(HttpStatusCode.Conflict, reapprove.StatusCode);
    }

    [Fact] // R15
    public async Task Permit_Approve_OtherCompany_NotFound()
    {
        var client = _factory.CreateClient();
        var otherCompany = Guid.NewGuid(); // not the caller's company
        var create = await client.PostAsJsonAsync("/api/permits", new CreatePermitRequest(
            otherCompany, "Hot Works", null, null, null, null, null, false, false, Issue: true));
        var id = await create.Content.ReadFromJsonAsync<Guid>();

        var approve = await client.PostAsync($"/api/permits/{id}/approve", content: null);
        Assert.Equal(HttpStatusCode.NotFound, approve.StatusCode);
    }
}
