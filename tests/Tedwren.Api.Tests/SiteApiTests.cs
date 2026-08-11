using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Sites;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the sites endpoints (SF-6/SF-14/SF-25/SF-26) in the API's default (mock) mode —
/// no database required, so these run in CI and locally.
/// </summary>
public sealed class SiteApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public SiteApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact] // SF-26: the seed includes a dispersed no-compound scheme
    public async Task GetSites_IncludesDispersedScheme()
    {
        var client = _factory.CreateClient();

        var sites = await client.GetFromJsonAsync<List<SiteSummary>>("/api/sites");

        Assert.NotNull(sites);
        var dispersed = Assert.Single(sites!, s => s.IsDispersed);
        Assert.True(dispersed.PropertyCount >= 2);
    }

    [Fact] // SF-26 detail
    public async Task GetDispersedScheme_ListsPropertiesAndNoCompound()
    {
        var client = _factory.CreateClient();
        var sites = await client.GetFromJsonAsync<List<SiteSummary>>("/api/sites");
        var slug = sites!.First(s => s.IsDispersed).Slug;

        var detail = await client.GetFromJsonAsync<SiteDetailDto>($"/api/sites/{slug}");

        Assert.NotNull(detail);
        Assert.False(detail!.HasCompound);          // SF-25: no fixed point of presence
        Assert.NotEmpty(detail.Properties);         // SF-26: geofenced properties
    }

    [Fact] // SF-6 record + SF-26 add property
    public async Task CreateSite_ThenAddProperty()
    {
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/sites", new CreateSiteRequest(
            Guid.NewGuid(), "New Retrofit Scheme", "Council", "Leeds", null, HasCompound: false, IsDispersed: true, Boundary: null));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var propertyResponse = await client.PostAsJsonAsync($"/api/sites/{created!.Id}/properties",
            new { Address = "5 Elm Road", Units = 1, Boundary = new GeofenceDto(53.8008, -1.5491, 50) });

        Assert.Equal(HttpStatusCode.Created, propertyResponse.StatusCode);
    }

    [Fact] // R15/MC-21: a site owned by the caller's tenant is listed and resolvable by slug
    public async Task TenantSite_IsListed_AndResolvableBySlug()
    {
        var client = _factory.CreateClient();
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");
        var name = "Tenant Site " + Guid.NewGuid().ToString("N")[..6];

        var created = await client.PostAsJsonAsync("/api/sites", new CreateSiteRequest(
            me!.CompanyId!.Value, name, "Client", "London", null, HasCompound: true, IsDispersed: false, Boundary: null));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var sites = await client.GetFromJsonAsync<List<SiteSummary>>("/api/sites");
        var row = Assert.Single(sites!, s => s.Name == name);

        var detail = await client.GetFromJsonAsync<SiteDetailDto>($"/api/sites/{row.Slug}");
        Assert.NotNull(detail);
        Assert.Equal(name, detail!.Name);
    }

    [Fact] // R15/MC-21: a site owned by another company is neither listed nor resolvable (visible 404, no leak)
    public async Task ForeignTenantSite_IsHidden_AndReturns404()
    {
        var client = _factory.CreateClient();
        var name = "Foreign Site " + Guid.NewGuid().ToString("N")[..6];
        var otherCompanyId = Guid.NewGuid();   // not the signed-in caller's tenant

        var created = await client.PostAsJsonAsync("/api/sites", new CreateSiteRequest(
            otherCompanyId, name, "Client", "Leeds", null, HasCompound: true, IsDispersed: false, Boundary: null));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var sites = await client.GetFromJsonAsync<List<SiteSummary>>("/api/sites");
        Assert.DoesNotContain(sites!, s => s.Name == name);

        var slug = name.ToLowerInvariant().Replace(' ', '-');
        var detail = await client.GetAsync($"/api/sites/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    [Fact] // SF-6 edit: a tenant site's details persist through PUT and are reflected on re-read
    public async Task UpdateSite_PersistsChanges()
    {
        var client = _factory.CreateClient();
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");
        var original = "Edit Me " + Guid.NewGuid().ToString("N")[..6];

        var created = await client.PostAsJsonAsync("/api/sites", new CreateSiteRequest(
            me!.CompanyId!.Value, original, "Old Client", "London", "1 Old St", HasCompound: true, IsDispersed: false, Boundary: null));
        var id = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var renamed = "Edited " + Guid.NewGuid().ToString("N")[..6];
        var put = await client.PutAsJsonAsync($"/api/sites/{id}", new UpdateSiteRequest(
            renamed, "New Client", "Leeds", "2 New St", HasCompound: false, Boundary: null));
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var slug = renamed.ToLowerInvariant().Replace(' ', '-');
        var detail = await client.GetFromJsonAsync<SiteDetailDto>($"/api/sites/{slug}");
        Assert.NotNull(detail);
        Assert.Equal(renamed, detail!.Name);
        Assert.Equal("New Client", detail.Client);
        Assert.False(detail.HasCompound);
    }

    [Fact] // R15/MC-21: updating a site outside the caller's tenant is refused (404)
    public async Task UpdateSite_ForeignTenant_Returns404()
    {
        var client = _factory.CreateClient();
        var created = await client.PostAsJsonAsync("/api/sites", new CreateSiteRequest(
            Guid.NewGuid(), "Foreign Edit " + Guid.NewGuid().ToString("N")[..6], null, null, null, HasCompound: true, IsDispersed: false, Boundary: null));
        var id = (await created.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var put = await client.PutAsJsonAsync($"/api/sites/{id}", new UpdateSiteRequest(
            "Nope", null, null, null, HasCompound: true, Boundary: null));
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    }

    private sealed record CreatedResponse(Guid Id);
}
