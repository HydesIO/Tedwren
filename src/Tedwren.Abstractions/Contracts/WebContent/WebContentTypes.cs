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
    string? PricingPlanKey,
    IReadOnlyList<ContentSection>? Sections = null)
{
    /// <summary>Page-specific narrative sections, or an empty list when none are configured.</summary>
    public IReadOnlyList<ContentSection> PageSections => Sections ?? Array.Empty<ContentSection>();
}

/// <summary>A single feature: a heading and its supporting copy (Plan §3).</summary>
/// <param name="Heading">Short feature heading.</param>
/// <param name="Copy">Supporting sentence.</param>
public sealed record FeatureCard(string Heading, string Copy);

/// <summary>
/// A titled narrative section on a product page (Plan §6.2, §6.3) — e.g. the understated CSCS add-on
/// line or the substantial retrofit / dispersed-site section. <see cref="Emphasis"/> marks a section
/// that gets a distinct visual treatment rather than a footnote.
/// </summary>
/// <param name="Heading">Section heading.</param>
/// <param name="Body">Section copy.</param>
/// <param name="Emphasis">When true, render with the distinct/emphasised treatment.</param>
public sealed record ContentSection(string Heading, string Body, bool Emphasis = false);

/// <summary>
/// The home page's narrative content (Plan §6.1): hero, the problem framing, the differentiator cards
/// and the "how it works" steps, plus the closing block. The two product cards on Home are rendered
/// short-form from the same <see cref="ProductProfile"/> entries as the dedicated pages, so there is no
/// separate home copy for them here.
/// </summary>
/// <param name="HeroHeading">Hero headline.</param>
/// <param name="HeroSubheading">Hero supporting line.</param>
/// <param name="ProblemHeading">Problem-section heading.</param>
/// <param name="ProblemBody">Problem-section copy.</param>
/// <param name="Differentiators">Differentiator cards (one is highlighted).</param>
/// <param name="HowItWorks">Ordered "how it works" steps.</param>
/// <param name="ClosingHeading">Closing-section heading.</param>
/// <param name="ClosingBody">Closing-section copy.</param>
public sealed record HomeContent(
    string HeroHeading,
    string HeroSubheading,
    string ProblemHeading,
    string ProblemBody,
    IReadOnlyList<Differentiator> Differentiators,
    IReadOnlyList<HowItWorksStep> HowItWorks,
    string ClosingHeading,
    string ClosingBody);

/// <summary>
/// A differentiator card on the home page (Plan §6.1). <see cref="Highlight"/> marks the strongest
/// differentiator ("Works beyond the site gate"), which is given a distinct visual treatment.
/// </summary>
/// <param name="Heading">Card heading.</param>
/// <param name="Body">Card copy.</param>
/// <param name="Highlight">When true, render as the highlighted differentiator.</param>
public sealed record Differentiator(string Heading, string Body, bool Highlight = false);

/// <summary>One step in the home page's five-step "how it works" sequence (Plan §6.1).</summary>
/// <param name="Order">1-based step number.</param>
/// <param name="Heading">Step heading.</param>
/// <param name="Body">Step copy.</param>
public sealed record HowItWorksStep(int Order, string Heading, string Body);

/// <summary>
/// The Worker Passport page content (Plan §6.4) — the individual-buyer register. Carries the "never
/// locked out for non-payment" benefit (PRD Rule W2), the consumer-contract facts (W7 informed consent
/// + UK 14-day cancellation), and CSCS-safe meta title/description. The price comes from the linked
/// <see cref="PricingPlan"/>, so there is a single price source (Plan §6.4, §13.1).
/// </summary>
/// <param name="Heading">Page H1.</param>
/// <param name="Subheading">Supporting hero line.</param>
/// <param name="HeroCopy">Hero paragraph.</param>
/// <param name="PricingPlanKey">Key of the pricing plan that carries the price (single source).</param>
/// <param name="Benefits">Benefit cards, including the "never locked out" benefit.</param>
/// <param name="ConsumerTerms">Consumer-contract facts (cancellation, distance selling).</param>
/// <param name="MetaTitle">CSCS-safe title tag (Plan §8.2).</param>
/// <param name="MetaDescription">CSCS-safe meta description (Plan §8.2).</param>
public sealed record WorkerPassportContent(
    string Heading,
    string Subheading,
    string HeroCopy,
    string PricingPlanKey,
    IReadOnlyList<FeatureCard> Benefits,
    IReadOnlyList<string> ConsumerTerms,
    string MetaTitle,
    string MetaDescription);

/// <summary>
/// The pricing page's narrative content (Plan §6.5): intro, the plain-language clarifiers (active
/// operative/site, 10% buffer overage) and the trust notes ("sites are free to record", "a dispersed
/// scheme = one site"). The numbers themselves come from <see cref="PricingPlan"/> — every number from
/// config, none typed into the view (Plan §6.5, §8).
/// </summary>
/// <param name="Heading">Page H1.</param>
/// <param name="Intro">Intro paragraph.</param>
/// <param name="Clarifiers">Plain-language pricing clarifiers.</param>
/// <param name="TrustNotes">Pricing trust notes.</param>
/// <param name="MetaDescription">Meta description.</param>
public sealed record PricingPageContent(
    string Heading,
    string Intro,
    IReadOnlyList<string> Clarifiers,
    IReadOnlyList<TrustPoint> TrustNotes,
    string MetaDescription);

/// <summary>
/// The Security &amp; Trust page content (Plan §6.6) — a stable URL for procurement/DPOs. Only claims
/// we can actually make now; there are no fabricated certification badges and no absolute-compliance
/// language (Plan §8.1).
/// </summary>
/// <param name="Heading">Page H1.</param>
/// <param name="Intro">Intro paragraph.</param>
/// <param name="Claims">The makeable claims, as titled sections.</param>
/// <param name="MetaDescription">Meta description.</param>
public sealed record SecurityContent(
    string Heading,
    string Intro,
    IReadOnlyList<ContentSection> Claims,
    string MetaDescription);

/// <summary>The About page content (Plan §6.7) — founder-led credibility.</summary>
/// <param name="Heading">Page H1.</param>
/// <param name="Intro">Intro paragraph.</param>
/// <param name="Sections">Story sections.</param>
/// <param name="MetaDescription">Meta description.</param>
public sealed record AboutContent(
    string Heading,
    string Intro,
    IReadOnlyList<ContentSection> Sections,
    string MetaDescription);

/// <summary>
/// A legal document rendered as real content, not a placeholder (Plan §4, W5). The final legal text is
/// subject to sign-off (Plan §11.4); the structure and drafting live here as content.
/// </summary>
/// <param name="Slug">URL slug (privacy, cookies, terms, data-protection).</param>
/// <param name="Title">Document title.</param>
/// <param name="LastReviewed">Human-readable review date/label.</param>
/// <param name="Sections">Document sections.</param>
/// <param name="MetaDescription">Meta description.</param>
public sealed record LegalDocument(
    string Slug,
    string Title,
    string LastReviewed,
    IReadOnlyList<ContentSection> Sections,
    string MetaDescription);

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
