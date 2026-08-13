# Tedwren.Web — component & content catalogue

> Companion to `docs/Tedwren-Web-Plan-and-Scope-of-Works.md`. Created in phase **W2**. Every reusable
> View Component and content type on the marketing site is listed here, so components are reused rather
> than re-invented (Web Plan §5). New components are added as a row plus a View Component — not
> page-local duplicated markup. Colour/spacing come only from `tokens.css` (shared from
> `Tedwren.Client`); customer-visible copy comes only from the content layer.

## Content layer (Web Plan §3)

The content seam is `Tedwren.Abstractions.Services.IContentProvider`, implemented for launch by
`Tedwren.Web.Content.JsonContentProvider` (JSON files under `src/Tedwren.Web/Content/`, loaded once at
startup, registered as a singleton). A headless CMS can replace the provider behind the same interface
with no view changes. The types live in `Tedwren.Abstractions.Contracts.WebContent` so the
product-owned compliance-pack viewer (Plan §4.1) can reuse the same site/brand content.

| Content type | Backing file | Rendered where | Notes |
|---|---|---|---|
| `SiteContent` | `site.json` | Header, footer, titles | Brand, legal entity, company no./office, currency, launch social accounts. |
| `ProductProfile` | `products.json` | Home card (short) + product page (long) | One entry → both renderers; no copy fork (Plan §6.1). |
| `FeatureCard` | (within `products.json`) | Product pages, Home | Heading + copy. |
| `PricingPlan` | `pricing.json` | `/pricing`, any "from £x" | Prices are decimals here — the **only** home for prices (Plan §8). |
| `TrustPoint` | `trust.json` | Trust strip, `/security` | Only claims we can actually make (Plan §8.1). |
| `FaqItem` | `faqs.json` | `/faq`, FAQPage schema | Question, answer, optional deflect link. |
| `Testimonial` | `testimonials.json` | Testimonial wall | **Empty at launch** — no fabricated social proof (Plan §8.3). |

**Token indirection (Plan §2 naming rule).** `IContentProvider.ResolveToken(key)` resolves a dotted key
to a display string so names/prices are referenced by key, never inline: `Site:Brand`,
`Site:LegalEntity`, `Products:{key}:Name|Slug|Tagline`, `Pricing:{key}:Annual|Monthly` (formatted in the
site currency). An unknown token throws `KeyNotFoundException` so a typo fails loudly.

## View components (Web Plan §5)

Each is a parameterised View Component with a single responsibility. Nav **structure** (which pages, in
what order) comes from the `Site` config section (`Tedwren.Web.Configuration.SiteConfig`); **copy** comes
from the content layer.

| Component | Responsibility | Inputs | Status |
|---|---|---|---|
| `SiteHeader` | Logo → home, primary nav, persistent top-right CTA (swaps to "Get your Worker Passport" on that page only). Mobile hamburger keeps the CTA outside the collapsed menu. | `IContentProvider` (brand), `SiteConfig` (nav) | ✅ W1/W2 |
| `SiteFooter` | Legal entity + company no./office, site + legal link lists, config-gated social icons. | `IContentProvider` (identity/social), `SiteConfig` (links) | ✅ W1/W2 |
| `Cta` | Renders one of exactly three canonical CTAs from a closed `CtaAction` enum (Book a demo / Start a pilot / Get your Worker Passport); primary or secondary style. Vague labels are unrepresentable. | `CtaAction action`, `bool secondary` | ✅ W1 |
| `ProductCard` / `ProductDetail` | Short/long renderers over one `ProductProfile`. | `ProductProfile` | ⏳ W3 |
| `TrustStrip` | Trust points, referenced on Home/product pages, expanded on `/security`. | `IReadOnlyList<TrustPoint>` | ⏳ W3 |
| `FeatureGrid`, `Differentiators`, `HowItWorks` | Home/product building blocks. | content | ⏳ W3 |
| `PricingTable` | Pricing bands from `PricingPlan`, all numbers from content. | `IReadOnlyList<PricingPlan>` | ⏳ W4 |
| `FaqAccordion` | FAQ list + FAQPage schema. | `IReadOnlyList<FaqItem>` | ⏳ W5 |
| `ConsentBanner` | CMP-backed consent; no script fires pre-consent. | — | ⏳ W6 |
| `LeadForm` | Server-validated demo/contact form (antiforgery, honeypot, timing, rate limit). | form model | ⏳ W6 |
| `TestimonialWall` | Renders `Testimonial` list — empty at launch. | `IReadOnlyList<Testimonial>` | ⏳ W5 |

## Conventions

- **Reuse first.** Extend these components rather than adding page-local markup. A genuinely new
  pattern gets a row here plus a View Component.
- **Tokens only.** No colour/spacing literal outside `tokens.css`. Structural dimensions (max-width,
  font-size) are layout, not design tokens.
- **No price/name literals in views.** Prices come from `PricingPlan`; names from `ProductProfile` /
  `ResolveToken`. (Enforced by the W8 content lint.)
- **Summary comment on every class and method**, single responsibility per component.
