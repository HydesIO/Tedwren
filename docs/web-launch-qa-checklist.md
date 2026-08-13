# Tedwren.Web — pre-launch QA checklist (Phase W8)

This tracks the Website Content & Build Spec **§14 pre-launch QA checklist** (mirror:
`docs/Tedwren-Website-Content-Build-Spec-v2.md`) against the built site. It is the "run the full §14
checklist" deliverable for Phase W8 in `docs/Tedwren-Web-Plan-and-Scope-of-Works.md`.

Each item is one of:

- **Enforced** — a mechanism fails the build/CI if the site regresses. The enforcing test is named,
  so a reviewer can see the gate rather than take the checkmark on trust (Plan §8: "built as
  mechanisms, not notes"). These do not need a human pass before every deploy.
- **Manual** — needs a tool run or human review that cannot run in this repo's test host (a live
  browser, Lighthouse, a legal reviewer). Owner/《how》 noted. Run before go-live.
- **Sign-off** — a founder decision or real-world fact from `Open items` (Plan §11 / Spec §13), not a
  build task. Blocks launch, not the build.

## §14 checklist status

| # | Spec §14 item | Status | Enforced by / owner |
|---|---|---|---|
| 1 | Every price and product name traces back to a config field (grep hardcoded `£` / product-name strings) | **Enforced** (price) + convention | `ContentLintTests` `hardcoded-price` rule scans content JSON + views; the sole `£` lives in `JsonContentProvider.FormatMoney`. Product names are keyed via `IContentProvider` (W2), never inline. |
| 2 | Cookie banner blocks all non-essential scripts until consent; reject-all works in one click | **Enforced** | `ConsentAndAnalyticsTests` (W6) — banner + one-click reject, no analytics tag pre-consent. |
| 3 | No absolute compliance claims anywhere (`guarantee`, `ensure`, `100%`, `compliant`) | **Enforced** | `ContentLintTests` `absolute-compliance` rule (Plan §8.1). Reviewed exceptions go through the lint allowlist. |
| 4 | No CSCS mention reads as rivalry, replacement, or on-demand verification | **Enforced** | `ContentLintTests` `cscs-positioning` rule — scans body copy **and** titles/meta (Plan §8.2); the understated CSCS add-on line is deliberately not flagged. |
| 5 | Compliance pack viewer has no sign-up wall at any point | **Enforced** | `LaunchGuardrailTests` — pack chrome view carries a UTM-tagged demo CTA and no form/login/sign-up markup (Plan §4.1). |
| 6 | Mobile Core Web Vitals pass on `/worker-passport` and the pack viewer | **Manual** | Lighthouse mobile run against a deployed instance. Can't run in the test host. Owner: web dev pre-launch. |
| 7 | WCAG 2.1 AA automated + manual pass, focused on mobile tap targets and contrast | **Partial → Manual** | Structural baseline **enforced** (one `<h1>`/page, canonical, `lang`, viewport, skip-link present). Full axe scan + manual mobile pass on a real device is **Manual** — owner: web dev pre-launch. |
| 8 | All three primary CTAs route correctly and are tagged for analytics | **Enforced** | `Cta` accepts only the closed `CtaAction` set with fixed hrefs (W1); `LaunchGuardrailTests` asserts the pack CTA carries `utm_source=pack`; conversion events wired in W6. |
| 9 | Legal pages reviewed by whoever handles Tedwren's legal position (not placeholder) | **Sign-off** | Real content present (W5), but legal review is Open item §13.4. Owner: founders/legal. |
| 10 | Footer company number/address matches the current Companies House filing | **Sign-off** | Open item §13 (company number). `CompanyNumber`/`RegisteredOffice` are `null` today and the footer omits them (no placeholder); populate `site.json` from the filing before launch. |
| 11 | Partners form has no self-serve signup; nowhere solicits site-access controllers as affiliates | **Enforced** | `PartnerIntegrationTests` / `PartnerProgrammeUnitTests` — submit creates only a pending record; §7.3 exclusion refuses anyone controlling site access. |

## Supporting SEO/crawler infrastructure (Plan §9/§10)

| Item | Status | Enforced by |
|---|---|---|
| `sitemap.xml` generated from the route list, excluding capability URLs | **Enforced** | `SeoInfrastructureTests` + `SitemapBuilder` unit test. |
| `robots.txt` disallows capability URLs (`/partners/dashboard`, `/r/`) and advertises the sitemap | **Enforced** | `SeoInfrastructureTests`. |
| Canonical URL on every page | **Enforced** | `SeoInfrastructureTests` (`_Layout` emits `rel="canonical"`). |
| Title within ~60 chars; meta description within ~155 chars | **Enforced** | `SeoInfrastructureTests` per-page budget assertions. |

## Open sign-off items still blocking launch (Plan §11 / Spec §13)

Not build tasks — carried here so they are visible at go-live:

1. Worker Passport price **£10/yr (PRD) vs £12/yr (GTM)** — pick one (`pricing.json` seeds £10, the PRD value).
2. Publish exact pricing bands vs. "from £x, talk to us".
3. Security & Trust real claims (hosting region, ICO number, certifications).
4. Worker Passport consumer-contract terms — legal sign-off.
5. Final product names (trademark/domain).
6. Which social accounts exist at launch (footer icons).
7. Partner programme public at launch vs. unlinked application page.
8. Partner-application vetting process + owner.
