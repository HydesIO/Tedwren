using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Services;

namespace Tedwren.Web.Tests;

/// <summary>
/// W2 integration tests: the shipped JSON content files load through DI at the real content root, and
/// content-sourced identity reaches the rendered footer. This exercises the whole path — file layout,
/// content-root resolution, DI registration — that the isolated unit tests deliberately bypass.
/// </summary>
public sealed class ContentIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Injects the in-process site host.</summary>
    /// <param name="factory">Test host for Tedwren.Web.</param>
    public ContentIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>The DI-registered provider loads the shipped content (products + plans present).</summary>
    [Fact]
    public void ShippedContent_LoadsThroughDi()
    {
        using var scope = _factory.Services.CreateScope();
        var content = scope.ServiceProvider.GetRequiredService<IContentProvider>();

        Assert.NotNull(content.FindProduct("Subcontractor"));
        Assert.NotNull(content.FindProduct("MainContractor"));
        Assert.NotNull(content.FindPricingPlan("WorkerPassport"));
        Assert.Equal("Tedwren", content.ResolveToken("Site:Brand"));
    }

    /// <summary>The footer's legal entity is served from the content layer, not a hardcoded view.</summary>
    [Fact]
    public async Task Footer_LegalEntity_ComesFromContent()
    {
        var client = _factory.CreateClient();

        var body = await client.GetStringAsync("/");

        Assert.Contains("Tedwren Ltd", body);
    }
}
