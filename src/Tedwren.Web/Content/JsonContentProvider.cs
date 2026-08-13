using System.Globalization;
using System.Text.Json;
using Tedwren.Abstractions.Contracts.WebContent;
using Tedwren.Abstractions.Services;

namespace Tedwren.Web.Content;

/// <summary>
/// JSON-backed <see cref="IContentProvider"/> (Tedwren.Web Plan §2, §3). Loads the content model from
/// a directory of JSON files once at construction and serves it from memory. This is the launch
/// implementation of the content seam; a headless CMS can replace it behind the same interface with
/// no view changes. Registered as a singleton — content is read at startup, not per request.
/// </summary>
public sealed class JsonContentProvider : IContentProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Maps an ISO currency code to its display symbol — the single home for the symbol.</summary>
    private static readonly IReadOnlyDictionary<string, string> CurrencySymbols =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GBP"] = "£", // £
            ["EUR"] = "€", // €
            ["USD"] = "$",
        };

    private readonly IReadOnlyDictionary<string, ProductProfile> _productsByKey;
    private readonly IReadOnlyDictionary<string, PricingPlan> _plansByKey;

    /// <inheritdoc />
    public SiteContent Site { get; }

    /// <inheritdoc />
    public IReadOnlyList<ProductProfile> Products { get; }

    /// <inheritdoc />
    public IReadOnlyList<PricingPlan> PricingPlans { get; }

    /// <inheritdoc />
    public IReadOnlyList<TrustPoint> TrustPoints { get; }

    /// <inheritdoc />
    public IReadOnlyList<FaqItem> Faqs { get; }

    /// <inheritdoc />
    public IReadOnlyList<Testimonial> Testimonials { get; }

    /// <summary>Loads and caches the content model from the given directory.</summary>
    /// <param name="contentDirectory">Absolute path to the directory of JSON content files.</param>
    public JsonContentProvider(string contentDirectory)
    {
        Site = LoadObject<SiteContent>(contentDirectory, "site.json");
        Products = LoadArray<ProductProfile>(contentDirectory, "products.json");
        PricingPlans = LoadArray<PricingPlan>(contentDirectory, "pricing.json");
        TrustPoints = LoadArray<TrustPoint>(contentDirectory, "trust.json");
        Faqs = LoadArray<FaqItem>(contentDirectory, "faqs.json");
        Testimonials = LoadArray<Testimonial>(contentDirectory, "testimonials.json");

        _productsByKey = Products.ToDictionary(p => p.ConfigKey, StringComparer.OrdinalIgnoreCase);
        _plansByKey = PricingPlans.ToDictionary(p => p.Key, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public ProductProfile? FindProduct(string configKey) =>
        _productsByKey.TryGetValue(configKey, out var product) ? product : null;

    /// <inheritdoc />
    public PricingPlan? FindPricingPlan(string planKey) =>
        _plansByKey.TryGetValue(planKey, out var plan) ? plan : null;

    /// <inheritdoc />
    public string ResolveToken(string key)
    {
        var parts = key.Split(':');
        return parts switch
        {
            ["Site", "Brand"] => Site.BrandName,
            ["Site", "LegalEntity"] => Site.LegalEntity,
            ["Products", var pk, var field] => ResolveProductToken(pk, field, key),
            ["Pricing", var pk, var field] => ResolvePriceToken(pk, field, key),
            _ => throw UnknownToken(key),
        };
    }

    /// <summary>Resolves a product field token to its display string.</summary>
    private string ResolveProductToken(string productKey, string field, string fullKey)
    {
        var product = FindProduct(productKey) ?? throw UnknownToken(fullKey);
        return field switch
        {
            "Name" => product.DisplayName,
            "Slug" => product.Slug,
            "Tagline" => product.Tagline,
            _ => throw UnknownToken(fullKey),
        };
    }

    /// <summary>Resolves a price field token, formatted in the site currency.</summary>
    private string ResolvePriceToken(string planKey, string field, string fullKey)
    {
        var plan = FindPricingPlan(planKey) ?? throw UnknownToken(fullKey);
        return field switch
        {
            "Annual" => FormatMoney(plan.AnnualPrice),
            "Monthly" => plan.MonthlyPrice is { } monthly ? FormatMoney(monthly) : throw UnknownToken(fullKey),
            _ => throw UnknownToken(fullKey),
        };
    }

    /// <summary>Formats a decimal amount using the site currency symbol (Plan §8 — one home for "£").</summary>
    private string FormatMoney(decimal amount)
    {
        var symbol = CurrencySymbols.TryGetValue(Site.CurrencyCode, out var s) ? s : Site.CurrencyCode + " ";
        // Whole amounts render without trailing zeros (e.g. £10, not £10.00); otherwise two places.
        var formatted = amount == Math.Truncate(amount)
            ? amount.ToString("0", CultureInfo.InvariantCulture)
            : amount.ToString("0.00", CultureInfo.InvariantCulture);
        return symbol + formatted;
    }

    /// <summary>Builds the exception thrown for an unknown or malformed content token.</summary>
    private static KeyNotFoundException UnknownToken(string key) =>
        new($"Unknown content token '{key}'. Check the key against the content files.");

    /// <summary>Deserializes a single JSON object from a required content file.</summary>
    private static T LoadObject<T>(string directory, string fileName)
    {
        var json = ReadFile(directory, fileName);
        return JsonSerializer.Deserialize<T>(json, SerializerOptions)
            ?? throw new InvalidOperationException($"Content file '{fileName}' deserialized to null.");
    }

    /// <summary>Deserializes a JSON array from a required content file.</summary>
    private static IReadOnlyList<T> LoadArray<T>(string directory, string fileName)
    {
        var json = ReadFile(directory, fileName);
        return JsonSerializer.Deserialize<List<T>>(json, SerializerOptions)
            ?? throw new InvalidOperationException($"Content file '{fileName}' deserialized to null.");
    }

    /// <summary>Reads a required content file, failing fast with a clear message when it is missing.</summary>
    private static string ReadFile(string directory, string fileName)
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required content file not found: {path}", path);
        }

        return File.ReadAllText(path);
    }
}
