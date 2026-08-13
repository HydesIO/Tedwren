namespace Tedwren.Abstractions.Contracts.WebContent;

// Strongly-typed content model for the public marketing site (Tedwren.Web Plan §3, §12.2). These are
// the config source of truth: nothing customer-visible that could change — product names, prices,
// copy — is typed into a .cshtml file. The JSON-backed IContentProvider binds these now; a headless
// CMS can be slotted behind the same interface later with no view changes. They live in Abstractions
// so the product-owned compliance-pack viewer (Plan §4.1) can reuse the same brand/site content.

/// <summary>
/// Site-wide identity and brand content (Plan §3 "SiteConfig" content type): the legal entity pulled
/// from the Companies House filing, the trading brand, and the social accounts that actually exist at
/// launch. <see cref="CurrencyCode"/> is the single source for the price symbol so no "£" literal is
/// needed in a view or content string (Plan §8).
/// </summary>
/// <param name="BrandName">Trading name shown in the header and titles (e.g. "Tedwren").</param>
/// <param name="LegalEntity">Registered legal entity for the footer (e.g. "Tedwren Ltd").</param>
/// <param name="CompanyNumber">Companies House registered number, if published.</param>
/// <param name="RegisteredOffice">Registered office address, if published.</param>
/// <param name="CurrencyCode">ISO currency code (e.g. "GBP") the price formatter maps to a symbol.</param>
/// <param name="Social">Social accounts that exist at launch; empty renders no footer icons.</param>
public sealed record SiteContent(
    string BrandName,
    string LegalEntity,
    string? CompanyNumber,
    string? RegisteredOffice,
    string CurrencyCode,
    IReadOnlyList<SocialAccount> Social);

/// <summary>A configured social account, rendered as a footer icon only when present (Plan §5).</summary>
/// <param name="Platform">Platform key used to pick the icon (e.g. "linkedin").</param>
/// <param name="Url">Absolute URL to the account.</param>
public sealed record SocialAccount(string Platform, string Url);

/// <summary>
/// One product, rendered both short-form (Home card) and long-form (dedicated page) from this single
/// entry so there is no copy fork (Plan §6.1, §12.2). Referenced by <see cref="ConfigKey"/>, never by
/// an inline name, so a rename is a content edit plus a redirect, not a redeploy (Plan §2, §3).
/// </summary>
/// <param name="ConfigKey">Stable key used to reference this product (e.g. "Subcontractor").</param>
/// <param name="DisplayName">Customer-facing product name.</param>
/// <param name="Slug">Audience/function URL slug (e.g. "/subcontractors").</param>
/// <param name="Tagline">One-line outcome statement.</param>
/// <param name="HeroCopy">Hero paragraph for the dedicated page.</param>
/// <param name="Features">Feature cards, reused short/long form.</param>
/// <param name="PricingPlanKey">Optional link to the <see cref="PricingPlan"/> that prices it.</param>
public sealed record ProductProfile(
    string ConfigKey,
    string DisplayName,
    string Slug,
    string Tagline,
    string HeroCopy,
    IReadOnlyList<FeatureCard> Features,
    string? PricingPlanKey);

/// <summary>A single feature: a heading and its supporting copy (Plan §3).</summary>
/// <param name="Heading">Short feature heading.</param>
/// <param name="Copy">Supporting sentence.</param>
public sealed record FeatureCard(string Heading, string Copy);

/// <summary>
/// A pricing plan (Plan §3, §6.5). Prices are decimals here — the only place they live — so every
/// "from £x" on the site is formatted from this record, never typed inline (Plan §8).
/// </summary>
/// <param name="Key">Stable key (e.g. "WorkerPassport").</param>
/// <param name="Name">Plan name shown on the pricing page.</param>
/// <param name="BandDescription">Plain-language band/unit description (e.g. "per active operative").</param>
/// <param name="AnnualPrice">Annual price in major units of the site currency.</param>
/// <param name="MonthlyPrice">Optional monthly price in major units.</param>
/// <param name="ProductKey">Optional <see cref="ProductProfile.ConfigKey"/> this plan prices.</param>
public sealed record PricingPlan(
    string Key,
    string Name,
    string BandDescription,
    decimal AnnualPrice,
    decimal? MonthlyPrice,
    string? ProductKey);

/// <summary>A trust claim shown in the trust strip / security page (Plan §3, §5.3, §6.6).</summary>
/// <param name="Claim">The claim text — only claims we can actually make (Plan §8.1).</param>
/// <param name="Logo">Optional logo asset path.</param>
public sealed record TrustPoint(string Claim, string? Logo);

/// <summary>A frequently-asked question and its answer (Plan §3, §6.8), used for the FAQ + schema.</summary>
/// <param name="Question">The question.</param>
/// <param name="Answer">Short answer.</param>
/// <param name="LinkTarget">Optional page the answer deflects to.</param>
public sealed record FaqItem(string Question, string Answer, string? LinkTarget);

/// <summary>
/// A customer testimonial (Plan §3, §9). The schema exists but ships empty at launch — there is no
/// path to render fabricated social proof (Plan §8.3).
/// </summary>
/// <param name="Quote">The testimonial text.</param>
/// <param name="Author">Person quoted.</param>
/// <param name="Company">Their company.</param>
/// <param name="Logo">Optional company logo path.</param>
public sealed record Testimonial(string Quote, string Author, string Company, string? Logo);
