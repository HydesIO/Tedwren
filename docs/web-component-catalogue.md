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
| `HomeContent` | `home.json` | Home | Hero, problem, differentiators, how-it-works steps, closing block (Plan §6.1). |
| `ProductProfile` | `products.json` | Home card (short) + product page (long) | One entry → both renderers; no copy fork (Plan §6.1). Carries optional `ContentSection[]`. |
| `FeatureCard` | (within `products.json` / `worker-passport.json`) | Product pages, Home, Worker Passport | Heading + copy. |
| `ContentSection` | (within products/security/about/legal) | Product, Security, About, Legal pages | Titled section; `Emphasis` for distinct treatment (CSCS line, retrofit section). |
| `Differentiator` | (within `home.json`) | Home | Card with optional `Highlight` for the strongest differentiator. |
| `HowItWorksStep` | (within `home.json`) | Home | Ordered "how it works" step. |
| `WorkerPassportContent` | `worker-passport.json` | `/worker-passport` | Individual-buyer register: benefits (incl. "never locked out"), consumer terms, CSCS-safe meta (Plan §6.4, §8.2). |
| `PricingPageContent` | `pricing-page.json` | `/pricing` | Intro, plain-language clarifiers, trust notes (Plan §6.5). Numbers come from `PricingPlan`. |
| `SecurityContent` | `security.json` | `/security` | Makeable claims only — no fabricated badges (Plan §6.6, §8.1). |
| `AboutContent` | `about.json` | `/about` | Founder-led credibility. |
| `LegalDocument` | `legal.json` | `/legal/{slug}` | Real legal content (draft pending sign-off, Plan §11.4). |
| `PartnerProgrammeContent` | `partners.json` | `/partners` | How-it-works, commission terms, and the §7.3 exclusion statement. |
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
| `ProductCard` / `ProductDetail` | Short/long renderers over one `ProductProfile` (by key). | `string configKey` | ✅ W3 |
| `TrustStrip` | Trust points, referenced on Home/product pages, expanded on `/security`. | `IContentProvider` | ✅ W3 |
| `FeatureGrid` | Grid of feature cards, reused on product pages and Home. | `IReadOnlyList<FeatureCard>` | ✅ W3 |
| `Differentiators` | Home differentiator cards; highlights the strongest one. | `IContentProvider` | ✅ W3 |
| `HowItWorks` | Home five-step sequence. | `IContentProvider` | ✅ W3 |
| `PricingTable` | Pricing bands from `PricingPlan`; formats prices, "Pricing on request" for unpriced bands. | `IContentProvider` | ✅ W4 |
| `FaqAccordion` | FAQ list as native `<details>` (JS-free). | `IContentProvider` | ✅ W5 |
| `ConsentBanner` | Cookie-consent banner (form-based, JS-free); one-click reject; shown until a choice is made. | — | ✅ W6 |
| `PackChrome` | Light header/footer for the product's compliance-pack viewer; UTM-tagged "Book a demo" (Plan §4.1). | `bool footer` | ✅ W6 |
| `TestimonialWall` | Renders `Testimonial` list — empty at launch. | `IReadOnlyList<Testimonial>` | ⏳ W5 |

The demo (`/demo`) and contact (`/contact`) forms are page views bound to `DemoRequest` / `ContactRequest`
and processed by `LeadController`: DataAnnotations validation, antiforgery, a honeypot + minimum fill-time
(`AntiBot`), rate limiting, then routing via the `ILeadRouter` seam (`LoggingLeadRouter` at launch;
`ContactRouting` maps a reason → inbox). Demo submissions capture UTM attribution and redirect to a
thank-you page with the booking link. Consent is recorded by `ConsentController` (`/consent`) into the
`tedwren_consent` cookie; `AnalyticsState` gates GA4 so nothing fires without both consent and a
configured measurement id.

The **partner programme** (`/partners`, `/partners/dashboard/{code}`, `/r/{code}`) lives under
`Tedwren.Web.Partners`: an `IPartnerStore` seam (in-memory at launch), `PartnerService`
(applications → pending; human approval mints a `Partner` with a unique referral code; refuses §7.3
site-access controllers) and `ReferralService` (attribution + 20% commission tied to a specific
referral with a 90-day, reversible clawback). Applications never self-activate. Referral links set the
`tedwren_ref` cookie so a later demo conversion is attributed across sessions.

## Conventions

- **Reuse first.** Extend these components rather than adding page-local markup. A genuinely new
  pattern gets a row here plus a View Component.
- **Tokens only.** No colour/spacing literal outside `tokens.css`. Structural dimensions (max-width,
  font-size) are layout, not design tokens.
- **No price/name literals in views.** Prices come from `PricingPlan`; names from `ProductProfile` /
  `ResolveToken`. (Enforced by the W8 content lint.)
- **Summary comment on every class and method**, single responsibility per component.
- **SEO schema** is built by `Tedwren.Web.Seo.JsonLd` (Organization sitewide via the layout,
  FAQPage on `/faq`, SoftwareApplication on `/pricing`). Each method returns a complete `<script>`
  block emitted with `@Html.Raw` so it isn't routed through the encoder. Page meta descriptions are set
  via `ViewData["MetaDescription"]`. The web encoder is configured for `UnicodeRanges.All` so the £
  symbol and typographic punctuation render as real characters, not numeric entities.
