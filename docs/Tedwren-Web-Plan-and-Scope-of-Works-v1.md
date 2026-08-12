# Tedwren.Web — Plan & Scope of Works (v1)

> **Status:** Planning only. This document proposes the build; no application code, Razor, CSS or
> configuration is written in this pass. It is the counterpart to `docs/plan-and-scope.md` (the
> admin-console / backend plan) for the new public marketing website.
>
> **Authoritative source of truth:** `docs/TedwrenPRDv6_4.docx` (mirror: `docs/TedwrenPRDv6_4.md`).
> Where this plan and the PRD disagree, the PRD wins and the discrepancy is raised in §2, not worked
> around silently. PRD requirement/rule IDs (SF/SUB/MC, R1–R18) are referenced inline.

---

## 1. Objective & audience

Build the public-facing SaaS marketing website for Tedwren Ltd — the site a specialist
subcontractor or a regional main contractor lands on, understands the offer from, and converts on.
Three jobs, in order:

1. **Explain the two products** — the subcontractor product (time and attendance) and the main
   contractor product (workforce management) — so a visitor self-identifies as one buyer or the
   other within one screen.
2. **Convert** — every page routes to a small, consistent set of calls to action (start a pilot,
   book a demo, get pricing).
3. **Establish credibility** — UK data residency (R13), audit-grade evidence, no app install (R1),
   and a defensible site-entry decision (R10) — stated accurately, without overclaiming.

**Secondary job:** the landing surface for the "find out more" route embedded in every compliance
pack (PRD **SUB-25**) — the cheapest acquisition channel in the product. The site must expose a
campaign-attributable entry point for it.

**Audience.** Two buyers with different pains:

| Buyer | Pain we lead with | Not competitive on |
|---|---|---|
| Specialist subcontractor | Duplicated evidence work; workers blocked at gates; cards that expire silently | Buddy-punching / hours fraud (no answer until Phase 5 — qualify out) |
| Regional main contractor | Reconstructing who was on site and why they were let in; dispersed schemes with no compound | — |

**Register.** Reads like the same company that made the admin console — same accent, same neutrals,
same one-step elevation, same restraint — but with the wider spacing, larger type and full-bleed
sections a public site needs and an admin console does not.

---

## 2. Conflicts & constraints found

The brief asked this plan to reconcile itself against the PRD and flag conflicts explicitly rather
than resolve them silently. Two were already known (brief §2: this is *not* a shared component
library, only a shared brand; brief §3: the C1–C8 content constraints). The following were found in
addition, and are carried into the relevant sections below.

| # | Finding | Detail | Resolution in this plan |
|---|---|---|---|
| F1 | **The referenced house-style document does not exist** | `Tedwren-UIUX-Plan-and-Scope-of-Works-v1.md` is not in the repo or in git history. Its §4 (tokens) and §5 (components) survive only as `src/Tedwren.Client/wwwroot/css/tokens.css` and `docs/component-catalogue.md`; the closest live sibling is `docs/plan-and-scope.md`. | House style reconstructed from those three files. Token values in §5 are taken from the *live* `tokens.css`, so there is no risk of quoting a stale spec. |
| F2 | **Differentiator #3 is not a whole-site pillar** | The brief lists "a site-entry decision that explains itself" among the five differentiators without scoping it. **R18** and **SUB-12** forbid the subcontractor product from presenting anything that reads as a site-access decision. | The decision/"permitted"/"denied"/reconstructable-entry language is **main-contractor content only**. Subcontractor copy uses *recorded* and *site-ready*. Enforced by the content checklist in §9 and the C3 mechanism in §8. |
| F3 | **CSCS is not a legal requirement** | PRD glossary: "CSCS — Not a legal requirement, but most large contractors require a card." The brief's C5 covers the Building Safety Act and CDM but not CSCS. | Extends C5: copy may say cards are *required by most large contractors*, never that they are *legally mandated*. Added to the content standard. |
| F4 | **The live demo URL carries a withdrawn product name** | PRD 1.1 / header: `https://inducted-mvp.vercel.app` "carries a withdrawn product name and should not be read as one." | The site must not link that URL. "Book a demo" is a human-scheduled call (§8); if a self-serve demo is ever linked it is proxied behind a neutral path on the marketing domain. This is a C1 concern and is included in the C1 guard scope. |
| F5 | **SUB-25 priority and the pack's cookie-free property are more precise than the brief states** | SUB-25 is **P1**, not P0. The "no cookie banner" property comes from SUB-17's §5.2 *acceptance criteria* ("no registration wall, no cookie banner and no sign-up prompt"), not the SUB-17 requirement row. The PRD does **not** state that the pack is served from a separate domain. | C8's domain/path separation is therefore an **assumption**, surfaced in §13 for sign-off, not asserted as PRD fact. The consent/analytics scoping in §9 is written so it holds under either a separate-domain or a separate-path model. |
| F6 | **Dark mode is out of scope but ships in the shared tokens** | The shared `tokens.css` already carries a full `.theme-dark` override block. The brief puts dark mode out of scope but says "do not block it." | Share the file whole (the dark variables come along for free); do **not** wire a theme toggle in this phase. No conflict once stated. |

Everything in the brief's C1–C8 table is accepted and given an enforcement mechanism in §8/§9 — these
are commercial, legal and safety constraints, not preferences, so the plan builds mechanisms that make
them hard to breach rather than merely noting them.

---

## 3. Technology baseline

Versions verified against Microsoft/NuGet on 2026-08-12 (not from training data). Verify again at
build start — .NET services monthly.

| Concern | Decision |
|---|---|
| Framework | **.NET 10** (LTS; GA 2025-11-11, supported to 2028-11-10). Latest SDK **10.0.400**; ASP.NET Core Runtime **10.0.11**. Matches every existing project's `net10.0` target. |
| Web stack | **ASP.NET Core MVC**, server-rendered Razor views. Controllers + view models, one controller per content area. MVC ships in the shared framework via `Microsoft.NET.Sdk.Web` — **no separate MVC NuGet package**. |
| Project name | `Tedwren.Web` (+ `Tedwren.Web.Tests`). |
| Components | Custom parameterised **View Components** and **Tag Helpers**, single responsibility. No page-local duplicated markup. |
| Styling | Clean CSS. Shared `tokens.css` (see §4) + `site.css` + per-component CSS, bundled predictably. **No CSS framework, no utility-class soup, no colour literals outside the token file.** |
| JavaScript | Minimal, progressive-enhancement only, vanilla ES modules. Site fully readable and navigable with JS disabled. No SPA framework. |
| Content | Strongly-typed options bound from JSON/appsettings (products, features, pricing, FAQ, testimonials, glossary). No CMS this phase — the `IContentProvider` seam is built so one can be added. |
| Forms | MVC model binding, DataAnnotations validation, antiforgery tokens, honeypot + submission-timing check, rate limiting. |
| Lead capture | Behind `ILeadCaptureService` with a logging / in-memory implementation this phase; swappable for CRM/email later. |
| Hosting | To be confirmed; **UK region required** for any personal data (**R13**). Surfaced in §13. |
| Testing | `xunit.v3` **3.2.2**; `Microsoft.AspNetCore.Mvc.Testing` **10.0.x** (repo pins 10.0.10; latest 10.0.11) for route/render smoke tests; `Microsoft.Playwright` **1.61.0** driving an automated accessibility check (axe) in CI. |

**Package-management note.** The solution has **no** `global.json`, `Directory.Build.props` or
`Directory.Packages.props`; versions are declared inline per `.csproj`. `Tedwren.Web` follows that
convention. *Optional, low-cost:* introduce `Directory.Packages.props` (central package management)
when adding the new projects, so `Tedwren.Web` and the test project cannot drift from the framework
version — recommended but flagged, not assumed (§13).

**Seams that must never require touching a view:** content source (`IContentProvider`), lead
destination (`ILeadCaptureService`), subscription (`ISubscriptionService`), analytics provider, and
product naming (§8, C1). Each is an interface or an options class.

---

## 4. Solution structure

`Tedwren.Web` **sits in the existing `Tedwren.sln`.** One repo, one CI pipeline, and — decisively —
token sharing becomes a project reference rather than a package publish or a git submodule. The cost
is that the solution now spans a Blazor WASM console and an MVC site; that is acceptable and already
true in spirit (the solution spans WASM + minimal-API today). A separate repo was considered and
rejected: it would force the shared tokens through a NuGet package or submodule and split CI for no
deployment benefit this phase (the two apps deploy independently regardless of repo layout).

### How the shared design tokens travel

`tokens.css` currently lives in **the Client's** `wwwroot` (`src/Tedwren.Client/wwwroot/css/tokens.css`),
which an MVC app cannot consume. Four options were weighed:

| Option | Drift risk | Cost | Verdict |
|---|---|---|---|
| Copy the file into `Tedwren.Web` | High — two files, hand-synced | Low | Rejected |
| Build-time copy step (MSBuild target) | Low | Medium; opaque | Rejected |
| Linked `<Content Include Link=…>` in each csproj | None (one source file) | Low; two *served* copies | Acceptable fallback |
| **Shared static-asset RCL** | **None** | **Low–medium** | **Recommended** |

**Recommended:** a new minimal Razor Class Library **`Tedwren.Brand`** (`Microsoft.NET.Sdk.Razor`,
**no MudBlazor dependency** — it contains only `wwwroot` assets) holding the single physical
`tokens.css`. Both the Client and `Tedwren.Web` reference it and load
`_content/Tedwren.Brand/css/tokens.css`. One file, served identically to both surfaces, drift
impossible.

- **Migration:** move `tokens.css` from the Client's `wwwroot` into `Tedwren.Brand`; update the one
  `<link>` in `src/Tedwren.Client/wwwroot/index.html` to the `_content/…` path. Behaviourally
  identical for the console.
- **Caveat:** `src/Tedwren.UiComponents/Theme/TedwrenTheme.cs` is a hand-synced C# mirror of the same
  values for MudBlazor's internals; it stays a Blazor-only concern and is untouched. The marketing
  site reads the CSS variables directly and needs no C# theme object.

### Proposed folder structure

```
Tedwren.sln
src/
  Tedwren.Brand              NEW  static-asset RCL — the single shared tokens.css (no Mud dependency)
  Tedwren.Web               NEW  ASP.NET Core MVC marketing site
    Controllers/                 Home, About, Products, Features, Pricing, Faq, Contact, Legal, Campaign, Error
    ViewModels/                  one per page + shared section view models (HeroVm, CtaBandVm, …)
    Views/
      Shared/
        _Layout.cshtml           shell: header, footer, skip link, consent slot
        Components/{Name}/Default.cshtml   one folder per View Component
      Home/ About/ Products/ Features/ Pricing/ Faq/ Contact/ Legal/ Campaign/
    TagHelpers/                  ProductNameTagHelper, IconTagHelper, MediaFrameTagHelper, CanonicalTagHelper, …
    ViewComponents/              SiteHeader, HeroBanner, CtaBand, FeatureCard, PricingTable, FaqAccordion, …
    Content/                     products.json, features.json, pricing.json, faq.json, testimonials.json, glossary.json
    Options/                     ProductNamingOptions, BrandOptions, PricingOptions, FeatureContentOptions, SeoOptions, ConsentOptions
    Services/                    IContentProvider, ILeadCaptureService, ISubscriptionService (+ logging/in-memory impls)
    wwwroot/
      css/  site.css, components/*.css        (tokens.css arrives via _content/Tedwren.Brand)
      js/   consent.js, nav.js, reveal.js, forms.js   (ES modules, progressive enhancement)
      img/  (MediaFrame imagery; placeholders marked)
      icons/ sprite.svg                        (single inline SVG sprite)
      robots.txt  sitemap.xml
  … existing 7 projects (Client, Abstractions, Domain, Application, DataAccess, Api, UiComponents)
tests/
  Tedwren.Web.Tests          NEW  view-model + content-binding unit tests, route/render smoke tests,
                                  C1 name-guard test, C2 pricing-vs-roadmap test, axe a11y check
  … existing 5 test projects
docs/
  component-catalogue-web.md NEW  living component inventory for Tedwren.Web (mirrors component-catalogue.md)
```

---

## 5. Design direction & tokens

Direction is pinned by the brief: match the admin console. One accent, one icon style, one shadow.
No gradient meshes, no glassmorphism, no drop shadows on text, no stock hard-hat hero.

### 5.1 Shared tokens (from the live `tokens.css` — reproduced, not re-typed)

These are consumed via `_content/Tedwren.Brand/css/tokens.css`; the marketing site **must not**
redeclare any value.

| Group | Token | Value |
|---|---|---|
| Surfaces | `--color-bg` / `--color-surface` | `#F7F8FA` / `#FFFFFF` |
| Text | `--color-text-primary` / `-secondary` / `-muted` | `#101828` / `#667085` / `#98A2B3` |
| Border | `--color-border` | `#E4E7EC` |
| Brand | `--color-brand` / `-pale` / `-dark` | `#E8590C` / `#FFF1E8` / `#B8430C` |
| Status | success/warning/danger/info/permit (+ `-pale` pairs) | `#17B26A` / `#F79009` / `#F04438` / `#2E90FA` / `#7F56D9` |
| Radius | `--radius-card` / `--radius-control` | `10px` / `8px` |
| Elevation | `--shadow-card` | `0 1px 2px rgba(16,24,40,.04), 0 1px 3px rgba(16,24,40,.06)` |
| Type | `--font-family` | `"Inter", -apple-system, … , sans-serif` |
| Spacing | `--spacing-4 … --spacing-32` | `4 / 8 / 12 / 16 / 20 / 24 / 32 px` |

**Semantic-colour discipline:** success/danger/etc. carry *status meaning* in the console. On a
marketing page they must not be used decoratively (no green ticks / red crosses as ornament) — only
where a genuine status is being shown (e.g. Available vs On-the-roadmap badges).

### 5.2 New tokens proposed (site-level, additive — declared in `site.css`, never in the shared file)

| Token | Value | Why |
|---|---|---|
| `--radius-media` | `16px` | Larger corner radius for imagery/screenshots (brief §6). |
| `--color-brand-text` | `#B8430C` | Accessible brand alias for brand-coloured **body text and links** (see 5.4). Points at the existing `--color-brand-dark` hue; named so intent is explicit. |
| `--spacing-40 / -48 / -64 / -80` | `40 / 48 / 64 / 80 px` | Section rhythm and full-bleed vertical space a public site needs and the console does not. |
| `--measure` | `68ch` | Body line length, within the 65–75-character target. |
| Type-scale tokens | see 5.3 | The shared file has **no numeric type scale** (console sizes come from MudBlazor defaults); the marketing site needs an explicit one. |

### 5.3 Type scale (one family — Inter — with a display treatment)

The disciplined answer the brief asks for: **no second family.** The display treatment is heavier
weight + tighter tracking of Inter, keeping the two surfaces cohesive and licensing/self-hosting
trivial (Inter is already the console face). Self-hosted, `font-display: swap`, subset, preloaded.

| Role | Token | Size (rem/px) | Weight | Line height | Tracking |
|---|---|---|---|---|---|
| Display XL (hero h1) | `--type-display-xl` | 3.5 / 56 | 700 | 1.05 | −0.02em |
| Display L (section h2) | `--type-display-l` | 2.25 / 36 | 700 | 1.15 | −0.015em |
| Heading M (h3 / card title) | `--type-heading-m` | 1.5 / 24 | 600 | 1.25 | −0.01em |
| Heading S (h4) | `--type-heading-s` | 1.25 / 20 | 600 | 1.3 | 0 |
| Body L (lead / subhead) | `--type-body-l` | 1.25 / 20 | 400 | 1.6 | 0 |
| Body (default) | `--type-body` | 1.0 / 16 | 400 | 1.6 | 0 |
| Small / caption | `--type-small` | 0.875 / 14 | 400 | 1.5 | 0 |
| Eyebrow / overline | `--type-eyebrow` | 0.8125 / 13 | 600 | 1.4 | 0.08em (uppercase) |

Display sizes are fluid-clamped down one step at the mobile breakpoint (hero 56→36px, section 36→28px).

### 5.4 Contrast — computed, with the compliant rule (WCAG 2.2 AA)

The brief requires brand orange on white and on the pale tint to be validated and any failure
flagged with a compliant variant. Computed:

| Foreground | Background | Ratio | Normal text (≥4.5) | Large / UI (≥3.0) |
|---|---|---|---|---|
| `--color-brand` `#E8590C` | `#FFFFFF` | **3.58:1** | ✗ **fail** | ✓ pass |
| `--color-brand` `#E8590C` | tint `#FFF1E8` | **3.24:1** | ✗ **fail** | ✓ pass |
| `--color-brand-dark` `#B8430C` | `#FFFFFF` | **5.46:1** | ✓ pass | ✓ pass |
| `--color-brand-dark` `#B8430C` | tint `#FFF1E8` | **4.94:1** | ✓ pass | ✓ pass |
| `#101828` text | `#FFFFFF` | ~16:1 | ✓ pass | ✓ pass |
| `#667085` secondary text | `#FFFFFF` | ~4.9:1 | ✓ pass | ✓ pass |

**Rule (binding on all copy):** `--color-brand` `#E8590C` is for **large headings (≥24px, or ≥18.66px
bold), icons, accents and UI fills only**. Any **brand-coloured body text or inline link** on white or
tint uses **`--color-brand-text` `#B8430C`**. Primary buttons use a brand fill with **white** text
(fill contrast is a UI-component check, not a text check, and the button label sits on the fill);
button label legibility is verified in the a11y pass. `--color-text-muted` `#98A2B3` (~2.5:1) is for
**non-text decoration only**, never body copy.

### 5.5 Surfaces, geometry, imagery, motion

- **Surfaces:** `#FFFFFF` cards on `#F7F8FA`; alternate section backgrounds between the two for
  rhythm; the pale brand tint for **at most one or two sections per page**.
- **Geometry:** card `10px`, control `8px`, media `16px`; the single `--shadow-card`. Card hover is a
  1px translate + border emphasis, **not** a second shadow.
- **Icons:** one outline set, single stroke weight, delivered as an **inline SVG sprite** via an
  `Icon` tag helper — never `<img>`, never an icon font. Icon-only controls carry accessible names.
- **Imagery:** prefer icons over photography. Where used, images sit in a `MediaFrame`:
  `--radius-media`, `overflow:hidden`, 1px `--color-border`, fixed aspect per usage — **16:10**
  product shots, **4:3** people/site, **1:1** avatars. Every image: meaningful `alt`, explicit
  `width`/`height`, lazy-load below the fold, modern format + fallback. **Product screenshots come from
  the admin console and must not show a product name (C1) or unavailable capability (C2)** — placeholder
  assets are used with a named handover step (§13).
- **Motion:** subtle scroll-reveal on section entry; hover on cards/buttons only.
  `prefers-reduced-motion` disables reveal and transitions everywhere. No parallax, no autoplaying
  video with sound.

---

## 6. Component inventory

Custom parameterised View Components / Tag Helpers, single responsibility, **no hard-coded copy**
(everything via parameters or content configuration). **House rule: switches over checkboxes** for
binary inputs; checkboxes reserved for genuine multi-select. Each ships a `docs/component-catalogue-web.md`
entry (props table, usage snippet, screenshot).

### Shell & navigation

| Component | Single responsibility | Key parameters |
|---|---|---|
| `SiteHeader` | Sticky header; condenses on scroll | `NavModel`, `IsCondensed`, `PrimaryCta` |
| `PrimaryNav` | Top nav with a two-product mega-menu | `Sections`, `ActivePath` |
| `MobileNavDrawer` | Off-canvas nav under the mobile breakpoint | `NavModel`, `IsOpen` |
| `SiteFooter` | Footer: nav columns, legal links, newsletter slot | `FooterModel`, `NewsletterForm` |
| `SkipLink` | "Skip to content" for keyboard/AT | `TargetId` |
| `BreadcrumbBar` | Breadcrumb trail + `BreadcrumbList` JSON-LD | `Trail` |
| `CookieConsentBanner` | Consent UI with genuine reject; gates non-essential tags | `ConsentModel` (never rendered on the pack surface — C8) |

### Hero & sections

| Component | Single responsibility | Key parameters |
|---|---|---|
| `HeroBanner` | Above-fold hero | `Eyebrow`, `Headline`, `Subhead`, `PrimaryCta`, `SecondaryCta`, `Media`, `TrustStrip` |
| `SectionHeader` | Eyebrow + title + intro, one alignment rule | `Eyebrow`, `Title`, `Intro`, `Align` |
| `SplitFeature` | Alternating media/copy | `Media`, `Copy`, `MediaSide` (left/right), `Cta?` |
| `LogoStrip` | Row of trust/partner logos | `Logos` |
| `StatBand` | Row of outcome stats | `Stats` (figures we can stand behind only) |
| `CtaBand` | The repeated mid/end conversion block | `Heading`, `Body`, `PrimaryCta`, `SecondaryCta?`, `Variant` |

### Content & data display

| Component | Single responsibility | Key parameters |
|---|---|---|
| `FeatureCard` | One feature: icon, title, body, availability | `Icon`, `Title`, `Body`, `Availability` |
| `ProductCard` | One product summary + "Which are you?" prompt | `Product`, `Audience`, `Cta` |
| `ComparisonTable` | Two products side by side; optionally us-vs-category (factual, **no named competitors**) | `Columns`, `Rows`, `Mode` |
| `PricingTable` / `PricingCard` | Render a pricing band | `Band`, `IsFeatured`, `PriceDisplay`, `Inclusions`, `Cta` |
| `PricingToggle` | Operative band ↔ per-site meter (**a switch**, house rule) | `Mode`, `OnChange` |
| `FaqAccordion` / `FaqItem` | Progressively enhanced `<details>` groups | `Groups` / `Question`, `Answer` |
| `TestimonialCard` | One quote/attribution | `Quote`, `Attribution`, `Avatar?` |
| `TimelineList` | Roadmap phases as a sequence | `Phases` |
| `StatusBadge` | Available / On the roadmap, visually distinct | `Availability` |
| `MediaFrame` | The rounded-rect image wrapper | `Src`, `Alt`, `AspectRatio`, `Width`, `Height`, `Loading` |
| `IconTile` | Icon in a tinted tile | `Icon`, `Tint` |
| `Icon` (tag helper) | One `<use>` into the SVG sprite + accessible name | `name`, `title?`, `decorative?` |
| `ProductName` (tag helper) | Resolve the *single* product-name value (C1) | `for` (subcontractor \| main) |

### Forms & conversion

| Component | Single responsibility | Key parameters |
|---|---|---|
| `CtaButton` | One button, all variants | `Variant` (primary/secondary/ghost), `Href`, `Label`, `Icon?` |
| `LeadCaptureForm` | Generic lead form | `ConversionType`, `Fields`, `PrivacyNotice` |
| `NewsletterSignupForm` | Double-opt-in email subscribe | `Source`, `PrivacyNotice` |
| `DemoRequestForm` | Book-a-demo capture | `Fields`, `Qualifiers` |
| `ContactForm` | General contact | `Fields` |
| `FormField` | Labelled field wrapper (label, hint, error slot) | `For`, `Label`, `Hint?` |
| `InlineValidationMessage` | One field's validation message | `For` |
| `FormSuccessPanel` | In-page success state (paired with a thank-you page) | `Heading`, `NextStep` |

### Feedback & state

| Component | Single responsibility | Key parameters |
|---|---|---|
| `EmptyState` | Nothing-to-show state (e.g. filtered features) | `Icon`, `Title`, `Body`, `Cta?` |
| `BannerAlert` | Page-level notice (e.g. pricing caveat) | `Severity`, `Message` |
| `ToastNotification` | Transient confirmation (progressive-enhancement only) | `Message`, `Severity` |

---

## 7. Page & section inventory

Every page ends with a CTA (§8). For each page: **title pattern**, **meta description approach**,
**OG/Twitter card**, **canonical**, **primary goal**. Title pattern site-wide:
`{Page} — Tedwren` (home: `Tedwren — {value-proposition}`). Meta descriptions are per-page,
≤155 chars, benefit-led (§9 copy standard). OG/Twitter: `summary_large_image` with a per-page
OG image (no product name — C1). Canonical: self-referential absolute URL on every page.

### Home — *goal: self-identify + primary CTA*

| # | Section | Components | Job | CTA |
|---|---|---|---|---|
| 1 | Hero | `HeroBanner` | Name the *problem*, not the category; dual CTA; trust strip (UK data, no app install) | Start a pilot / Book a demo |
| 2 | Problem statement | `SectionHeader`, `SplitFeature` | Both sides pay for the same duplicated work | — |
| 3 | Two products, "Which are you?" | `ProductCard` ×2 | Route the visitor to their product within one screen | Explore each product |
| 4 | Five differentiators | `FeatureCard` ×5 / `IconTile` | The five things the site exists to say (order per brief §3) — **#3 scoped main-contractor (F2)** | — |
| 5 | How it works (3 steps) | `TimelineList` | Numbered because it *is* a sequence | — |
| 6 | Dispersed schemes | `SplitFeature`, `StatBand` | The one place we are the *only* credible option (SF-25 / MC-28) | Book a demo |
| 7 | Social proof / pilot | `TestimonialCard`, `LogoStrip` | Credibility; pilot programme | Start a pilot |
| 8 | Outcome stats | `StatBand` | Only figures we can stand behind — **no borrowed competitor stats** | — |
| 9 | FAQ teaser | `FaqAccordion` (subset) | Remove top objections | See all FAQs |
| 10 | Closing CTA | `CtaBand` | Convert | Start a pilot / Get pricing |

### About — *goal: trust → contact*
Who Tedwren is · the founding insight (both sides of the market pay for the same duplicated work) ·
the approach · **UK data residency & data-protection posture (R13)** · careers/contact. Components:
`HeroBanner` (compact), `SectionHeader`, `SplitFeature`, `StatBand`, `CtaBand`.

### Products — index + two detail pages — *goal: product fit → demo/pilot*
- **Index:** frames the two products side by side (`ProductCard` ×2, `ComparisonTable`), self-ID prompt, CTA.
- **Detail (each):** hero · who it's for · the promise · capability sections (`FeatureCard` +
  `StatusBadge`, C2) · an explicit **"what it does not do"** honesty section · outcomes · CTA.
- **Subcontractor detail respects F2/C3 throughout**: no "permitted"/"denied", no site-access-decision
  framing; language is *recorded* / *site-ready*. The main-contractor detail owns the
  reconstructable-decision story (R10).

### Features — *goal: capability confidence → demo*
Filterable/grouped index across both products, **grouped by job-to-be-done, not PRD ID**. Every item
carries its availability state (C2), Available and On-the-roadmap **visually distinct, never mixed
silently**. Components: `SectionHeader`, filter controls (switches), `FeatureCard`, `StatusBadge`,
`EmptyState`, `CtaBand`.

### Pricing — *goal: qualify → get pricing / pilot*
Meter explanation (operative band vs active site, via `PricingToggle`) · the bands (`PricingTable`)
· what is / isn't included · **the dispersed-scheme statement — "a scheme of many properties is one
site"** · pilot offer · a billing-questions FAQ block · the **"indicative, will be confirmed" caveat**
(`BannerAlert`). **All numbers from configuration (C4).** No roadmap item may appear as an included
line (C2 — enforced by test).

### FAQ — *goal: remove objection → CTA*
Grouped, accordion, searchable (client-side filter, progressive-enhancement) · `FAQPage` JSON-LD ·
closing `CtaBand`.

### Plus
Contact / Book a demo · a **thank-you page per conversion type** (real page with a next step, not a
toast) · Privacy notice · Cookie notice · Terms · Accessibility statement · 404 · 500 · and a
**campaign landing page pattern** for the compliance-pack referral route (SUB-25) with a
campaign-attributable, consent-gated entry point.

---

## 8. CTA & conversion model

**Vocabulary (fixed — nothing else):**

| Tier | CTA | Meaning |
|---|---|---|
| Primary | **Start a pilot** | The paid, discounted pilot with a named champion (PRD 10.2 **q23**) |
| Secondary | **Book a demo** | Human-scheduled call (does **not** link the withdrawn-name demo URL — F4) |
| Tertiary | **Get pricing / Talk to us** | Lower-intent routing |
| Low-commitment | **Get product updates** | Email subscribe (footer, end of page) |

**Placement rule:** one shared `CtaBand`; **no page ends without a CTA.** Wording and placement live
in one component so they cannot drift.

**Minimum viable fields (asking for eight kills a construction lead):**

| Conversion | Fields | Qualifiers (route the lead) |
|---|---|---|
| Start a pilot | Name, work email, company, phone | Which product · approx operative headcount **or** number of sites · runs dispersed schemes? (switch) |
| Book a demo | Name, work email, company | Which product · headcount/sites band |
| Get pricing / contact | Name, work email, message | Which product |
| Get updates | Email only | — (double opt-in) |

**Qualifying fields** — which product, approximate operative headcount **or** number of sites, and
whether they run dispersed schemes — decide routing (and let us qualify out an hours-fraud buyer until
Phase 5).

**Trust, safety & compliance on every form:** DataAnnotations validation with inline messages,
antiforgery token, **honeypot + submission-timing check**, per-IP rate limiting. **Double opt-in** for
the mailing list, a working unsubscribe route, and the **lawful basis + privacy wording sitting next to
the form** (not only in the policy). Success and failure are **real pages** with a next step.

**Explicitly out of scope this phase:** self-serve checkout / payment. Pricing is unsettled and no
billing system exists (PRD 9); "subscribe" means **lead capture and updates, not payment**. *If Stripe
is added later* it brings: a billing provider integration, a plan/price catalogue reconciled with the
pricing config, tax/VAT handling, a customer portal, and webhooks — none of which this phase's
`ISubscriptionService` seam assumes, but all of which it can be replaced by without touching a view.

**Conversion tracking** is consent-gated and works **without third-party cookies** (first-party,
privacy-first — §9). Campaign attribution for SUB-25 rides on first-party UTM capture stored with the
lead, not on a third-party pixel.

---

## 9. Cross-cutting requirements

**Accessibility (WCAG 2.2 AA target).** The §5.4 contrast rule is binding: brand orange is
large-text/UI only; body/link brand text uses `#B8430C`. Visible keyboard focus, logical heading order
(one `h1` per page), skip link, accessible names on all icon-only controls, keyboard-operable
accordions/menus, no colour-only meaning, `prefers-reduced-motion` respected. **Automated axe check in
CI** (Playwright) **plus a manual keyboard + screen-reader pass** before launch.

**Responsive (mobile-first).** Breakpoints: `≤640` (mobile), `641–1024` (tablet), `≥1025` (desktop).
Reflow: hero stacks (media below copy on mobile); card grids **3→2→1**; nav → `MobileNavDrawer`;
`ComparisonTable` → stacked cards; `PricingTable` → horizontally scrollable **or** stacked cards.

**Performance (Core Web Vitals budget).** Targets: **LCP < 2.0s**, **CLS < 0.05**, **INP < 200ms**;
**page-weight ceiling ≤ 500KB** on the home route (excluding below-fold lazy imagery). Self-hosted
fonts (`font-display: swap`, subset, preload), a critical-CSS approach, no render-blocking third-party
scripts, response compression (brotli), long-lived cache headers with **asset fingerprinting**.

**SEO.** Semantic HTML, one `h1` per page, `sitemap.xml`, `robots.txt`, canonical URLs, structured
data (**Organization**, **WebSite**, **SoftwareApplication/Product**, **FAQPage**, **BreadcrumbList**).
Keyword/intent map targets the categories buyers actually search — **"time and attendance"** and
**"workforce management"** — plus **retrofit / dispersed-scheme / no-compound sign-in** terms where we
have no competition. Structured data must carry **no product name (C1)** and **no unavailable
capability (C2)**.

**Analytics & consent (UK GDPR).** A consent banner with a **genuine reject**, **no non-essential tags
before consent**, and a privacy-first analytics recommendation (e.g. a first-party, cookieless
provider). **This machinery is scoped so it can never load on the compliance-pack surface (C8)** — see
the domain/path-separation assumption in §13; the scoping holds under either model.

**Security.** HTTPS with **HSTS**, a **strict CSP** (inline styles/scripts avoided or **nonced** — the
minimal ES modules and any inline JSON-LD carry a per-response nonce), the standard security-header set,
**no secrets in configuration files**, anti-automation on every public form (§8).

**Content governance — how C1–C7 are enforced (mechanisms, not notes):**

| Constraint | Mechanism |
|---|---|
| **C1** product names | One `ProductNamingOptions` + one `<product-name for="…">` tag helper; **no name literal** in markup, routes, image filenames, titles, meta or JSON-LD. **CI guard test** scans Views/Content/wwwroot/routes for a denylist (`INDUCTED`, `PERMITTD`, configured names) and **fails the build** on a hit. Covers F4 (withdrawn-name URL). |
| **C2** roadmap vs available | `FeatureAvailability { Available, Roadmap }` on the content model + `StatusBadge`; **test** asserts no `Roadmap` item appears in any Pricing inclusion. |
| **C3 / F2** subcontractor language | Site-entry-decision / "permitted" / "denied" language is main-contractor-only; content-review checklist item + a lint of subcontractor content for the forbidden words. |
| **C4** pricing | Bands/currency/"from"/caveat entirely in `pricing.json`; a test asserts no price literal in any `.cshtml`. |
| **C5 / F3** regulation | Copy standard: **lead with time saved, fewer blocked workers, faster site starts, cards that don't silently expire, proof available immediately.** Building Safety Act record-keeping applies **principally to higher-risk buildings in England**; CDM 2015 and CSCS described accurately; **CSCS never described as legally mandated (F3)**. Regulation reinforces urgency; it is not the headline. |
| **C6** biometrics | No claim, imagery or comparison line implying face match / verified identity / buddy-punching prevention (all Phase 5). |
| **C7** UK residency / no app install | Built as the two trust pillars (R13, R1). |

**Copy ownership:** a single named owner (Leigh / Tedwren Ltd) signs off all customer-facing copy
against C1–C7 before publication; the checklist lives in the PR template for content changes.

---

## 10. Deliverables

1. `Tedwren.Web` MVC project building and running in `Tedwren.sln`, with the §4 structure.
2. `Tedwren.Brand` shared-token RCL — working and documented; the Client migrated onto it.
3. Component library + `docs/component-catalogue-web.md` (props table, usage snippet, screenshot each).
4. Every §7 page implemented and reachable, populated with **real approved copy** (placeholders
   clearly marked where copy is pending).
5. Lead capture + email subscribe working end-to-end against the interface implementations, with
   thank-you pages.
6. SEO / accessibility / performance / consent checklists completed and **evidenced** (CI axe report,
   Lighthouse/CWV run, contrast table, consent-reject verification).
7. `README.md` (in `src/Tedwren.Web`) covering: run, add a page, add a component, where content lives,
   and the C1–C7 copy constraints.

---

## 11. Phased plan

Each phase is independently testable and must not break existing functionality or an earlier phase.

> **Phase W1 — Foundations** *(scaffold + shared brand)*
> - Add `Tedwren.Brand` (move `tokens.css` in; repoint the Client's `<link>`), `Tedwren.Web` and
>   `Tedwren.Web.Tests` to `Tedwren.sln`. Layout, `SiteHeader`, `SiteFooter`, `SkipLink`, the type
>   scale + new tokens in `site.css`, the SVG icon sprite + `Icon` tag helper, `ProductName` tag
>   helper + `ProductNamingOptions`.
> - **Testable:** solution builds clean (no new warnings); the console renders **unchanged** on the
>   relocated token file; `Tedwren.Web` serves a themed layout at `/`; the C1 name-guard test runs and
>   passes; axe check wired in CI.

> **Phase W2 — Home** *(the reference implementation)*
> - Every home-page section (§7) with its components, driven by `Content/*.json` via `IContentProvider`.
> - **Testable:** `/` renders all ten sections from config; no colour/price/name literal in markup
>   (guard tests green); Lighthouse a11y ≥ 95 and CWV within the §9 budget on `/`.

> **Phase W3 — Content pages** *(Products index + two details, Features, About)*
> - Products index + two detail pages (subcontractor detail enforces F2/C3); Features index with
>   availability filtering; About.
> - **Testable:** route/render smoke test covers every new route; the C2 test proves no `Roadmap`
>   feature is presented as Available in pricing; the subcontractor-language lint passes.

> **Phase W4 — Conversion** *(Pricing, FAQ, forms, thank-you, lead capture)*
> - Pricing (all from `pricing.json`, `PricingToggle`), FAQ (+ `FAQPage` JSON-LD), all four forms,
>   thank-you pages, `ILeadCaptureService` + `ISubscriptionService` (logging/in-memory), double opt-in.
> - **Testable:** each form validates, rejects the honeypot/timing bot, writes a lead via the interface,
>   and lands on its thank-you page; the C4 test proves no price literal in any view.

> **Phase W5 — Compliance & polish** *(a11y, perf, SEO, consent, legal, errors, catalogue)*
> - Manual keyboard + screen-reader pass; CWV tuning; `sitemap.xml`/`robots.txt`/canonicals/structured
>   data; consent banner with genuine reject scoped off the pack surface (C8); legal + accessibility
>   pages; 404/500; `component-catalogue-web.md` finalised.
> - **Testable:** CI axe report clean; consent-reject verified to load zero non-essential tags; every
>   §10 checklist evidenced; the site is fully navigable with JavaScript disabled.

---

## 12. Out of scope (this phase)

Authentication and customer accounts; self-serve sign-up or payment; a CMS or blog engine (**seam
only**); localisation; dark mode (not blocked — F6); the customer application itself; live product data
of any kind; named-customer case studies until pilot case-study rights exist (PRD 10.2 q23); named
competitor comparison content.

---

## 13. Open questions for sign-off

1. **Missing reference doc (F1).** Confirm it is acceptable to reconstruct the "v1" house style from
   `tokens.css` + `component-catalogue.md` + `plan-and-scope.md`, since
   `Tedwren-UIUX-Plan-and-Scope-of-Works-v1.md` is not in the repo.
2. **Domain/path separation for the pack surface (C8, F5).** Is the compliance-pack link served from a
   **separate domain** (e.g. `packs.…`) or a **path** on the marketing domain? The consent scoping is
   built to hold either way, but the deployment/CSP config differs.
3. **Hosting region/provider (R13).** Which UK-region host? Needed before any form stores personal data.
4. **Product-name resolution at launch.** Until names clear trademark (classes 9/41/42), do the
   customer-visible names resolve to the neutral terms ("the subcontractor product" / "the main
   contractor product"), or to working placeholders? The mechanism (C1) supports either; the value is Leigh's.
5. **Screenshot handover.** Who provides admin-console screenshots scrubbed of product names (C1) and
   unavailable capability (C2)? Until then, `MediaFrame` uses marked placeholders.
6. **Central package management.** Adopt `Directory.Packages.props` when adding the new projects
   (recommended, low cost), or keep inline per-csproj versions to match the current convention?
7. **Privacy-first analytics provider.** Confirm the specific cookieless provider (affects CSP and the
   consent copy).
8. **Testimonials / pilot logos.** None may name a customer until case-study rights exist (q23) — is
   there approved anonymised social proof for launch, or does the pilot/social-proof section ship with
   placeholders?

---

## 14. Risk register

| # | Risk | Likelihood / impact | What reduces it |
|---|---|---|---|
| R-1 | **A withdrawn or unconfirmed product name leaks into markup, a route, an image name or JSON-LD (C1/F4).** Reputational + rework; a rename becomes a find-and-replace. | Med / High | Single `ProductNamingOptions` + one tag helper; **build-failing CI denylist scan** covering Views/Content/wwwroot/routes and the `inducted-mvp.vercel.app` URL; no name literal anywhere. |
| R-2 | **Roadmap capability presented as available (C2) or subcontractor content implying a site-access decision (C3/R18/F2).** Legal/commercial misrepresentation. | Med / High | `FeatureAvailability` model + `StatusBadge`; test that no `Roadmap` item enters pricing inclusions; subcontractor-language lint; single-owner copy sign-off against C1–C7. |
| R-3 | **Token drift between the console and the marketing site.** Two surfaces slowly stop looking like one company. | Low / Med | One physical `tokens.css` in `Tedwren.Brand`, served to both via `_content/…`; marketing site forbidden from redeclaring values; new tokens are additive and site-scoped. |
| R-4 | **Brand orange used as body text, failing WCAG AA (§5.4).** Accessibility non-compliance shipped. | Med / Med | Binding §5.4 rule (brand orange = large/UI only; `#B8430C` for text); CI axe check + manual pass gate; contrast table in the catalogue. |
```
