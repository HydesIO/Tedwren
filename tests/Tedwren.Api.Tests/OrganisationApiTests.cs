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

    [Fact] // SUB-4 — a posted document surfaces on the company detail
    public async Task AddCompanyDocument_IsReturnedOnCompanyDetail()
    {
        var client = _factory.CreateClient();
        var name = "DocCo " + Guid.NewGuid().ToString("N");

        var created = await client.PostAsJsonAsync("/api/organisation/companies",
            new CreateCompanyRequest(name, "Subcontractor", null, null, null, null, null, null));
        var companyId = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var companies = await client.GetFromJsonAsync<List<CompanySummary>>("/api/organisation/companies");
        var slug = companies!.Single(c => c.Name == name).Slug;

        var docResponse = await client.PostAsJsonAsync($"/api/organisation/companies/{companyId}/documents",
            new CreateCompanyDocumentRequest(companyId, "Public Liability Insurance", "Insurance", null, "PL-1"));
        Assert.Equal(HttpStatusCode.Created, docResponse.StatusCode);

        var detail = await client.GetFromJsonAsync<CompanyDetailDto>($"/api/organisation/companies/{slug}");
        Assert.Contains(detail!.Documents, d => d.Name == "Public Liability Insurance");
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

    [Fact] // SF-2 edit: an operative's engagement name + trade persist through PUT
    public async Task UpdateEngagement_PersistsNameAndTrade()
    {
        var client = _factory.CreateClient();
        // The workforce detail is tenant-scoped (R15), so operate on the caller's own company.
        var me = await client.GetFromJsonAsync<Tedwren.Abstractions.Contracts.Identity.CurrentUserDto>("/api/me");
        var companyId = me!.CompanyId!.Value;

        var add = await client.PostAsJsonAsync("/api/organisation/operatives",
            new AddOperativeRequest(companyId, "Rename Me " + Guid.NewGuid().ToString("N")[..6], "07700900801", "Labourer", null));
        var result = (await add.Content.ReadFromJsonAsync<AddOperativeResult>())!;
        Assert.True(result.Succeeded);

        var newName = "Renamed " + Guid.NewGuid().ToString("N")[..6];
        var put = await client.PutAsJsonAsync(
            $"/api/organisation/companies/{companyId}/operatives/{result.EngagementId}",
            new UpdateEngagementRequest(newName, "Electrician"));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var slug = newName.ToLowerInvariant().Replace(' ', '-');
        var detail = await client.GetFromJsonAsync<Tedwren.Abstractions.Contracts.Workforce.OperativeDetailDto>($"/api/workforce/{slug}");
        Assert.NotNull(detail);
        Assert.Equal(newName, detail!.Name);
        Assert.Equal("Electrician", detail.Trade);
    }

    [Fact] // update of an unknown engagement is a visible 404
    public async Task UpdateEngagement_Unknown_Returns404()
    {
        var client = _factory.CreateClient();
        var companies = await client.GetFromJsonAsync<List<CompanySummary>>("/api/organisation/companies");
        var companyId = companies![0].Id;

        var put = await client.PutAsJsonAsync(
            $"/api/organisation/companies/{companyId}/operatives/{Guid.NewGuid()}",
            new UpdateEngagementRequest("Ghost", null));
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    }

    /// <summary>Shape of a create response body.</summary>
    private sealed record CreatedResponse(Guid Id);
}
