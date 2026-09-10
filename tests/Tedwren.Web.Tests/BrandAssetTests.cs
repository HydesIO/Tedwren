using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tedwren.Web.Tests;

/// <summary>
/// Verifies the marketing site serves the same brand logo as the app: the SVG owned by
/// <c>Tedwren.Client</c> is copied into this project's wwwroot at build (single source), rendered in
/// both the header and footer, and served as an SVG static file.
/// </summary>
public sealed class BrandAssetTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string LogoPath = "/images/logo-icon.svg";
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Injects the in-process site host.</summary>
    /// <param name="factory">Test host for Tedwren.Web.</param>
    public BrandAssetTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>The shared logo is referenced in both the header and the footer (two occurrences).</summary>
    [Fact]
    public async Task Header_And_Footer_ReferenceSharedLogo()
    {
        var body = await _factory.CreateClient().GetStringAsync("/");

        var references = Regex.Matches(body, Regex.Escape("images/logo-icon.svg")).Count;
        Assert.True(references >= 2, $"Expected the logo in the header and footer, found {references} reference(s).");
    }

    /// <summary>The build-shared logo is served as an SVG (proves the copy target + static-file wiring).</summary>
    [Fact]
    public async Task Logo_IsServed_AsSvg()
    {
        var response = await _factory.CreateClient().GetAsync(LogoPath);
        response.EnsureSuccessStatusCode();

        Assert.Equal("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<svg", await response.Content.ReadAsStringAsync());
    }
}
