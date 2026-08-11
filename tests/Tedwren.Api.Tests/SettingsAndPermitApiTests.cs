using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Permits;
using Tedwren.Abstractions.Contracts.Settings;
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
        var request = new CreatePermitRequest(
            companyId, "Hot Works", "Meridian Tower", "M. Adeyemi",
            new DateOnly(2026, 8, 12), new DateOnly(2026, 8, 14), "Welding to level 4", true, true, Issue: true);

        var create = await client.PostAsJsonAsync("/api/permits", request);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var permits = await client.GetFromJsonAsync<List<PermitDto>>($"/api/permits/company/{companyId}");
        var permit = Assert.Single(permits!);
        Assert.Equal("Hot Works", permit.PermitType);
        Assert.Equal("Issued", permit.Status);
        Assert.True(permit.HighRisk);
    }
}
