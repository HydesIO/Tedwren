using Tedwren.Abstractions.Contracts.WebContent;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The marketing site's content seam (Tedwren.Web Plan §2, §3). Views and components resolve every
/// customer-visible string through this interface rather than hardcoding it, so product names, prices
/// and copy change without a redeploy. The launch implementation is JSON-backed; a headless CMS can
/// replace it behind the same interface with no view changes.
/// </summary>
public interface IContentProvider
{
    /// <summary>Site-wide identity/brand content (brand, legal entity, currency, social).</summary>
    SiteContent Site { get; }

    /// <summary>The home page's narrative content (hero, problem, differentiators, how-it-works).</summary>
    HomeContent Home { get; }

    /// <summary>All product profiles, in declared order.</summary>
    IReadOnlyList<ProductProfile> Products { get; }

    /// <summary>All pricing plans, in declared order.</summary>
    IReadOnlyList<PricingPlan> PricingPlans { get; }

    /// <summary>Trust points for the trust strip / security page.</summary>
    IReadOnlyList<TrustPoint> TrustPoints { get; }

    /// <summary>FAQ items for the FAQ page and FAQPage schema.</summary>
    IReadOnlyList<FaqItem> Faqs { get; }

    /// <summary>Testimonials — empty at launch (no fabricated social proof, Plan §8.3).</summary>
    IReadOnlyList<Testimonial> Testimonials { get; }

    /// <summary>Finds a product by its <see cref="ProductProfile.ConfigKey"/>, or null.</summary>
    /// <param name="configKey">Stable product key (e.g. "Subcontractor").</param>
    ProductProfile? FindProduct(string configKey);

    /// <summary>Finds a pricing plan by its <see cref="PricingPlan.Key"/>, or null.</summary>
    /// <param name="planKey">Stable plan key (e.g. "WorkerPassport").</param>
    PricingPlan? FindPricingPlan(string planKey);

    /// <summary>
    /// Resolves a dotted content token to its display string, so names and prices can be referenced
    /// by key inline (Plan §2 naming rule). Supported forms include <c>Site:Brand</c>,
    /// <c>Site:LegalEntity</c>, <c>Products:{key}:Name|Slug|Tagline</c>, and
    /// <c>Pricing:{key}:Annual|Monthly</c> (formatted in the site currency). Throws
    /// <see cref="KeyNotFoundException"/> for an unknown token so a typo fails loudly rather than
    /// rendering an empty string.
    /// </summary>
    /// <param name="key">The dotted token key.</param>
    string ResolveToken(string key);
}
