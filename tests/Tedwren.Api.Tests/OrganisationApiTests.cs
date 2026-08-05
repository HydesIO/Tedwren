using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Organisation;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the organisation endpoints, driven through <see cref="WebApplicationFactory{TEntryPoint}"/>
/// in the API's default (mock) mode — no database required, so these run in CI and locally.
/// </summary>
public sealed class OrganisationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public OrganisationApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task GetCompanies_ReturnsData()
    {
        var client = _factory.CreateClient();

        var companies = await client.GetFromJsonAsync<List<CompanySummary>>("/api/organisation/companies");

        Assert.NotNull(companies);
        Assert.NotEmpty(companies!);
    }

    [Fact]
    public async Task CreateCompany_ReturnsCreated()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/organisation/companies",
            new CreateCompanyRequest("Testburgh Ltd", "Subcontractor", "Roofing", null, null, null, null, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddOperative_DuplicateInSameCompany_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var companies = await client.GetFromJsonAsync<List<CompanySummary>>("/api/organisation/companies");
        var companyId = companies![0].Id;

        var first = await client.PostAsJsonAsync("/api/organisation/operatives",
            new AddOperativeRequest(companyId, "Test Person", "07700900555", null, null));
        var duplicate = await client.PostAsJsonAsync("/api/organisation/operatives",
            new AddOperativeRequest(companyId, "Test Person", "+447700900555", null, null));

        Assert.True(first.IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }
}
