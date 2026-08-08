using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
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

    private sealed record CreatedResponse(Guid Id);
}
