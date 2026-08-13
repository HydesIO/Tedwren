using Tedwren.Web.Content;

namespace Tedwren.Web.Tests;

/// <summary>
/// W2 unit tests for the JSON content provider (Web Plan §3). Each test writes purpose-built JSON to
/// an isolated temp directory, so nothing depends on the shipped content files. Covers loading, lookup
/// by key, the naming/price token indirection, and fail-fast behaviour on bad input.
/// </summary>
public sealed class JsonContentProviderTests : IDisposable
{
    private readonly string _dir;

    /// <summary>Creates an isolated temp content directory seeded with valid content.</summary>
    public JsonContentProviderTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "tedwren-content-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        SeedValidContent();
    }

    /// <summary>Removes the temp directory after each test.</summary>
    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    /// <summary>The provider loads every content collection and the site identity.</summary>
    [Fact]
    public void Loads_AllContent()
    {
        var provider = new JsonContentProvider(_dir);

        Assert.Equal("Tedwren", provider.Site.BrandName);
        Assert.Equal("Tedwren Ltd", provider.Site.LegalEntity);
        Assert.Equal(2, provider.Products.Count);
        Assert.Single(provider.PricingPlans);
        Assert.Single(provider.TrustPoints);
        Assert.Single(provider.Faqs);
        Assert.Empty(provider.Testimonials);
    }

    /// <summary>Lookup by key resolves products and plans, and returns null for unknown keys.</summary>
    [Fact]
    public void Find_ByKey_ResolvesOrReturnsNull()
    {
        var provider = new JsonContentProvider(_dir);

        Assert.Equal("Tedwren for Subcontractors", provider.FindProduct("Subcontractor")!.DisplayName);
        Assert.NotNull(provider.FindPricingPlan("WorkerPassport"));
        Assert.Null(provider.FindProduct("Nonexistent"));
        Assert.Null(provider.FindPricingPlan("Nonexistent"));
    }

    /// <summary>Name/slug tokens resolve to the product's content, never an inline literal (Plan §2).</summary>
    [Theory]
    [InlineData("Site:Brand", "Tedwren")]
    [InlineData("Site:LegalEntity", "Tedwren Ltd")]
    [InlineData("Products:Subcontractor:Name", "Tedwren for Subcontractors")]
    [InlineData("Products:Subcontractor:Slug", "/subcontractors")]
    public void ResolveToken_ReturnsContentValue(string key, string expected)
    {
        var provider = new JsonContentProvider(_dir);

        Assert.Equal(expected, provider.ResolveToken(key));
    }

    /// <summary>Price tokens format from the decimal in the currency symbol — the only source of "£".</summary>
    [Fact]
    public void ResolveToken_FormatsPrice_FromCurrency()
    {
        var provider = new JsonContentProvider(_dir);

        Assert.Equal("£10", provider.ResolveToken("Pricing:WorkerPassport:Annual"));
    }

    /// <summary>An unknown or malformed token fails loudly rather than rendering empty (Plan §8).</summary>
    [Theory]
    [InlineData("Products:Subcontractor:Nope")]
    [InlineData("Products:Missing:Name")]
    [InlineData("Pricing:WorkerPassport:Monthly")] // monthly is null in the seed
    [InlineData("Totally:Unknown:Token")]
    public void ResolveToken_Throws_OnUnknown(string key)
    {
        var provider = new JsonContentProvider(_dir);

        Assert.Throws<KeyNotFoundException>(() => provider.ResolveToken(key));
    }

    /// <summary>A missing required content file fails fast at construction.</summary>
    [Fact]
    public void Construction_Throws_WhenFileMissing()
    {
        File.Delete(Path.Combine(_dir, "products.json"));

        Assert.Throws<FileNotFoundException>(() => new JsonContentProvider(_dir));
    }

    /// <summary>Writes a minimal, valid content set to the temp directory.</summary>
    private void SeedValidContent()
    {
        Write("site.json", """
            { "BrandName": "Tedwren", "LegalEntity": "Tedwren Ltd", "CompanyNumber": null,
              "RegisteredOffice": null, "CurrencyCode": "GBP", "Social": [] }
            """);
        Write("products.json", """
            [
              { "ConfigKey": "Subcontractor", "DisplayName": "Tedwren for Subcontractors",
                "Slug": "/subcontractors", "Tagline": "t", "HeroCopy": "h",
                "PricingPlanKey": "WorkerPassport", "Features": [ { "Heading": "H", "Copy": "C" } ] },
              { "ConfigKey": "MainContractor", "DisplayName": "Tedwren for Main Contractors",
                "Slug": "/main-contractors", "Tagline": "t", "HeroCopy": "h",
                "PricingPlanKey": null, "Features": [] }
            ]
            """);
        Write("pricing.json", """
            [ { "Key": "WorkerPassport", "Name": "Worker Passport", "BandDescription": "per worker, per year",
                "AnnualPrice": 10, "MonthlyPrice": null, "ProductKey": null } ]
            """);
        Write("trust.json", """ [ { "Claim": "Your data stays yours.", "Logo": null } ] """);
        Write("faqs.json", """ [ { "Question": "Q?", "Answer": "A.", "LinkTarget": null } ] """);
        Write("testimonials.json", "[]");
    }

    /// <summary>Writes one content file into the temp directory.</summary>
    private void Write(string fileName, string json) => File.WriteAllText(Path.Combine(_dir, fileName), json);
}
