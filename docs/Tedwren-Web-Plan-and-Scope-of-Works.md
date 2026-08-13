# Tedwren.Web — Plan of Action & Scope of Works (v2)

> **Status:** Planning only. This document proposes the build; no application code, Razor, CSS or
> configuration is written in this pass.
>
> **Source of truth for this website:** `docs/Tedwren-Website-Content-Build-Spec-v2.docx` (mirror:
> `docs/Tedwren-Website-Content-Build-Spec-v2.md`) — the *Website Content & Build Specification v1.0*.
> This plan supersedes `Tedwren-Web-Plan-and-Scope-of-Works-v1.md`, which was built against the earlier
> content draft. Section references below in the form **(Spec §n)** point at that specification.
>
> **Product source of truth:** `docs/TedwrenPRDv6_4.docx` (mirror `.md`) remains authoritative for
> product behaviour, requirement IDs (SF/SUB/MC) and rules (R1–R18). Where the marketing spec and the
> PRD disagree, the PRD wins and the discrepancy is raised, not worked around (Spec §13 already lists
> the known conflicts — e.g. the Worker Passport £10 vs £12 price).

---

## 1. Objective

Build the public marketing website described in the Content & Build Spec, on **ASP.NET Core MVC /
.NET 10**, server-rendered, with a content layer that is **config-driven, not hardcoded**, so the
founders can change product names, prices and copy without a redeploy (Spec §2, §12.1).

The site has four jobs (Spec §3): make each buyer recognise their problem, explain the product in
~30 seconds, build enough trust to justify a demo, and seed organic/referral acquisition. It sells
**outcomes, not features**, and splits the visitor by audience — subcontractor, main contractor,
worker — within the first screen.

The critical distribution mechanism is the **compliance pack viewer** (Spec §4.1): a subcontractor
sends a pack, a main contractor opens it with no account, and a light UTM-tagged footer routes them
to a demo. That page is served by the product but must reuse this site's brand chrome.

---

## 2. Technology choice vs. the spec's recommendation

The spec (§12.1) *recommends* a headless-CMS + Next.js stack. **We are deliberately building on
ASP.NET Core MVC / .NET 10 instead**, per the direction for this repository. This is a conscious
deviation, and the plan carries every reason the spec gives for wanting a CMS and satisfies each one
on the MVC stack:

| Why the spec wanted a CMS (§12.1) | How this MVC plan satisfies it |
|---|---|
| Product names, prices, feature copy are unstable and must change without a deploy | A structured **content layer** (`IContentProvider`) backed by editable JSON content files (`nvarchar(max)`/`jsonb`-ready), hot-reloadable; a small headless CMS can be slotted behind the same interface later with no view changes. |
| One feature-card source rendered both short-form (Home) and long-form (dedicated page) | One strongly-typed `ProductProfile` content entry, rendered by two parameterised View Components (`ProductCard` vs `ProductDetail`) — single source, no copy fork (Spec §6.1, §12.2). |
| Future growth (case studies, resources) without a rebuild | Content model is open/extensible; new content types are added as records + a View Component, not a re-architecture. |

**Stack summary**

| Concern | Decision |
|---|---|
| Framework | **.NET 10**, `Microsoft.NET.Sdk.Web`. Matches every existing project's `net10.0` target. |
| Web stack | **ASP.NET Core MVC**, server-rendered Razor. Controllers + view models; one controller per content area. |
| Components | **Parameterised View Components + Tag Helpers**, single responsibility, catalogued (Spec §5). No page-local duplicated markup — reuse-first, matching the repo's MudBlazor-component discipline in spirit. |
| Content layer | `IContentProvider` seam (Abstractions) → JSON-backed provider now, CMS-backed provider possible later. All product names, prices, copy come from here (Spec §2, §12.2). |
| Styling | Shared brand tokens (`tokens.css`) + `site.css` + per-component scoped CSS. **No colour/price literals outside the token/content sources.** No CSS framework. |
| JavaScript | Minimal, progressive-enhancement, vanilla ES modules. Site fully usable with JS off. No SPA framework. |
| Forms | MVC model binding + DataAnnotations, antiforgery, honeypot + timing check, rate limiting. |
| Project | `Tedwren.Web` (+ `Tedwren.Web.Tests`), added to `Tedwren.sln`. Reuses `Tedwren.Abstractions` DTOs/interfaces where they already fit. |

---

## 3. Content model (Spec §12.2) — the config source of truth

Strongly-typed content types, bound from JSON via `IContentProvider`. Nothing customer-visible that
could change is typed into a `.cshtml` file.

| Content type | Fields (indicative) | Rendered where |
|---|---|---|
| **`ProductProfile`** | `configKey`, `displayName`, `slug`, tagline, hero copy, feature list (`FeatureCard[]`), pricing reference | Home card (short) + dedicated page (long) |
| **`PricingPlan`** | plan name, band description, annual price, monthly price, product reference | `/pricing` + any "from £x" mention elsewhere |
| **`FeatureCard`** | heading, copy | Product pages, Home |
| **`TrustPoint`** | claim, optional logo | Trust strip (§5.3), `/security` |
| **`FaqItem`** | question, short answer, link target | `/faq`, FAQPage schema |
| **`Testimonial`** | quote, author, company, logo | **Schema built, empty at launch** (Spec §9, §12.2) |
| **`SiteConfig`** | product name keys, legal entity (Companies House number + registered office), social links present at launch, prices | Footer, titles, everywhere |

**Naming rule (Spec §2):** every product name is referenced by key (`Products:Subcontractor:Name`),
never inline. URL slugs are audience/function based (`/subcontractors`, `/main-contractors`,
`/worker-passport`) so a rename is a content edit + redirect map, not a redeploy. Legal entity strings
use "Tedwren Ltd", pulled from config sourced from the Companies House filing (Spec §5.2, §12.6), not
typed by hand.

---

## 4. Sitemap & routing (Spec §4, §6)

Launch scope is deliberately small (Spec §4). Audience/function slugs, one controller per area.

| Page | Route | Controller/action | Primary CTA |
|---|---|---|---|
| Home | `/` | `HomeController.Index` | "I'm a: Subcontractor / Main Contractor" audience split |
| Subcontractors | `/subcontractors` | `ProductsController.Subcontractor` | Book a demo |
| Main Contractors | `/main-contractors` | `ProductsController.MainContractor` | Book a demo |
| Worker Passport | `/worker-passport` | `WorkerPassportController.Index` | Get your Worker Passport |
| Pricing | `/pricing` | `PricingController.Index` | Book a demo |
| Security & Trust | `/security` | `TrustController.Index` | Talk to us |
| About | `/about` | `AboutController.Index` | Book a demo |
| FAQ | `/faq` | `FaqController.Index` | (deflects to relevant page) |
| Book a Demo / Pilot | `/demo` | `LeadController.Demo` | Submit form |
| Partners / Affiliates | `/partners` | `PartnersController.Index` | Apply to become a partner |
| Contact | `/contact` | `LeadController.Contact` | Send message |
| Legal (×4) | `/legal/{privacy\|cookies\|terms\|data-protection}` | `LegalController` | — |
| 404 | fallback | error handler | Return home / Book a demo |

**Compliance pack viewer** (`/pack/{id}`) is **served by the product, not this site** (Spec §4.1). It
is out of `Tedwren.Web`'s scope to *own*, but in scope to **supply the shared header-light/footer-light
components and brand tokens** so the product page can render them. The "Book a demo" link in the pack
chrome carries a `utm_source=pack` tag so GTM Stage 2 is measurable. **No sign-up wall, ever** — a hard
rule, tracked in the QA checklist.

---

## 5. Global components (Spec §5) — build once, reference everywhere

Each is a parameterised View Component (or Tag Helper) with a single responsibility, catalogued in a
`docs/web-component-catalogue.md` created during Phase W2.

- **`SiteHeader`** — logo → home; primary nav (For Subcontractors · For Main Contractors · Worker
  Passport · Pricing · About); persistent top-right CTA "Book a demo", which swaps to "Get your Worker
  Passport" on the Worker Passport page only. Mobile: hamburger, **CTA stays outside the collapsed
  menu** (never two taps).
- **`SiteFooter`** — company (Tedwren Ltd, registered office + number from config, Spec §12.6); site
  links (nav + Security & Trust, FAQ, Partners, Contact); legal (Privacy, Cookies, Terms, Data
  Protection); **social icons only for accounts that exist at launch** (config-gated).
- **`TrustStrip`** (Spec §5.3) — one component, referenced on Home, Subcontractor, Main Contractor,
  and expanded on `/security`. Not copy-pasted three times.
- **`Cta`** — exactly three canonical actions (Spec §5.4): *Book a demo*, *Start a pilot*, *Get your
  Worker Passport*. Button copy is the literal next action — **no "Learn More / Get Started / Discover /
  Explore"** anywhere. Enforced by the Cta component only accepting known CTA keys.
- **`ConsentBanner`** (Spec §5.5, §12.7) — CMP-backed; **one-click reject-non-essential** with equal
  weight to Accept; **no analytics/ad/heatmap script fires before consent**. See §8.
- **`ProductCard`** / **`ProductDetail`** — the short/long renderers over one `ProductProfile`.
- **`FeatureGrid`, `Differentiators`, `HowItWorks`, `PricingTable`, `FaqAccordion`, `LeadForm`,
  `TestimonialWall`** (empty at launch).

---

## 6. Page content mapping (Spec §6)

All draft copy in Spec §6 lands in content entries, not views. Notable page-specific requirements:

- **Home (§6.1):** hero, problem section, the two product cards (rendered short-form from the same
  entry as the dedicated pages — no copy fork), four differentiator cards with **"Works beyond the site
  gate"** given a distinct visual treatment (strongest differentiator), five-step "How it works", trust
  strip + closing CTA.
- **Subcontractors (§6.2):** treated as the **primary launch landing page** (paid/referral traffic
  lands here, not Home). Must include the **"company documents"** feature (Spec §6.2 dev-note: a real
  gap in the earlier draft). Understated CSCS add-on line. Closing CTA + pilot secondary line.
- **Main Contractors (§6.3):** the pre-arrival induction proposition, feature grid, and a
  **substantial, distinct Retrofit / dispersed-site section** ("Workforce management when there isn't a
  site gate") — not a footnote.
- **Worker Passport (§6.4):** different register (individual buyer). Price line from config
  (**£10 vs £12 conflict — Spec §13 item 1, do not launch two prices**). Add the **"never locked out
  for non-payment"** benefit (PRD Rule W2). Consumer checkout expectations set on the page; the flow
  itself is product, but the page must state W7 informed-consent + UK 14-day cancellation facts. **Hard
  CSCS positioning restriction** applies to copy *and* meta/title tags (Spec §8.2).
- **Pricing (§6.5):** **every number from config** (Spec §6.5 dev-note). Subcontractor per-operative
  bands, main-contractor per-active-site, Worker Passport, pilot, and the plain-language clarifiers
  (active operative/site, 10% buffer overage). "Sites are free to record" and "a dispersed scheme = one
  site" stated as explicit trust points.
- **Security & Trust (§6.6):** own stable URL for procurement/DPO. Only claims we can make now
  (Spec §6.6 table); **no fabricated ISO/Cyber Essentials badges**; no absolute-compliance language.
- **About (§6.7), FAQ (§6.8):** founder-led credibility; FAQ drafted from the Spec §6.8 table + FAQPage
  schema.
- **Demo/Contact (§6.9):** two separate lightweight forms routed differently (see §7). Demo uses a real
  calendar-booking integration after submit, not "we'll be in touch".
- **Partners (§6.10 + §7):** application page, **not** self-serve signup — see §7 below.

---

## 7. Lead capture & the partner programme (Spec §6.9, §7)

**Forms.** `LeadForm` component, server-validated, antiforgery + honeypot + timing + rate limit.

| Form | Fields | Routing |
|---|---|---|
| Book a demo (`/demo`) | name, work email, company, role, company type, headcount/sites, phone (opt), "Interested in a pilot" checkbox | CRM lead → sales-qualified queue; auto-confirmation email with calendar link |
| Contact (`/contact`) | name, email, message, reason (General/Press/Partner/Support) | routed by reason to the right inbox; no calendar link |
| Partner application (`/partners`) | who they are, how they work with subcontractors, relationship to any site/contractor | **creates a pending-review record, not an active account** |

**Partner programme is approval-gated (Spec §7.2), never a site-access channel (Spec §7.3 — a hard
legal/ethical rule).** Build requirements carried into the plan:

- Application form → **pending-review record**; referral links and dashboard activate only after a
  **human approval step**. No "approve on signup" wiring (Spec §7.2 dev-note).
- The form must **not** target or accept anyone who controls/influences site access (site managers,
  gate/induction staff). Stated plainly on the page; the relationship question exists to catch the §7.3
  conflict before approval.
- Unique trackable referral code per approved partner; attribution persists through demo booking /
  Worker Passport checkout (credit even if not same-session).
- Commission model: 20% of first-year subcontractor revenue, paid after funds clear, **90-day
  clawback** — clawback/reversal must be modelled against a specific referral record **from the start**.
- Referral events logged to the same analytics/CRM stack, **tagged distinctly** from organic/paid.
- A simple partner dashboard (referrals sent, status, commission paid/pending) — not public/self-serve.

Given the modelling depth here (attribution, clawback, dashboard), the partner programme is **phased
last** (Phase W7) and the page can launch as an unlinked application page if the founders defer public
recruitment (Spec §13 items 7–8).

---

## 8. Compliance, copy & consent guardrails — built as mechanisms, not notes

These are commercial/legal/safety constraints (Spec §8, §11), so the plan makes them hard to breach:

- **No absolute compliance claims (§8.1):** a build-time content lint fails the build on
  `guarantee`, `ensure`, `100% compliant`, `fully compliant` (reviewed allowlist) across content files
  and views. Mirrors the QA checklist grep (Spec §14).
- **CSCS constraint (§8.2):** the lint also scans **titles and meta descriptions**, not just body
  copy, for CSCS rivalry/replacement/"on-demand verification" phrasing. Worker Passport is never
  positioned against My CSCS / the Digital Skills Passport.
- **No fabricated social proof (§8.3, §9):** `Testimonial`/logo components ship **empty**, schema
  ready; there is no path to render placeholder logos or review stars.
- **Prices from config (§6.5, §14):** no `£` literal in any view or content string outside
  `PricingPlan` fields; enforced by the same lint (grep for hardcoded `£`).
- **Consent before scripts (§5.5, §11):** a proper CMP; no GA4/ad/heatmap tag fires pre-consent;
  one-click reject-non-essential. Cross-site ad pixels sit behind a separate consent category from
  analytics.
- **UTM discipline (§4.1, §11):** the pack footer "Book a demo" and all sales/referral outbound links
  carry UTMs so pack-driven bookings are attributable (the whole point of GTM Stage 2).
- **Conversion events from day one (§11):** demo submit, pilot checkbox ticked, Worker Passport
  checkout started/completed, pricing→demo click-through.

---

## 9. Non-functional requirements (Spec §12)

- **Accessibility — WCAG 2.1 AA baseline (§12.3), non-negotiable.** Tested on the real mobile
  breakpoint (contrast, focus states, tap-target sizing), not just desktop — the worker audience uses
  a basic phone browser outdoors.
- **Performance (§12.4):** Core Web Vitals "good" on **mobile specifically** for `/worker-passport`
  and the pack-viewer chrome. Responsive/optimised images; no render-blocking third-party scripts above
  the fold.
- **Responsive (§12.5):** mobile-first for `/worker-passport` and pack chrome; standard breakpoints
  (~360–480 / 768 / 1024+) elsewhere.
- **SEO/metadata (§10):** unique audience-led title tags (<60 chars) and outcome-led meta descriptions
  (<155); one H1/page; Organization schema sitewide, SoftwareApplication/Product on `/pricing`,
  FAQPage on `/faq`; canonical URLs; per-core-page OG images; **`sitemap.xml` + `robots.txt` generated
  from the route list**, not hand-maintained.
- **Payments (§12.8):** Worker Passport checkout is product-owned (Stripe or equivalent, annual
  billing); this site links into it and states the W7 consent + UK distance-selling facts on the page.

---

## 10. Delivery phases

Numbering continues the repo's convention (backend work is at Phase 25). Each phase is independently
testable and must not break existing functionality.

| Phase | Scope | Exit criteria |
|---|---|---|
| **W1 — Skeleton** | `Tedwren.Web` project + `Tedwren.Web.Tests`, added to `Tedwren.sln`; layout, `tokens.css` shared from the client, `SiteHeader`/`SiteFooter`/`Cta` components; routing for all §4 pages returning stub views. | Solution builds; every route resolves; header/footer render from config. |
| **W2 — Content layer** | `IContentProvider` (Abstractions) + JSON-backed provider; `ProductProfile`/`PricingPlan`/`FeatureCard`/`TrustPoint`/`FaqItem`/`Testimonial`/`SiteConfig` types; component catalogue doc. | Content resolves by key; unit tests cover provider + naming/price key indirection. |
| **W3 — Core pages** | Home, Subcontractors, Main Contractors from content (one product entry → short/long); `TrustStrip`, `Differentiators`, `HowItWorks`, `FeatureGrid`. | Copy matches Spec §6.1–6.3; no forked product copy; "company documents" + retrofit section present. |
| **W4 — Worker Passport & Pricing** | `/worker-passport` (register, W2 benefit, CSCS restriction in meta), `/pricing` (all numbers from config, clarifiers). | Single price source; CSCS meta lint passes; price-conflict flagged for sign-off (Spec §13.1). |
| **W5 — Trust, About, FAQ, Legal** | `/security`, `/about`, `/faq` (+FAQPage schema), 4 legal pages as content (not placeholder). | Only makeable claims present; SEO schema validates. |
| **W6 — Lead capture & consent** | Demo + Contact forms with routing + calendar integration; `ConsentBanner`/CMP; GA4 behind consent; conversion events; UTM plumbing incl. pack footer components. | Forms route correctly + tagged; no script fires pre-consent; reject-all one click. |
| **W7 — Partners programme** | `/partners` page + approval-gated application (pending record), referral attribution + clawback model, simple partner dashboard. | No self-serve activation; §7.3 exclusion enforced; clawback reversible against a referral. |
| **W8 — Hardening & pre-launch QA** | Content lint (compliance/CSCS/`£`), a11y automated+manual mobile pass, Core Web Vitals, run the full Spec §14 checklist. | Every §14 checklist item passes; whole solution builds + tests green. |

---

## 11. Open items carried from the spec (Spec §13) — block sign-off, not the build

These are surfaced here so they are tracked in `TODO.md`, not silently resolved:

1. **Worker Passport price £10/yr (PRD) vs £12/yr (GTM doc)** — pick one before the page goes live.
2. Publish exact pricing bands vs. "from £x, talk to us".
3. Final Security & Trust claims (hosting region, ICO number, certifications) — real answers, not
   placeholders.
4. Worker Passport consumer-contract terms (cancellation, distance-selling) — legal sign-off.
5. Final product names for the two products + Worker Passport (names blocked on trademark/domain, not
   on the build — Spec §2).
6. Which social accounts exist at launch (footer icons).
7. Whether the partner programme goes public at launch or stays an unlinked application page.
8. Vetting process + owner for partner applications.

---

## 12. Out of scope this pass

No SEO content farm, resources hub, or case studies (Spec §4); no CMS integration (the
`IContentProvider` seam is built ready for one); the Worker Passport payment flow and the compliance
pack viewer themselves (product-owned — this site supplies shared chrome/tokens only).
