# TODO — Tedwren development checklist

Working checklist for delivery. **Source of truth is `docs/TedwrenPRDv6_4.docx` (PRD v6.4)**;
delivery sequence and phase definitions are in `docs/plan-and-scope.md`. Update this file whenever
work is started, completed, deferred or newly identified. Completed items note what changed and
the phase/area. PRD requirement/rule IDs (SF-/SUB-/MC-/R-) are referenced, not reproduced.

Legend: ✅ complete · 🔄 in progress · ⏳ planned · ⏸️ deferred · ❗ outstanding/known issue

---

## Completed

### Admin — Affiliates, payouts & e-sign agreements (Phase 3, this change)
- ✅ **Affiliate slice (commercial DB).** `Affiliate` (embedded commission plan), `AffiliatePayout`,
  `AffiliateAgreement` entities + enums, DTOs, `IAffiliateService`/`AffiliateService`, `IAffiliateRepository`
  (Dapper dual-engine + in-memory), scripts **`028_affiliates.sql`**, **`029_affiliate_payouts.sql`**,
  **`030_affiliate_agreements.sql`**. No raw bank details — only a payee reference.
- ✅ **Profit-after-margin commission.** `Affiliate.CommissionOn(revenue) = revenue × ProfitMarginPct ×
  AffiliateRatePct` (e.g. £15,000 × 33% × 20% = £990). Associated accounts are the affiliate's attributed
  converted leads, each showing its computed commission. Payouts recorded as amount + status (raise → mark paid).
- ✅ **Agreement e-sign + PDF.** `AffiliateAgreementTemplate` builds the clauses once → HTML (web) + PDF
  (`AffiliateAgreementPdfRenderer`, QuestPDF, both-party signature block; render falls back to the typed name
  if the signature image is invalid). Public token-gated Blazor page `/affiliate-agreement/{token}`
  (`RecipientLayout` + `TedwrenSignaturePad`) → sign → download countersigned PDF. Countersignatory James
  Darby, Director.
- ✅ **Endpoints.** `PlatformAdmin` affiliate CRUD + payouts; anonymous `GET/POST/GET .../affiliate-agreements/{token}[/sign|/pdf]`.
  Client `ApiAffiliateService`. Lead↔affiliate link: `POST /api/leads/{id}/affiliate` + assignment menu on the
  lead detail page.
- ✅ **Emails.** On create, a branded setup email with the terms + signing link; on signing, a confirmation
  email with the countersigned PDF attached (`SendHtmlWithAttachmentsAsync`).
- ✅ **Admin UI.** `/admin/affiliates` list + Add dialog (commission plan with a live worked example);
  `/admin/affiliates/{id}` detail with **Associated accounts / Payouts / Agreement** tabs. Nav item added.
- ✅ Tests: `AffiliateServiceTests` (commission math, create→agreement+email, sign→PDF+activate+attachment,
  associated-account commission, payouts) + `AffiliateApiTests` (admin flow, anonymous view/sign/pdf, 404).
  Whole solution builds (0 code warnings); all tests pass (LocalDB skipped).
- ❗ **Raised discrepancy (commission model).** This profit-after-margin model **supersedes** the earlier
  website-spec rule (`Tedwren-Website-Content-Build-Spec-v2.md` §7: "20% of first-year subcontractor
  *revenue*", 90-day clawback), confirmed with the product owner. The `Tedwren.Web.Partners`
  `ReferralService` still implements the old revenue-based rule for the marketing site; reconcile the two
  models (and whether the 90-day clawback applies here) in a future revision.

### Admin — Lead management (Phase 2, previous change)
- ✅ **Lead pipeline slice (commercial DB).** `Lead` + `LeadNote` entities, `LeadModel`/`LeadStatus` enums,
  DTOs, `ILeadService`/`LeadService`, `ILeadRepository` (Dapper dual-engine + in-memory), scripts
  **`026_leads.sql`** + **`027_lead_notes.sql`**. Estimated revenue is admin-entered (pricing is
  configuration, not hard-coded — PRD §9). Account/affiliate links are soft `Guid` references (no cross-DB FK).
- ✅ **Pipeline rules.** Statuses New → Contacted → Qualified → Proposal → Converted/Lost; every status change
  writes an automatic activity note; convert-to-account sets Converted + links the account id; anonymous
  capture deduplicates against an open lead (company + email).
- ✅ **Endpoints.** `PlatformAdmin` CRUD + `/{id}/status`, `/{id}/notes`, `/{id}/convert`; anonymous
  `POST /api/leads/capture` for marketing-site inbound. Client `ApiLeadService`.
- ✅ **Admin UI.** `/admin/leads` responsive **card grid** (status chips, model, sites, revenue, owner) +
  Add-lead dialog; `/admin/leads/{id}` detail with overview + **activity/notes tab** (`ActivityFeed`), status
  menu and confirm-gated convert. Nav item added.
- ✅ **Marketing capture rewired.** `Tedwren.Web` `ILeadRouter` now `ApiLeadRouter` — forwards demo/contact
  leads to `/api/leads/capture` (typed HttpClient, `Api:BaseUrl`; logs/no-ops when unset), replacing
  `LoggingLeadRouter`. Referral attribution in `LeadController` is unchanged.
- ✅ Tests: `LeadServiceTests` (create/status-note/convert/capture-dedupe) + `LeadApiTests` (admin flow,
  anonymous capture dedupe, 404). Whole solution builds (0 code warnings); all tests pass (LocalDB skipped).

### Admin — Launch List + separate Commercial database (previous change)
- ✅ **Separate Commercial database (architecture).** All commercial/admin-plane data now targets a second
  database via its own connection string (`ConnectionStrings:SqlServerCommercial` / `PostgreSqlCommercial`,
  empty ⇒ falls back to the product connection string). New `AdminSqlDataAccessOptions`,
  `IAdminDbConnectionFactory`/`AdminDbConnectionFactory` and `AdminRepositoryBase` (reuses every
  `RepositoryBase` helper); `MigrationRunner` is area-aware (`MigrationArea.Product`/`Commercial`) and run
  once per database from `Program.cs`. Registered via `AddCommercialSqlDataAccess(...)`. Cross-DB links are
  soft `Guid` references (no cross-database FK); R15 tenant scoping stays in query predicates.
- ✅ **Billing plane relocated.** Mandates/payments/subscriptions/webhook-events/payouts repositories now use
  `AdminRepositoryBase` and register in `AddCommercialSqlDataAccess`; scripts `022`–`024` moved to
  `Migrations/Scripts/{SqlServer,Postgres}/Commercial/`. Existing populated product DBs need a one-off data
  copy of those tables into the commercial DB (see `docs/ef-migrations.md`).
- ✅ **Launch List (Web Content Spec §6.9).** Vertical slice in the commercial DB: `LaunchSignup` entity,
  DTOs, `ILaunchListService`/`LaunchListService`, `ILaunchSignupRepository` (Dapper dual-engine + in-memory),
  script **`025_launch_signups.sql`** (dedupe on lower-cased email). Endpoints: anonymous
  `POST /api/launch-signups`; `PlatformAdmin` `GET /api/launch-signups` + `POST .../notify`. Admin page
  `/admin/launch-list` (KPI row + `DataTable` + confirm-gated bulk send); branded `LaunchAnnouncementEmail`
  sent per address individually via `IEmailSender`. Nav item added to `AdminNavItems`.
- ✅ **Landing-page capture (Tedwren.Web).** Email form on the home page + standalone `/launch` (antiforgery +
  honeypot + min-fill via existing `AntiBot`, rate-limited) → `ILaunchSignupSink`/`ApiLaunchSignupSink`
  forwards to the API (`LaunchSignup:ApiBaseUrl`; logs/no-ops when unset).
- ✅ Tests: `LaunchListServiceTests` (dedupe, per-address notify, failure counting) + `LaunchListApiTests`
  (anonymous signup/dedupe, list, notify). Whole solution builds (0 new warnings); all tests pass (LocalDB
  integration tests skipped). Existing DataAccess integration tests updated to the new `MigrationRunner`
  signature (Product area).
- ⏳ Next: Phase 2 Lead management (card-grid admin UI + notes + convert), Phase 3 Affiliates (profit-after-
  margin commission plans, payouts, e-sign agreement + PDF). A commercial-DB EF `DbContext` mirror is
  deferred — the idempotent SQL scripts are authoritative for the commercial DB for now (as with the deferred
  Postgres EF path, `docs/ef-migrations.md §7`).

### Admin area — Phase D: GoCardless BACS payouts (previous change)
- ✅ **Payout settlement reads.** `IGoCardlessClient.ListPayoutsAsync` (`GET /payouts`) + `Payout` entity /
  `PayoutStatus` enum (Pending/Paid), `IPayoutRepository` (Dapper dual-engine + in-memory), migration
  **`024_payouts.sql`** (both engines, unique on the GoCardless payout id). A payout is Tedwren's own
  settlement, so it is **not** tenant-scoped (documented on the entity).
- ✅ **Sync + admin surface.** `PayoutSyncService` upserts payouts from GoCardless (deduped, safe no-op when
  unconfigured — same shape as `BillingReconciliationService`), folded into
  `BillingReconciliationHostedService` so payouts refresh on the existing schedule. `IBillingService` gains
  `GetPayoutsAsync`/`SyncPayoutsAsync`; `GET /api/admin/billing/payouts` + `POST .../payouts/sync` under
  `PlatformAdmin` (sync returns 503 when GoCardless is unconfigured). `/admin/payouts` is now a live
  `DataTable` with a "Refresh from GoCardless" button (reuses the money formatter + `StatusPill`).
- ✅ Tests: `PayoutSyncServiceTests` (add, update-not-duplicate, unchanged-not-recounted, unconfigured no-op)
  + API payouts-200 and sync-503. Whole solution builds (0 new warnings); all 509 tests pass (15 LocalDB
  skipped). **Admin-area plan (Phases A–D) complete.**

### Admin area — Phase C: GoCardless webhooks, returns & reconciliation (previous change)
- ✅ **Signature-verified webhook receiver.** `POST /api/webhooks/gocardless` is `.AllowAnonymous()` (webhooks
  aren't JWT-authed) but authenticated by the `Webhook-Signature` HMAC-SHA256, verified against
  `GoCardless:WebhookSecret` **before** any processing (`GoCardlessSignatureVerifier`, constant-time); it
  fails closed on a bad/absent signature or unset secret (401). The read-only write-blocker only applies to
  authenticated users, so the anonymous webhook is unaffected.
- ✅ **Idempotent event processing.** `GoCardlessWebhookProcessor` stores each event once (deduped by
  GoCardless event id), updates the referenced mandate/payment to the event's status via `GoCardlessStatusMap`
  (now action-aware, returning null for no-op actions), and records a **returned payment's** failure reason so
  the admin can re-take it (the Phase B retry path). One failing event never aborts the batch; every event's
  outcome is stored. New `WebhookEvent` entity + repo (Dapper dual-engine + in-memory) + migration
  **`023_webhook_events.sql`** (both engines, unique on the event id).
- ✅ **Reconciliation backstop.** `BillingReconciliationService` polls GoCardless for non-terminal
  mandates/payments and converges their status if a webhook was missed; `BillingReconciliationHostedService`
  runs it on a schedule (modeled on `ExpirySchedulerHostedService`, gated by `Jobs:SchedulerEnabled`,
  `Jobs:ReconciliationIntervalHours` default 6). Safe no-op when GoCardless is unconfigured.
- ✅ **Admin events view.** `/admin/events` is now functional (`GET /api/admin/billing/events` under
  `PlatformAdmin` → `IBillingService.GetWebhookEventsAsync`), showing each event's resource/action/outcome.
- ✅ Tests: `GoCardlessWebhookTests` (signature valid/tampered/empty; processor status-update, returned-reason,
  dedupe, unknown-resource) + `BillingReconciliationServiceTests` (converge, skip-terminal, unconfigured no-op)
  + API webhook 401-without-signature and events-200. Whole solution builds (0 new warnings); all 503 tests
  pass (15 LocalDB skipped).
- ⏳ **Next (Phase D):** BACS payouts (settlement reads) + `/admin/payouts`. **Sandbox credentials** still let
  Phases B–C be verified live (token in `GoCardless:AccessToken`, `GoCardless:WebhookSecret` for webhooks).

### Admin area — Phase B: GoCardless mandates & payments (previous change)
- ✅ **GoCardless transport seam.** `GoCardlessOptions` (Abstractions) + a conditional typed `HttpClient`
  in `Program.cs` (base address + Bearer token + `GoCardless-Version` header), mirroring the Resend email
  integration. `IGoCardlessClient`/`GoCardlessClient` (Application) cover hosted mandate set-up (Billing
  Request Flow — no raw bank details handled), get/cancel mandate, create/get/retry payment, with
  idempotency keys on payment creation. When no token is configured an `UnconfiguredGoCardlessClient`
  default stands so reads work and collection actions fail with a clear "not configured" (503) message.
- ✅ **Billing domain slice (net-new, `CompanyId`-scoped, R15).** `Mandate`, `Payment`, `BillingSubscription`
  entities + status enums mirroring GoCardless; `IBillingService`/`BillingService` map provider statuses via
  `GoCardlessStatusMap`; Dapper repositories over `RepositoryBase` (ANSI-portable) + in-memory doubles;
  migration **`022_billing.sql`** in both SqlServer and Postgres folders. Meter/band held as **configuration
  keys, not prices** (PRD §9); amounts in minor units (pence).
- ✅ **Admin billing UI + API.** `BillingEndpoints` under `PlatformAdmin` (`/api/admin/billing`): list
  mandates/payments, company overview, start/cancel mandate, take/retry payment, set subscription.
  `ApiBillingService` client proxy; the `/admin/billing`, `/admin/payments` and `/admin/subscriptions`
  placeholder pages are now functional (mandate set-up returns the hosted authorisation link; take a
  payment; **re-take a returned payment**; set a company's meter/band). Two-way bound inputs per the
  live-state rule.
- ✅ Tests: `BillingServiceTests` (11 — set-up, reuse-pending, take-payment + no-mandate/zero guards,
  re-take returned/non-returned/unknown, cancel, subscription upsert) + `AdminBillingApiTests` (4 — reads
  200 under platform admin, setup → 503 unconfigured, subscription persists). Whole solution builds
  (0 new warnings); all 491 tests pass (15 LocalDB skipped).
- ⏳ **Next (Phase C):** GoCardless webhooks (HMAC-verified, deduped) to keep mandate/payment status live,
  returns→retry automation, and a reconciliation background job. **Needs sandbox credentials** to verify
  live end-to-end (token in `GoCardless:AccessToken`); no secrets committed.

### Admin area — Phase A: platform-admin shell & read-only views (previous change)
- ✅ **Platform-admin gate (server-authoritative).** New `PlatformAdmin` authorization policy
  (`src/Tedwren.Api/Program.cs`) — stricter than `AdminOnly`: requires `AccessRole.Administrator` **and**
  the `company` claim to equal the Tedwren seed tenant (`AdminUserSeeder.SeedCompanyId`), so a customer's
  own company administrator can never reach cross-company data. `CurrentUserDto` gains a server-computed
  `IsPlatformAdmin` (in `ClaimsCurrentUserService`); the client exposes `AuthState.IsPlatformAdmin`
  (sourced from `/api/me`, never derived client-side).
- ✅ **Admin menu swap.** `Admin:Enabled` flag in the client `appsettings.json` (bound to
  `AdminAreaOptions`) turns the capability on per deployment; `MainLayout` swaps the sidebar to
  `ShellChrome.AdminNavItems` when the flag is set **and** the signed-in user is a platform admin, bypassing
  entitlement/onboarding gating for the (non-purchasable) admin surfaces. Regular users are unaffected.
- ✅ **Admin surface.** New `/admin/*` pages under `Pages/Admin` (dashboard, companies, users, plus
  placeholders for subscriptions/billing/payments/events/payouts/settings), each wrapped in an `AdminGuard`
  that redirects non-admins. Reuses the existing `DataTable`/cards/`StatusPill` kit. Backed by a
  dedicated, `PlatformAdmin`-gated `/api/admin` surface (`AdminEndpoints`, `IPlatformAdminService` →
  `ApiPlatformAdminService`) that reuses the existing organisation/user services — the tenant console
  endpoints are left untouched so they keep working for normal company admins.
- ✅ Tests: `ClaimsCurrentUserServiceTests` (platform-admin computation: seed-tenant admin true; other-tenant
  admin, non-admin role and anonymous all false) + `AdminApiTests` (admin reads 200 under the seed-tenant
  identity; `/api/me` reports the flag). Whole solution builds (0 new warnings); all 476 tests pass.
- ❗ **PRD gap raised (per CLAUDE.md — do not silently work around).** The admin area's billing scope
  (GoCardless direct-debit collection for the SaaS, Phases B–D) is **not in PRD v6.4** — §9 defines the
  commercial model (metered by sites/operatives) but names no collection rail, and §12.8 only cites Stripe
  card checkout for the separate Worker Passport product. Confirmed with the product owner that GoCardless
  is the intended SaaS billing rail; this should be reconciled into PRD §9 in a future revision. Tracked in
  `docs/plan-and-scope.md` (Admin-area phases).

### Tedwren.Web — Phase W8 Hardening & pre-launch QA (previous change)
- ✅ **Content lint as a build/CI gate (Web Plan §8, §14).** `Tedwren.Web.Qa.ContentLint` scans the
  content JSON + Razor views and fails on three commercial/legal breaches: a hardcoded price symbol
  outside the single `PricingPlan` source (`hardcoded-price` — `£` always, `$`/`€` only next to a digit
  so `$"…"` interpolation isn't a false hit), absolute-compliance claims (`absolute-compliance` —
  guarantee/ensure/100% compliant/fully compliant, §8.1, with a reviewed allowlist seam), and CSCS
  rivalry/replacement/on-demand-verification or Digital-Skills-Passport positioning (`cscs-positioning`,
  §8.2 — scanned in titles/meta too, while the understated CSCS add-on line is left untouched).
- ✅ **SEO/crawler infrastructure (Web Plan §9/§10).** `SitemapBuilder` + `SeoController` generate
  `/sitemap.xml` and `/robots.txt` from the live `SiteConfig` route list (never hand-maintained),
  excluding and disallowing the capability URLs (`/partners/dashboard`, `/r/`); `_Layout` now emits a
  `rel="canonical"` per page (query/UTM stripped). Worker Passport meta-title shortened so the rendered
  `<title>` fits the ~60-char SERP budget (still CSCS-safe, keeps the "you own" benefit).
- ✅ **§14 checklist enforced where automatable.** `docs/web-launch-qa-checklist.md` maps every Spec §14
  item to its enforcing test or its manual/sign-off owner. New tests: `ContentLintTests` (gate + each
  rule proven on a seeded breach + allowlist), `SeoInfrastructureTests` (sitemap/robots/canonical, one
  `<h1>`/page, title<60/meta<155 across all indexable pages), `LaunchGuardrailTests` (pack chrome has a
  UTM-tagged demo CTA and **no** sign-up wall, §4.1).
- ✅ Tests: Web 84 → **164**. Whole solution builds (0 errors); Web + Web.Tests 0 warnings.
- ❗ **Manual/sign-off, not codeable here (tracked in the checklist doc):** mobile Core Web Vitals
  (Lighthouse) and the full axe + manual-mobile WCAG pass need a live browser; legal review of the legal
  pages, the Companies House footer number/address, and the Plan §11 / Spec §13 open items remain
  founder/legal sign-off before go-live.

### Tedwren.Web — Phase W7 Partners programme (previous change)
- ✅ **Approval-gated applications — no self-serve activation (Web Plan §7.2).** `/partners` shows the
  programme content + an application form; submitting only ever creates a **pending** record.
  `PartnerService.Approve` is a separate human step that mints a `Partner` with a **unique referral
  code** and activates the dashboard; nothing activates on submit.
- ✅ **§7.3 exclusion enforced.** The form asks the relationship + "I control/influence site access"
  questions; an applicant who controls site access **cannot be approved** (`Approve` throws), and the
  page states the exclusion plainly. The programme is never a route to site access.
- ✅ **Referral attribution + clawback modelled from the start (§7.2).** `ReferralService` attributes a
  conversion to a partner's code (`/r/{code}` sets the `tedwren_ref` cookie so credit lands across
  sessions — the demo POST captures it). Commission is **20%** of first-year revenue, **tied to the
  specific referral**, with a **90-day clawback** that is reversible against that referral (and refused
  once the window passes). Idempotent reversal.
- ✅ **Simple partner dashboard — private, not public.** `/partners/dashboard/{code}` renders a partner's
  referrals + commission totals (pending/paid/clawed back); an unknown/inactive code 404s.
- ✅ **Seams + persistence.** `IPartnerStore` (in-memory singleton at launch; DB-backed store slots in
  later) under `Tedwren.Web.Partners`; `PartnerProgrammeContent` (+ `partners.json`) added to the content
  layer for all page copy; `AddPartnerProgramme` DI registration; `TimeProvider` for testable clawback timing.
- ✅ Tests: Web 73 → **84** (unit: pending-only submit, approve activates, §7.3 refusal, referral capture,
  20%/clawback/deadline, dashboard totals; integration: page states exclusion + form, application →
  pending record with no activation, dashboard 404 on unknown code, referral link attributes a later demo
  conversion end-to-end). Whole solution builds; Web project 0 warnings.
- ⏳ **Deferred / open:** the dashboard is reached by a capability URL (the referral code); real partner
  auth is a follow-up. Whether the programme goes public at launch vs. an unlinked application page, and
  the vetting owner, remain sign-off items (Plan §11.7–8).

### Tedwren.Web — Phase W6 Lead capture & consent (previous change)
- ✅ **Demo + Contact forms (Web Plan §6.9, §7).** `/demo` and `/contact` are server-validated
  (DataAnnotations) with **antiforgery**, a **honeypot**, a **minimum fill-time** check (`AntiBot`) and
  **rate limiting** (fixed-window policy on the POST endpoints). Genuine submissions route via the
  `ILeadRouter` seam (`LoggingLeadRouter` at launch — no CRM/email wired yet, plugs in behind the
  interface); post/redirect/get to a thank-you page. Demo thank-you shows the real **booking link** from
  config (not "we'll be in touch"). Bots (honeypot/too-fast) get the same response but route nothing.
- ✅ **Contact routing by reason.** `ContactRouting.ResolveInbox` maps General/Press/Partner/Support →
  the configured inbox (falls back to sales, then a visible "unrouted"). Reason→inbox asserted by a test.
- ✅ **UTM attribution (§4.1, §11).** `/demo` captures `utm_*` from the query into hidden fields → the
  routed lead carries the source, so pack-driven bookings are attributable. `PackChrome` view component
  supplies the compliance-pack viewer's light header/footer with a `utm_source=pack` "Book a demo" link.
- ✅ **Consent before scripts (§5.5, §8).** `ConsentBanner` (form-based, works with JS off) offers a
  **one-click "Reject non-essential"** with equal weight to Accept; `ConsentController` (`/consent`) stores
  the choice in the `tedwren_consent` cookie. **No analytics tag is emitted before consent** — GA4 is
  gated by `AnalyticsState` on *both* analytics consent and a configured measurement id (empty by
  default, so nothing loads in this environment). Demo-submit fires a consent-gated `generate_lead`
  conversion event.
- ✅ **Error route hardening.** `/error` is now verb-agnostic so a failed POST (e.g. antiforgery) keeps
  its 400 through status-code re-execution instead of 405-ing.
- ✅ Tests: Web 57 → **73** (valid/invalid/honeypot/too-fast/antiforgery form flows, reason routing, UTM
  capture, consent banner + one-click reject, no-script-pre-consent, GA gating; plus `AntiBot`,
  `ContactRouting`, `ConsentState`, `Utm` unit tests). Whole solution builds; Web project 0 warnings.
- ⏳ **Deferred within W6:** additional conversion events (pricing→demo click, Worker Passport checkout
  start/complete) — the consent-gated dataLayer/GA mechanism is in place; wiring those specific events is
  incremental (WP checkout is product-owned). Real CRM/email + calendar provider behind the seams.

### Tedwren.Web — Phase W5 Trust, About, FAQ, Legal (previous change)
- ✅ **Security & Trust (`/security`, §6.6).** Own stable URL; only makeable claims (data ownership,
  access control, encryption in transit, audit trail, DPA on request). No fabricated ISO/Cyber Essentials
  badges and no absolute-compliance language — asserted by a test scanning for prohibited phrasing.
- ✅ **About (`/about`, §6.7).** Founder-led credibility from content.
- ✅ **FAQ (`/faq`, §6.8).** `FaqAccordion` (native `<details>`, JS-free) over content FAQs, plus valid
  **FAQPage JSON-LD** (parsed + type-checked in a test).
- ✅ **Legal ×4 (§4).** Privacy, Cookies, Terms, Data Protection served as **real content** (not
  placeholders) via `LegalController` → `IContentProvider.FindLegal(slug)`; each shows a "Draft — pending
  legal sign-off" label (Plan §11.4). Wording avoids the W8 compliance-lint tokens.
- ✅ **Sitewide SEO.** Organization JSON-LD in the layout; `JsonLd` helper returns whole `<script>` blocks
  emitted raw. Web encoder set to `UnicodeRanges.All` so £/em-dashes render as real characters.
- ✅ Tests: +6 (security no-fabricated/absolute claims, about renders, FAQ questions + valid schema, four
  legal pages serve real content, valid Organization schema). Whole solution builds; Web project 0 warnings.

### Tedwren.Web — Phase W4 Worker Passport & Pricing (previous change)
- ✅ **Worker Passport (`/worker-passport`, §6.4).** Individual-buyer register/tone; the "never locked out
  for non-payment" benefit (PRD Rule W2); price line from the single configured `PricingPlan` source;
  consumer-contract facts (annual billing, UK 14-day cancellation, informed consent). **CSCS positioning
  restriction** enforced in copy **and** title/meta — a test scans the `<head>` for prohibited CSCS
  rivalry/replacement phrasing (§8.2).
- ✅ **Pricing (`/pricing`, §6.5).** `PricingTable` renders every number from config; unpriced bands show
  "Pricing on request" (not a fabricated number) pending the §11.2 sign-off; plain-language clarifiers
  (active operative/site, 10% buffer) and trust notes ("sites are free to record", "a dispersed scheme =
  one site"). SoftwareApplication JSON-LD emitted + validated.
- ✅ **Single price source; £ in one place.** Prices are decimals in `PricingPlan`; `IContentProvider.FormatMoney`
  applies the currency symbol (one map). Worker Passport £10 (PRD value) — **£10/£12 conflict still flagged
  for sign-off (Plan §11.1)**; bands at 0 → "Pricing on request" (Plan §11.2). No `£` literal in views/content.
- ✅ **Meta descriptions** added to the layout (`ViewData["MetaDescription"]`), set per page from content.
- ✅ Content model extended (Abstractions): `WorkerPassportContent`, `PricingPageContent`, `SecurityContent`,
  `AboutContent`, `LegalDocument`; provider exposes them + `FindLegal` + public `FormatMoney`.
- ✅ Tests: +8 (WP benefit, WP price from config, CSCS meta restriction, pricing clarifiers + unpriced band,
  valid SoftwareApplication schema, provider FindLegal + FormatMoney). Web tests 41 → 57.

### Tedwren.Web — Phase W3 Core pages (previous change)
- ✅ **Home, Subcontractors, Main Contractors from content (Web Plan §6.1–6.3).** The three core pages
  now render from the content layer, not stubs. Home: hero + audience split, problem section, the two
  product cards (short-form), differentiators, five-step how-it-works, trust strip, closing CTA. Product
  pages render long-form via `ProductDetail`.
- ✅ **No forked product copy.** `ProductCard` (short) and `ProductDetail` (long) render the **same**
  `ProductProfile` entry, so the home card and the dedicated page can't drift — asserted by a test.
- ✅ **Required §6 content present.** Subcontractor page carries the "company documents" feature and an
  understated CSCS line; Main Contractor page carries a **substantial, distinct** retrofit / dispersed-
  site section ("Workforce management when there isn't a site gate") as an emphasised content section —
  not a footnote. The strongest home differentiator ("Works beyond the site gate") gets a distinct
  highlight treatment.
- ✅ **New reusable components (Plan §5).** `ProductCard`, `ProductDetail`, `FeatureGrid`, `TrustStrip`,
  `Differentiators`, `HowItWorks` view components — catalogued in `docs/web-component-catalogue.md`.
- ✅ **Content model extended.** Added `HomeContent`, `Differentiator`, `HowItWorksStep`, `ContentSection`
  (+ optional `Sections` on `ProductProfile`) in Abstractions; `home.json` added; provider exposes `Home`.
- ✅ **Styles from tokens only.** New component CSS in `site.css` uses `tokens.css` variables for all
  colour/spacing — no literals. (Razor note: a loop variable named `section` collides with the `@section`
  directive; renamed to `part`.)
- ✅ **Tests + build.** `Tedwren.Web.Tests` 36 → 41 (home sections render, differentiator highlight,
  company-documents present, emphasised retrofit section, product copy single-sourced). Whole solution
  builds (0 errors); Web project 0 warnings.

### Tedwren.Web — Phase W2 Content layer (previous change)
- ✅ **`IContentProvider` seam + content types (Web Plan §3).** Added
  `Tedwren.Abstractions.Services.IContentProvider` and the content model
  (`Tedwren.Abstractions.Contracts.WebContent`: `SiteContent`, `ProductProfile`, `FeatureCard`,
  `PricingPlan`, `TrustPoint`, `FaqItem`, `Testimonial`, `SocialAccount`). Types live in Abstractions so
  the product-owned compliance-pack viewer (Plan §4.1) can reuse the same site/brand content.
- ✅ **JSON-backed provider.** `Tedwren.Web.Content.JsonContentProvider` loads `Content/*.json`
  (`site`, `products`, `pricing`, `trust`, `faqs`, `testimonials`) once at startup, registered as a
  singleton via `AddJsonContent`. Fails fast on a missing file. A CMS can replace it behind the same
  interface with no view changes. Content files copy to publish output; content root is the project dir
  in dev/test.
- ✅ **Naming/price key indirection (Plan §2, §8).** `ResolveToken` resolves dotted keys
  (`Site:Brand`, `Products:{key}:Name|Slug|Tagline`, `Pricing:{key}:Annual|Monthly`) so names/prices are
  referenced by key, never inline; unknown tokens throw. Prices are decimals in `PricingPlan` (the only
  home for prices) formatted via a single currency-symbol map — no "£" literal in views/content.
  Worker Passport price seeded at £10 (PRD value) with the £10/£12 conflict still flagged for sign-off
  (Plan §11.1); band prices left at 0 pending the publish-vs-"from £x" decision (Plan §11.2).
- ✅ **Chrome now consumes content.** `SiteHeader`/`SiteFooter`/`_Layout` read brand, legal entity and
  social from `IContentProvider`; `SiteConfig` (appsettings) is trimmed to nav **structure** only
  (which pages, in what order). Identity moved out of appsettings into `site.json`.
- ✅ **Component catalogue.** Added `docs/web-component-catalogue.md` (content types + view components,
  built and planned, plus conventions), per the W2 exit criteria.
- ✅ **Tests + build.** `Tedwren.Web.Tests` now 36 (was 22): isolated provider unit tests (load, lookup,
  token/price indirection, fail-fast on missing file) + DI/integration tests proving the shipped content
  loads at the real content root and reaches the footer. Whole solution builds (0 errors); Web project 0
  warnings.

### Tedwren.Web — Phase W1 Skeleton (previous change)
- ✅ **New marketing-site project.** Added `src/Tedwren.Web` (ASP.NET Core MVC, `Microsoft.NET.Sdk.Web`,
  `net10.0`) and `tests/Tedwren.Web.Tests`, both wired into `Tedwren.sln`. Server-rendered Razor, no auth
  (public site) — deliberately no `FallbackPolicy` unlike the API. Per the Tedwren.Web Plan & Scope of Works
  (`docs/Tedwren-Web-Plan-and-Scope-of-Works.md`) §10, phase W1.
- ✅ **Routing for the full sitemap (Web Plan §4).** Attribute-routed controllers, one per content area:
  Home (`/`), Products (`/subcontractors`, `/main-contractors`), WorkerPassport (`/worker-passport`), Pricing
  (`/pricing`), Trust (`/security`), About (`/about`), Faq (`/faq`), Lead (`/demo`, `/contact`), Partners
  (`/partners`), Legal (`/legal/{privacy|cookies|terms|data-protection}` — slug constrained). Unknown routes
  re-execute a friendly 404 page (Return home / Book a demo).
- ✅ **Config-driven chrome (Web Plan §3, §5).** `SiteHeader`/`SiteFooter`/`Cta` view components render brand,
  legal entity (Tedwren Ltd + optional company no./office), nav and CTAs from the bound `Site` config section
  (`SiteConfig`) — not hardcoded in views. The seam W2 swaps for `IContentProvider`. Header CTA is "Book a demo"
  everywhere, swapping to "Get your Worker Passport" on the Worker Passport page only; footer social icons are
  config-gated (none render at launch). Mobile: hamburger nav with the CTA kept outside the collapsed menu.
- ✅ **Canonical CTAs as a mechanism (Web Plan §5.4).** `Cta` accepts a closed `CtaAction` enum (Book a demo /
  Start a pilot / Get your Worker Passport) with fixed copy+href, so vague labels can't be introduced by a view.
- ✅ **Shared design tokens, single source.** `tokens.css` stays owned by `Tedwren.Client`; a build target
  copies it into `Tedwren.Web/wwwroot/css` (git-ignored) so the site serves it with no colour/spacing literal
  duplicated. `site.css` layers layout using only tokens.
- ✅ **Tests + build.** `Tedwren.Web.Tests` (22 tests) assert every route resolves, unknown routes 404 to the
  error page, the config chrome renders, the CTA swap works, and both stylesheets (incl. client-sourced tokens)
  are served. Whole solution builds (0 errors; pre-existing MUD0002 + one Api.Tests nullable warning unchanged).

### Onboarding wizard per-step validation (previous change)
- ✅ **Validate on Next, per step.** The onboarding wizard previously only validated required fields on the
  final "Finish setup". It now validates the step being left when the user presses Next: `TedwrenStepper`
  forwards MudStepper's `OnPreviewInteraction` (`Func<StepperInteractionEventArgs, Task>`), and
  `Onboarding.razor` cancels a forward move off an invalid step (company name required on the Company step;
  administrator name + valid email on the Administrator step, SF-20). Errors surface only once a step has been
  attempted (`_validatedSteps` gate via `ShowErrors`), so they appear on the step in question rather than all
  at once; `Finish` remains a backstop.
- ✅ Client builds clean (0 errors); pre-existing MUD0002 analyzer casing warnings unchanged.

### Onboarding wizard polish + binding/auth guardrails (previous change)
- ✅ **Chrome-free layout.** `OnboardingLayout` app bar removed; the wizard now carries the Tedwren brand
  mark top-left in its own masthead, on a plain sunken (`--color-bg`) centered shell.
- ✅ **Wizard tidy-up.** Professional step scaffolding: heading/hint per step, selectable choice cards for the
  org-type step (brand-pale selected state), sites/operatives as titled sub-cards ("Site 1"…) with a header
  remove action, insurances as a checklist with a selected state, and a bordered review summary. All colour
  from `tokens.css`.
- ✅ **`_model.TypeLabel` bug fix.** The company-step "Company type" was a read-only `MudTextField` fed a
  derived value one-way (`Value=` with no `ValueChanged`) — MudBlazor inputs cache their text, so it stuck on
  the first-render default. Now rendered as plain markup so it always reflects the chosen type. Audited every
  other `Value=` binding: all others correctly pair with `ValueChanged`.
- ✅ **Guardrails in `CLAUDE.md`.** Added engineering standards for (a) Blazor bindings reflecting live state
  (two-way or plain markup; never one-way `Value=` for mutable/derived values) and (b) the secure-by-default
  API (`FallbackPolicy` requires auth; pre-auth flows must `.AllowAnonymous()`, sensitive ones must not).
- ✅ Whole solution builds; `dotnet test` green (API 63, Application 117, others unchanged).

### Login redesign + forgot-password (D1, previous change)
- ✅ **401 crash fix (root cause).** `MainLayout` redirected unauthenticated users to `/login` but still
  rendered the routed `@Body`, so a console page (e.g. Dashboard) called the API tokenless, threw on the 401
  and crashed the WASM renderer before the redirect landed (surfaced *on* the login page). The shell + `@Body`
  now render only once the sign-in check passes (`_ready` gate), so no page initialises unauthenticated.
- ✅ **Full-screen branded login.** New `AuthLayout` (no app bar): split screen — Tedwren brand panel (logo,
  tagline, feature list, brand-orange gradient from `tokens.css`) on the left, form on the right; stacks on
  narrow screens. `Login`, `AcceptInvite`, and the new pages moved onto it (off `RecipientLayout`, which stays
  for external compliance-pack recipients). Login restyled ("Sign In" / credentials copy) with Enter-to-submit.
- ✅ **Forgot / reset password.** `POST /api/auth/forgot-password` (anonymous) → `IAuthService.
  RequestPasswordResetAsync`: mints a 1-hour one-time token (reuses the invite-token fields, no schema change)
  and emails a branded reset link (`PasswordResetEmail` → `{ConsoleBaseUrl}/reset-password?token=…`). Always
  returns 200 and never discloses whether the email exists (**no account enumeration**); only Active accounts
  get a link. New `/forgot-password` and `/reset-password` pages; reset reuses the accept-invite endpoint to
  set the new password and sign in. `AuthState` gains `RequestPasswordResetAsync` / `ResetPasswordAsync`.
- ✅ Tests: `AuthApiTests` — forgot-password stays anonymous & non-enumerating; forgot → reset → login
  (old password rejected, new accepted). `dotnet test` green (API 63, Application 117).

### Email notifications wired into real flows — invite email + test-send (previous change)
- ✅ **Console-user invite now emails the accept-invite link.** `UserService.InviteUserAsync` composes a
  branded invite (greeting, "Accept your invitation" button → `{ConsoleBaseUrl}/accept-invite?token=…`,
  expiry note, fallback link via new `InviteEmail` composer) and sends it. **Best-effort:** the user is still
  created and the token returned on any delivery failure, so an invite is never lost; `InviteUserResult` now
  carries `EmailSent`. `InviteUser.razor` shows "emailed to X" and keeps the link as a copy/share fallback.
- ✅ **Rich-HTML sending seam.** `IEmailSender.SendHtmlAsync(to, subject, contentHtml)` added so component-based
  emails (buttons/tables/2FA) can be sent; `ResendEmailSender` wraps content via `EmailTemplateRenderer.Render`
  and `OutboxEmailSender` records it. The plain-text `SendAsync` path (existing jobs) is unchanged.
- ✅ **Admin test-send endpoint** `POST /api/email-templates/test-send` (AdminOnly) delivers a branded sample
  and reports the resolved provider, so live Resend delivery can be verified from within the app.
- ✅ **`EmailOptions.ConsoleBaseUrl`** added (+ appsettings) to build console links in emails.
- ✅ Confirmed the **scheduled notifications are already wired** to real DB data — `ExpiryWarningJob` (SF-9),
  `WeeklyDigestJob` (SUB-5), `OvernightSignInJob` (SF-19) and `JobHeartbeatMonitor` (R12) run via
  `ExpirySchedulerHostedService` and call `IEmailSender`; they send real branded email as soon as
  `Email:Provider=Resend` + a key are set. No code change needed there.
- ✅ Tests: `UserServiceTests` (invite emails the link / outbox-stub reports not-sent / a throwing sender still
  creates the user); `UserApiTests` (invite records the email end-to-end); new `EmailApiTests` (test-send
  delivers + input validation). Full suite green (Application 117, Api 61).

### Branded HTML email template + Resend delivery (Phase-7 email — previous change)
- ✅ **Branded, HTML-email-compliant template hosted in the API.** Table-based, inline-styled shell
  (`EmailLayout`) with the Tedwren logo top-left, a white content container and a "Private & Confidential /
  Tedwren Ltd" footer, built from a new `EmailOptions` config contract (`Email` section). Renders reliably in
  Outlook/Gmail/webmail (no flexbox/grid, no SVG). `src/Tedwren.Application/Notifications/Email/`.
- ✅ **Optional component kit** (`EmailComponents` + fluent `EmailContentBuilder`): heading, paragraph,
  bulletproof button, data table, verification/2FA code block, highlighted callout, inline highlight,
  key/value rows, bullet list, divider, spacer — all email-safe with caller text HTML-encoded.
- ✅ **`IEmailTemplateRenderer`/`EmailTemplateRenderer`** wrap either composed components or a plain-text body
  (auto-split into paragraphs), so the existing SF-9/SUB-5/R12 job emails become branded with no call-site
  changes.
- ✅ **Resend.com delivery** — `ResendEmailSender : IEmailSender` (typed `HttpClient` → `POST /emails`, Bearer
  auth) fulfils the deferred "Real SMS/email providers (PRD-Phase 7)" item for email. Registered from the API
  composition root and gated by config: `Email:Provider=Outbox` by default (nothing sends), overrides the stub
  only when `Provider=Resend` + an API key are set. API key in `appsettings.json` for now (rotate to a secret
  store before launch). SMS provider remains outstanding.
- ✅ **Logo asset + endpoints hosted in the API**: embedded PNG served anonymously at
  `GET /api/email-assets/logo.png` (absolute URL used by emails), plus a Development-only
  `GET /api/email-templates/preview/{template}` to eyeball the layout/components without sending.
- ✅ Tests: `EmailTemplateRendererTests` (logo/container/footer present, HTML-encoding, components) and
  `ResendEmailSenderTests` (endpoint/URL, Bearer header, JSON payload, reply-to, non-2xx throws) via a
  hand-written fake `HttpMessageHandler` (no mocking lib); test host keeps the outbox sender so existing
  outbox assertions stay green.

### Demo write-actions persisted (D7 — previous change)
- ✅ **Site "Edit" now persists.** `ISiteService.UpdateSiteAsync` + `PUT /api/sites/{id}` (tenant-scoped,
  R15/MC-21 → 404 for a foreign site) + `ApiSiteService.UpdateSiteAsync` + an `EditSiteDialog`; the
  SiteDetail "Edit" button replaces the demo snackbar and follows the new slug on rename.
- ✅ **Operative "Edit" now persists.** `IOrganisationService.UpdateEngagementAsync` + `PUT
  /api/organisation/companies/{companyId}/operatives/{engagementId}` (SF-2 name stays distinct within a
  company; tenant-scoped, R15) + `ApiOrganisationService.UpdateEngagementAsync` + an `EditOperativeDialog`.
  `OperativeDetailDto` now carries `EngagementId` + `CompanyId` so the page can address the engagement.
- ✅ **Operative "Send update link" now real.** Creates a genuine self-service onboarding link (SF-4/SUB-2)
  for the operative's company via `IOnboardingService.CreateAsync`; when the operative opens it and submits
  their mobile, SF-1 reuses the existing person so captured cards attach to them. Surfaced as a copyable
  banner (no email backend yet).
- ✅ **CompanyDetail "Compliance pack"** navigates to the real `/compliance-packs` builder instead of a demo
  snackbar; the stray unused `_busy` field warning on System Configuration is cleared (buttons now disable
  while saving). System Configuration general-settings + module entitlements and Permits "Save"/"Issue" were
  already persisted (M4/M5) — no demo write-actions remain under `Pages` (Reports/Integrations stay
  intentional placeholders, PRD Phase 7).
- ✅ Tests: `SiteApiTests` (update persists / foreign-tenant 404), `OrganisationApiTests` (engagement update
  persists / unknown 404), `SiteServiceTests` + `OrganisationServiceTests` unit coverage (update, duplicate-name
  refusal, cross-company refusal). `dotnet test` green (Application 105, Api 58).

### Deferred items, Phases D4–D6 (previous change)
- ✅ **D4 — real per-site operatives & compliance (MC-12/13).** `SiteService` now derives a site's operative
  count from the attendance log (distinct persons who attended) and their aggregate compliance via
  `ComplianceRollup` (SF-8), replacing the hard-coded `0`/`Pending`. The Dashboard heatmap becomes real
  automatically. Test: `SiteServiceTests.SiteOperatives_ComeFromAttendance`.
  ✅ **Follow-up done — tenant-scoping pass (R15/MC-21).** The seeded lead main contractor
  ("Meridian Construction Ltd") now carries the bootstrap admin's tenant id (`AdminUserSeeder.SeedCompanyId`),
  so it owns the seeded sites and operatives. `SiteService`, `WorkforceService` and `DashboardService` take
  the optional `ICurrentUserService` and scope sites / the operative register / the compliance tally to the
  signed-in caller's `CompanyId`; a site outside the caller's tenant fails visibly (404 via `GetSiteAsync →
  null`), never leaking across companies. Scoping is skipped when no current user is wired (unit tests) or the
  caller is unauthenticated, so those paths run unscoped rather than showing an empty screen. Tests:
  `SiteApiTests.TenantSite_IsListed_AndResolvableBySlug` / `ForeignTenantSite_IsHidden_AndReturns404`;
  workforce/dashboard/onboarding API tests now onboard into the caller's own company via `/api/me`.
- ✅ **D5 — retired the last demo constants.** `CompliancePacks`, `SiteGate`, `InductionRecords`,
  `TimeAndAttendance` now resolve the company from `ITenantState` and operatives/sites from
  `IWorkforceService`/`ISiteService` (SiteGate also runs a real `DecideAsync`); `TimeAndAttendance` uses the
  current Monday-anchored week. Fixed a latent SUB-22 bug: the pack-send role check compared against
  `"Compliance Manager"` but the claim carries the enum name `ComplianceManager`. Grep guard: no `Demo seed`
  constants remain under `Pages`.
- ✅ **D6 — N+1 batching.** `IQualificationCardRepository.GetByPersonsAsync` (Dapper `IN`/in-memory) added;
  `WorkforceService`, `DashboardService` and `SiteService` now fetch all operatives' cards in one read
  instead of per-person in a loop.
- ✅ Docs synced (`docs/plan-and-scope.md`, this file). `dotnet test` green (API 54, Application 100).
- ⚠️ **Still-open production follow-ups (from D1/D2):** set `Jwt:SigningKey` + `Seed:AdminPassword` from
  secrets; **rotate the committed DB password in `src/Tedwren.Api/appsettings.json`** (still present — a real
  credential in the repo). *(Invite-by-email is now delivered — see Completed. Onboarding links still need a
  delivery channel: the operative has no email captured, so SMS is the right route and isn't built yet.)*

### Deferred items, Phase D3: induction template authoring (previous change)
Implements MC-15/MC-4 — a manager edits the induction video, questions, pass mark and attempts.
- ✅ **Template gains** `AttemptLimit`, `Mandatory`, `MediaUrl`, `SiteId` (migration `017_induction_authoring.sql`,
  both providers; EF fields; Dapper + in-memory `UpdateAsync`).
- ✅ **Service** — `IInductionService.GetTemplateForEditAsync` (returns answers to the authorised admin only,
  R5) + `UpdateTemplateAsync` (name/validity/pass mark/attempts/mandatory/media/steps/questions; rejects a
  pass mark above the question count). New DTOs `InductionQuizAuthoringDto`, `InductionTemplateAuthoringDto`,
  `UpdateInductionTemplateRequest`. Quiz scoring stays server-side (`SubmitQuizAsync`, R5).
- ✅ **API** — `GET /api/inductions/templates/{id}/edit` + `PUT /api/inductions/templates/{id}` (authorised).
- ✅ **Client** — `Inductions.razor` builder is now a real editor: create-or-load the company template, edit
  details/media/validity/mandatory + a quiz question editor (prompt, options, correct answer), Publish persists.
- ✅ Tests: `InductionAuthoringApiTests` (create→edit→readback; pass-mark validation). `dotnet test` green (API 52).

### Deferred items, Phase D2: self-service operative onboarding link (previous change)
Implements SF-4/SUB-2 — an operative completes their own details and uploads card photos from a link.
- ✅ **Domain** — `OnboardingLink` (+`OnboardingLinkStatus`) and `StoredImage`; migration `016_onboarding.sql`
  (`OnboardingLinks` + `StoredImages`, both providers) + EF records + Dapper repos + in-memory doubles.
- ✅ **Service** — `IOnboardingService` (`CreateAsync`, `GetByTokenAsync`, `SubmitDetailsAsync`,
  `CaptureCardAsync`) reusing `IOrganisationService.AddOperativeAsync` (SF-1/SF-2), `IQualificationService.CaptureCardAsync`
  (cards land unconfirmed, SF-5/SF-6), and the compliance-pack `PackToken`/`PackPasscode` helpers (SUB-18
  30-day + passcode). `CaptureCardRequest` gained an optional `ImageReference`; card photos are stored via
  `IImageStore` and served only through the authorised `GET /api/images/{id}` (R9).
- ✅ **API** — `/api/onboarding` (create authorised; `view`/`submit`/`cards` anonymous, token+passcode gated);
  `/api/images/{id}` (authorised). `/api/qualifications/types` made anonymous (global reference library, SF-12,
  needed by the recipient page).
- ✅ **Client** — `ApiOnboardingService`; recipient page `/onboard?token=&passcode=` (RecipientLayout) with
  details form + card capture (photo → base64); `AddOperative` "send link" branch now mints a real link and
  shows it (no email backend yet).
- ✅ Tests: `OnboardingApiTests` (create/403/submit→workforce/capture). `dotnet test` green (API 50).

### Deferred items, Phase D1: authentication & authorization (previous change)
Adds real console sign-in (the PRD leaves the mechanism to the implementer, §10.1 Q1).
- ✅ **Credentials on `User`** — `PasswordHash`, `PasswordSetUtc`, `InviteToken`, `InviteTokenExpiresUtc`
  (migration `015_user_auth.sql`, SqlServer+Postgres; EF fields; Dapper+in-memory `GetByInviteTokenAsync`).
  PBKDF2 `PasswordHasher` (no new dep).
- ✅ **Auth service + JWT** — `IAuthService` (`LoginAsync`, `AcceptInviteAsync`) + `AuthService`;
  `ITokenIssuer`→`JwtTokenIssuer` (claims: sub/name/role/company); `JwtOptions` (`Jwt` section);
  `/api/auth/login` + `/api/auth/accept-invite` (anonymous). Invite now mints a one-time accept token
  (`InviteUserAsync`→`InviteUserResult`); `InviteUser.razor` surfaces the accept link (no email backend yet).
- ✅ **API authz** — JWT bearer + **secure-by-default** fallback policy (every endpoint needs a user unless
  `AllowAnonymous`). Recipient/kiosk flows kept anonymous (auth, `/health`, `/api/site-entry`, packs
  view/download, induction take-flow sessions). SF-23 **Auditor read-only enforced server-side** via a
  write-verb middleware. Claims-based `ICurrentUserService` (`ClaimsCurrentUserService`) — `/api/me` now
  returns the real user with a **real tenant CompanyId** (R15); the config stub is deleted.
- ✅ **Bootstrap admin** — `AdminUserSeeder` (idempotent, both modes) seeds an Administrator from the `Seed`
  config section so a fresh install can sign in.
- ✅ **Master administrators** — `AdminUserSeeder` also seeds the named Tedwren master administrators
  (`AdminUserSeeder.MasterAdmins`: leigh.hydes@, james.darby@, james.wheeler@tedwren.com) as active
  `Administrator` accounts in the seed tenant. Idempotent (matched by email, never clobbers an existing
  account); each uses the `Seed:Password` and should change it on first sign-in. Covered by
  `AdminUserSeederTests`.
- ✅ **Client** — `TokenStore` + `AuthTokenHandler` (attaches bearer, 401→`/login`), `AuthState`
  (login/accept/logout, localStorage token via `tedwren.auth.*`), `/login` + `/accept-invite` pages
  (RecipientLayout), MainLayout gates the console + sign-out; Auditor UI write-gating via `AuthState.CanWrite`.
- ✅ **Test hook** — `Auth:TestBypass` authenticates every request as Administrator so the existing 44 API
  tests run unchanged; auth-specific tests (`AuthApiTests`) flip it off to exercise real JWT. `dotnet test`
  green (API 48, Application 99, Client 6, Domain 67).
- ⚠️ **Prod follow-ups:** set a real `Jwt:SigningKey` and `Seed:AdminPassword` from a secret; wire email
  delivery of the invite link. **Committed DB secret in appsettings.json still needs rotating** (user
  deferred).

### Sample-data → API migration, Phase M6: shell chrome + project removal (previous change)
Completes the migration — no page renders sample data any more.
- ✅ **Inductions builder** — the "applies to site" dropdown now loads from `ISiteService` (off
  `IFormSampleDataService`). The video/quiz/publish steps remain explicit UI placeholders (no template-
  authoring backend exists; publishing arbitrary inductions is out of scope).
- ✅ **MainLayout shell chrome retired.** Current user → `ICurrentUserService`; top-bar notifications →
  `IExpiryQueryService` (real upcoming expiries, SF-9); nav/route inventory, platform switcher and
  environment badge relocated to `Tedwren.Client/Services/ShellChrome.cs` (fixed app config, not tenant
  data). `IShellSampleDataService` / `IListSampleDataService` no longer used.
- ✅ **`Tedwren.UiComponents.SampleData` project deleted** — removed from the solution, the client project
  reference, `Program.cs` registrations and `_Imports`. Presentation view models it used to own
  (`KpiTile`, the compliance-overview VM) now live in `Tedwren.Client/Services`.
- ✅ Docs updated (CLAUDE.md architecture table + data-source note; Slug.cs comment). Whole solution builds;
  `dotnet test` green (API 44, Application 99).

### Sample-data → API migration, Phases M4 (settings) + M5 (permits) (previous change)
- ✅ **M4 — general settings persistence.** `ISettingsService` (`GetForCompanyAsync`, `SaveForCompanyAsync`) +
  `SettingsService` (returns per-company settings, defaults seeded from company name when unset) +
  `/api/settings/{companyId}` (GET/PUT) + `ApiSettingsService`. Per-company JSON row: `CompanySettings` table
  (migration `013`, SqlServer + Postgres), Dapper `SettingsRepository`, in-memory double (singleton), EF
  `CompanySettingsRecord`. `SystemConfiguration` now loads and **saves** general settings via the API (the
  "not yet persisted" caveat is gone), off `IFormSampleDataService`.
- ✅ **M5 — permits backend.** New `Permit` entity + `PermitStatus` enum; `IPermitService`
  (`CreateAsync`, `ListForCompanyAsync`) + `PermitService`; `/api/permits` (POST + `company/{id}`) +
  `ApiPermitService`. `Permits` table (migration `014`, SqlServer + Postgres), Dapper `PermitRepository`,
  in-memory double (singleton), EF `PermitRecord`. `Permits` page now **persists** issued/draft permits
  (company via `ITenantState`) and **lists** them in a `DataTable`.
- ✅ Tests: `SettingsAndPermitApiTests` (API, 3); whole solution builds; `dotnet test` green (API 44,
  Application 99).
- ⏳ **Remaining:** M6 — Inductions config page + retire `IShellSampleDataService` chrome (MainLayout
  nav/platforms/environment/notifications/user) — the last sample-data dependency.

### Sample-data → API migration, Phase M3: dashboard aggregation (previous change)
Adds the dashboard aggregation read model and migrates the Dashboard and Compliance pages onto it.
- ✅ **Dashboard aggregation** — `IDashboardService` (`GetSummaryAsync`, `GetComplianceAsync`) +
  `DashboardService`, composing company/engagement/qualification-card repositories + `ISiteService` +
  `IExpiryQueryService` (no new store; reuses `ComplianceRollup` for SF-8). DTOs `DashboardSummaryDto`,
  `DashboardKpisDto`, `ComplianceBreakdownDto`, `SiteRiskRowDto`.
- ✅ **API + client** — `/api/dashboard` (summary) + `/api/dashboard/compliance` + `ApiDashboardService`,
  registered both ends. New client helper `ComplianceOverviewView` maps the breakdown to the donut/legend VM
  (theme-token colours; no literals in pages).
- ✅ **Pages migrated:** `Dashboard` — KPIs (companies/operatives/sites/compliance%/upcoming expiries),
  compliance donut, site-risk heatmap all from `/api/dashboard`; upcoming expiries from `IExpiryQueryService`
  (SF-9); recent activity from `IAuditService` (SF-20). `Compliance` — overview donut from
  `GetComplianceAsync`. Both fully off `IDashboardSampleDataService`.
- ✅ Honest-data note: the heatmap now shows Site / Operatives / Compliance% / Status (the sample
  Compliant/Expiring/At-risk per-site breakdown has no domain source — there is no person→site compliance
  link yet); KPI sparklines/trends are dropped (no historical series stored).
- ✅ Tests: `DashboardApiTests` (API, 2); whole solution builds; `dotnet test` green (API 41, Application 99).
- ⏳ **Remaining sample-data pages:** SystemConfiguration settings (M4); Permits issue-flow backend (M5);
  Inductions config + retire `IShellSampleDataService` chrome (M6).

### Sample-data → API migration, Phase M2: workforce read model (previous change)
Adds the org-wide workforce read model and migrates the operative-facing pages onto it.
- ✅ **Workforce read model** — `IWorkforceService` (`ListOperativesAsync`, `GetOperativeBySlugAsync`) +
  `WorkforceService`, composing existing company/engagement/person/qualification-card/decision
  repositories & services (no new store — reuses `ComplianceRollup` for SF-8 state). DTOs
  `OperativeListItemDto` / `OperativeDetailDto` / `OperativeQualificationDto` / `OperativeHistoryDto`.
- ✅ **API + client** — `/api/workforce` (list + `/{slug}`) + `ApiWorkforceService`, registered both ends.
- ✅ **Pages migrated:** `Workforce` (register → `ListOperativesAsync`), `OperativeDetail` (→
  `GetOperativeBySlugAsync`; overview now shows only domain-backed fields — trade, employer, phone,
  qualifications, and site-entry history — the sample DoB/NI/email/primary-site are dropped as the model
  doesn't hold them), `Permits` (permit types → reference, sites → `ISiteService`, operatives → workforce —
  fully off `IFormSampleDataService`), `MainLayout` command-palette search index (companies/operatives/sites
  → real APIs, off `IListSampleDataService`).
- ✅ **AddOperative real persistence** — direct entry now creates a real engagement via
  `IOrganisationService.AddOperativeAsync` (added an Employer/company selector; SF-1/SF-2, surfaces the SF-2
  duplicate refusal). Self-service link path stays a demo (no backend yet).
- ✅ Tests: `WorkforceApiTests` (API, 2); whole solution builds; `dotnet test` green (API 39, Application 99).
- ⏳ **Remaining sample-data pages:** Dashboard + Compliance overview (M3); SystemConfiguration settings (M4);
  Permits issue-flow backend (M5); Inductions config (M6). `IShellSampleDataService` still supplies
  MainLayout chrome (nav/platforms/environment/notifications/user) — retire in M6 cleanup.

### Sample-data → API migration, Phase M1: foundations (previous change)
Begins moving the pages that still render `UiComponents.SampleData` onto real database-backed services.
Phase M1 delivers the shared foundations and the first page migrations:
- ✅ **Current-operator service** — `ICurrentUserService` + `CurrentUserService` (configured/dev identity via
  `CurrentUserOptions`, until an auth phase) + `/api/me` + `ApiCurrentUserService`. Replaces the sample shell
  user for the SUB-22 role check.
- ✅ **Reference-data lookup (DB-backed)** — `IReferenceDataService` + `/api/reference/{listKey}` +
  `ApiReferenceDataService`, `ReferenceValues` table (migration `012_reference_data.sql`, SqlServer +
  Postgres, idempotent seed) + Dapper repo + in-memory double + EF `ReferenceValueRecord`. Keys: company
  types, trades, permit types, regions (`ReferenceListKeys`).
- ✅ **Decisions client wiring** — added `ApiDecisionService` + registered `IDecisionService` (endpoint +
  backend already existed; only client binding was missing, R10).
- ✅ **Pages migrated off sample data:** `CompliancePacks` (role → current-user), `AddCompany` (company
  types/trades → reference), `AddOperative` (trades → reference, sites → `ISiteService`), `Onboarding`
  (trades → reference).
- ✅ Tests: `ReferenceAndIdentityApiTests` (API, 4), `ReferenceDataServiceTests` (Application, 2); whole
  solution builds; `dotnet test` green (API 37, Application 99).
- ⏳ **Remaining sample-data pages (later M-phases):** Workforce + OperativeDetail + MainLayout search
  (M2 workforce read model); Dashboard + Compliance overview (M3 aggregation); SystemConfiguration general
  settings (M4); Permits (M5 backend); Inductions config (M6). `AddOperative` real persistence lands with
  the M2 workforce write-model (needs a company target the form doesn't yet collect).

### Remove Mock mode + fix client↔API connectivity (previous change)
- ✅ **Removed runtime Mock mode.** The client always calls the Web API; the API always uses the database.
  Deleted `ClientDataSourceMode` and all 12 `ClientMock*Service` classes; client `Program.cs` registers only
  the `Api*` services. Renamed `DataSourceMode.Mock` → `InMemory` (test-only), defaulted `BackendOptions.Mode`
  to `Database`, and gated the API's in-memory registrations behind `Mode == InMemory` (selected only by the
  `Tedwren.Api.Tests` module initializer, so the suite still runs without SQL Server).
- ✅ **Fixed CORS / API base URL.** Client `Api:BaseUrl` → `https://localhost:7296` (the API's real https
  port); API `Cors:AllowedOrigins` → the client's real origins `https://localhost:11379` / `http://localhost:11380`.
  These previously pointed at unrelated default ports (5001/5000), causing `NetworkError`/CORS failures.
- ✅ Inlined the few demo constants that four not-yet-migrated sample pages (SiteGate, CompliancePacks,
  InductionRecords, TimeAndAttendance) had borrowed from the deleted mock services.
- ❗ **Follow-up:** ~12 pages still render `UiComponents.SampleData` directly (Dashboard, Workforce,
  OperativeDetail, Compliance, Permits, etc.) — migrating each to real API data is a separate project.
  Also `/api/decisions` has no client caller (server-side write path only).

### Organisation onboarding wizard (previous change)
- ✅ **Onboarding wizard (branches by org type: Subcontractor / Main Contractor).** New `OnboardingLayout`
  (plain centered shell modelled on `RecipientLayout`) + `/onboarding` wizard (`Onboarding.razor` +
  `OnboardingModel`) built from the existing `TedwrenStepper`/Forms suite/`DashboardCard`. Steps: org type →
  company details → first administrator → sites → (Sub) operatives + insurances/accreditations (SUB-4 default
  doc types) / (MC) seeded induction template (MC-3). `Finish()` sequences the service calls with the company
  as the anchor and best-effort child steps (partial-failure summary; R18-safe copy).
- ✅ **First-run + in-app triggers.** `MainLayout` redirects to `/onboarding` when the database has no
  companies (fails open; mock mode is never empty). "Add client" button on the Organisation page reaches the
  wizard for pre-launch testing.
- ✅ **Dynamic tenant (replaces the static `TenantContext`).** New `ITenantState`/`TenantState`
  (localStorage-backed, falls back to the seed company id); the wizard sets the newly-created company as the
  active tenant (R15). All prior `TenantContext.CurrentCompanyId` readers migrated.
- ✅ **Invite carries `CompanyId` (R15).** `InviteUserRequest` gains `CompanyId`; `UserService` attaches the
  new user to it instead of the hard-coded owner company — so onboarding's first admin lands on the new org.
- ✅ **Company documents persistence (SUB-4).** New `CompanyDocument` entity, `ICompanyDocumentRepository`
  (in-memory + Dapper both dialects), `CompanyDocuments` table (`011_company_documents.sql` + EF
  `AddCompanyDocuments` migration); `IOrganisationService.AddCompanyDocumentAsync` and documents surfaced on
  the company detail with an expiry-derived state. (File bytes not yet stored — metadata only.)
- ✅ **Induction template seeding (MC-3/SF-12).** `IInductionService.CreateDefaultTemplateAsync` clones the
  shipped default into a company; `/api/inductions/templates` POST + client impls.
- ✅ Tests: invite `CompanyId` honoured; company-document add/read + expiry state; induction-template seed;
  API document + template endpoints; skippable Dapper `CompanyDocumentRepository` integration test.

### UX completeness pass — user management, UI defect closure & EF migrations tooling (previous change)
- ✅ **Console user management (SF-20, SF-23, Q2).** New full vertical: `User` entity + `UserStatus` enum
  (reusing the existing `AccessRole`); `IUserService` + DTOs; `UserService` (invite with duplicate-email
  guard, edit, suspend/reactivate — access withdrawn, never deleted, keeping audit history); seeded
  in-memory store/repository; Dapper `UserRepository` + `010_users.sql` (SQL Server + PostgreSQL);
  `/api/users` endpoint group; client `ApiUserService` + interactive `ClientMockUserService`; **Users list**
  page, **InviteUser** form and **UserDetail** (inline edit + suspend/reactivate). Replaces the previous
  invite-only `/users` form. Unit + API tests added.
- ✅ **Sites create flow (SF-6/14/25/26).** New `/sites/add` page (company picker, optional boundary,
  no-compound & dispersed toggles); wired the previously dead "Add site" button.
- ✅ **Dashboard actions.** Date-range button now opens a working period menu; **Export** button downloads
  the site-risk heatmap as CSV via a reusable browser-download JS helper (`tedwrenDownload`). Both were
  dead no-ops.
- ✅ **SiteDetail migrated to `ISiteService`** (overview + real properties; no invented workforce data).
- ✅ **Audit "Export CSV" button** on `AuditLog.razor` wired to the existing `IAuditService.ExportCsvAsync`
  (was a logged follow-up); **Time & Attendance CSV export** now performs a real browser download.
- ✅ **EF Core migrations tooling (alongside Dapper).** EF authors/applies schema (DDL); Dapper stays for
  queries (DML). `Tedwren.DataAccess/Ef`: flat `SchemaRecords` mirroring every table, `TedwrenDbContext`
  (identifiers lower-cased on PostgreSQL to match the Dapper repos' unquoted SQL), design-time factory
  driven by `TEDWREN_EF_PROVIDER`/`TEDWREN_EF_CONNECTION`. Both provider models validated via
  `dotnet ef dbcontext info`; no migrations generated (operator runs them). Full command set in
  `docs/ef-migrations.md`. NuGet audit scoped to direct deps (unactionable transitive advisory).

### Phases 1–6 — UI/UX foundation over mock data (pre-existing, on `main`)
- ✅ **Phase 1 — Shell & theme.** Solution scaffold, `tokens.css` design tokens, MudBlazor theme,
  application shell (`MainLayout`, `AppSidebar`, `AppTopBar`).
- ✅ **Phase 2 — Dashboard.** Card/data-display/chart components (`KpiCard`, `DashboardCard`,
  `DonutStat`, `TrendSparkline`, `ExpiryList`, `LegendList`, `StatusPill`, `RiskChip`) and the
  dashboard rebuilt from them.
- ✅ **Phase 3 — Lists.** Generic `DataTable<TItem>`, `EmptyState`, and the list pages
  (Organisation, Workforce, Sites, Compliance, Audit Log) with search + filter.
- ✅ **Phase 4 — Forms.** Forms suite (`FormField`, `TedwrenTextField/Select/Autocomplete/Toggle/
  DateRangePicker/FileUpload/Stepper`, `FormSection`, `FormActions`, `InlineValidationMessage`,
  `BannerAlert`) + add/invite/stepper flows.
- ✅ **Phase 5 — Feedback/state.** `ConfirmDialog`, `LoadingSkeleton`, polish and UI fixes.
- ✅ **Phase 6 — Detail pages.** Entity detail pages (company, operative, site) + `DetailHeader`,
  `Flyout`, `NotificationsMenu`, `KeyValueList`.
- Mock data behind five interfaces (`IShell/IDashboard/IList/IForm/IDetailSampleDataService`).

### Phase 7 — Governance & backend scaffolding (previous change)
- ✅ Reset designated branch onto latest `origin/main` (was stale at PR #2; now at PR #10).
- ✅ Stored source-of-truth PRD (`docs/TedwrenPRDv6_4.docx`) and the phased plan
  (`docs/plan-and-scope.md`) in the solution.
- ✅ Added **`CLAUDE.md`** (PRD v6.4 declared authoritative; architecture, standards, the
  mock↔database switch) and this **`TODO.md`**.
- ✅ Added and wired backend + test projects (all net10.0): `Tedwren.Abstractions`,
  `Tedwren.Domain`, `Tedwren.Application`, `Tedwren.DataAccess` (Dapper + Microsoft.Data.SqlClient
  + Npgsql referenced), `Tedwren.Api` (CORS, `/health`), and `*.Tests` (xUnit). All added to
  `Tedwren.sln`.
- ✅ Introduced the `DataSource` mock↔database switch (`BackendOptions`, `DataSourceMode`,
  `DatabaseProvider`); API + client `appsettings.json` default to `Mock` — **no behaviour change**.
- ✅ Fixed a pre-existing compile error: `ExpiryList.razor` used a `switch` expression whose
  `< 0 =>` relational-pattern arm was misparsed by the Razor generator as a markup transition;
  rewritten as an equivalent `if`-chain (behaviour unchanged). Solution now builds with 0 errors.
- ✅ Resolved the one newly-introduced warning (NU1903: vulnerable transitive `Microsoft.OpenApi`
  2.0.0) by pinning a patched `Microsoft.OpenApi` 2.11.0 in `Tedwren.Api`.
- ✅ Verified: full solution builds (0 errors), all tests pass, `GET /health` returns the resolved
  data-source mode.

---

### Phase 8 — Data-access seam on the first slice (SF-1, SF-2, SF-3) (previous change)
- ✅ **Domain** (`Tedwren.Domain`): `PhoneNumber` value object (settle-once mobile normaliser, SF-1/Q9);
  `Company`, `Person`, `Engagement` entities; `EngagementStatus` enum.
- ✅ **Abstractions**: `IOrganisationService` + organisation DTOs (`CompanySummary`, `CompanyDetailDto`,
  `CreateCompanyRequest`, `AddOperativeRequest`/`Result`, …); neutral `ComplianceState`; canonical
  `Slug`; `ClientDataSourceMode`.
- ✅ **Application**: single store-agnostic `OrganisationService` (SF-1 create-or-reuse person by phone;
  SF-2 refuse duplicate engagement naming the existing; SF-3 archive/reactivate; R15 company-scoped);
  repository interfaces + seeded in-memory repositories (API mock mode); DI extensions.
- ✅ **DataAccess** (Dapper, dual-engine): `IDbConnectionFactory`, `ISqlDialect`
  (SqlServer/Postgres), `RepositoryBase`, Company/Person/Engagement repositories, `MigrationRunner`
  with embedded idempotent SQL for both engines (unique index on `Persons.PhoneNumber` = SF-1, on
  `Engagements(CompanyId, PersonId)` = SF-2). SQL is ANSI-portable across both engines.
- ✅ **API**: `/api/organisation` endpoints; composition root registers one `OrganisationService` and
  chooses in-memory repos (`DataSource=Mock`) or Dapper repos + migrations (`DataSource=Database`).
- ✅ **Client**: `ApiOrganisationService` (HTTP) and `ClientMockOrganisationService` (wraps sample data,
  keeps Mock mode visually identical); `DataSource:Mode` switch in `Program.cs`; Organisation list,
  detail and add-company pages migrated to the async `IOrganisationService` + DTOs (no other page
  touched). Client default remains `Mock` — **no regression**.
- ✅ **Tests**: `PhoneNumberTests` (normalisation/equality); `OrganisationServiceTests` (SF-1/2/3, R15);
  `Tedwren.Api.Tests` (WebApplicationFactory, mock mode — GET/POST/409); `OrganisationRepositoryTests`
  (SQL Server integration, skip-guarded on `TEDWREN_TEST_SQLSERVER`, purpose-created rows). Result:
  **26 passed, 1 skipped** (the DB integration test — no SQL Server in this environment).
- ✅ Verified live: `GET /api/organisation/companies` returns seeded data; SF-1 (`07700 900123` and
  `+447700900123` → one person) and SF-2 (duplicate → HTTP 409 naming the existing) proven over HTTP.

### Phase 9 — Qualification cards & competency (SF-5–SF-8, SF-10–SF-12) (previous change)
- ✅ **Domain**: `QualificationType` (SF-12), `QualificationCard` with pure `GetStatus(asOf, window)`
  computing currency from expiry (SF-8) + supersede fields (SF-10), `TradeQualificationRequirement`
  (SF-11); enums `CardVerificationState` (SF-7), `CardStatus` (SF-8), `CardCaptureSource` (SF-5).
- ✅ **Abstractions**: neutral `CardVerificationState`; `QualificationDtos` (`QualificationTypeDto`
  incl. `HeldBy`; `QualificationCardDto` with server-computed state/labels; capture/confirm/renew
  requests; `CompetencyShortfallDto`); `IQualificationService`.
- ✅ **Application**: `DefaultQualificationLibrary` (canonical SF-12 seed + illustrative SF-11 trade
  requirements, single source of truth); store-agnostic `QualificationService` (SF-5 capture →
  needs-review, never auto-confirmed; SF-6 confirm records who/when; SF-8 status computed server-side;
  SF-10 renew supersedes + retains; SF-11 shortfall; SF-12 library + `HeldBy`); repo interfaces +
  seeded in-memory store/repos; DI `AddQualificationCore` / `AddInMemoryQualificationStore`.
- ✅ **DataAccess**: Dapper `QualificationType/Card/TradeRequirement` repositories (ANSI-portable);
  `002_qualifications.sql` for both engines; idempotent `QualificationLibrarySeeder` (keeps the C#
  library as the source); registrations extended.
- ✅ **API**: `/api/qualifications` endpoints (types, person cards, capture, confirm, renew, shortfall);
  composition root registers the qualification service + store/seeder alongside organisation.
- ✅ **Client**: `ApiQualificationService` (HTTP) + `ClientMockQualificationService` (wraps sample
  library, browser-safe stable id — no `System.Security.Cryptography`); `DataSource:Mode` switch;
  **Compliance** qualification-library table migrated to `IQualificationService` (no other page touched).
- ✅ **Tests**: `QualificationCardStatusTests` (SF-8 boundaries); `QualificationServiceTests`
  (SF-5/6/8/10/11/12 + `HeldBy`); `QualificationApiTests` (types + capture→confirm flow, mock mode);
  `QualificationRepositoryTests` (SQL Server integration, skip-guarded). Result: **41 passed, 2 skipped**.
- ✅ Verified live (mock mode): `GET /api/qualifications/types` returns the 7-type default library;
  capture → "Read — not checked" (SF-5, not auto-confirmed) → confirm → "Checked" by the named person (SF-6).

### Phase 10 — Expiry engine, warning schedule & job heartbeat (SF-9, SF-21, SUB-5, R12) (previous change)
- ✅ **Domain**: `Notifications/ExpiryWarningStage` (60/30/7/0/−1) + pure `ExpiryWarningSchedule.DueStages`
  (catch-up-safe, SF-9), `NotificationChannel`, `ExpiryNotification` (idempotency log row); `Jobs/JobRun`
  + `JobRunStatus` (SF-21/R12).
- ✅ **Abstractions**: `ISmsSender`, `IEmailSender`, `INotificationOutbox` (+`OutboxMessage`); expiry
  contracts (`ExpiryScanResultDto`, `DigestResultDto`, `HeartbeatResultDto`, `UpcomingExpiryDto`,
  `JobRunDto`); `IExpiryQueryService`.
- ✅ **Application**: `ExpiryWarningJob` (SF-9: due stages → worker SMS + each engaging company's admin
  email, recorded so twice-a-day ≠ twice), `WeeklyDigestJob` (SUB-5, cards now; company docs when SUB-4
  lands), `ExpiryQueryService`; `JobRunner` (records Running→Succeeded/Failed + counts, SF-21) and
  `JobHeartbeatMonitor` (emails ops on silent stop, R12); stub `Outbox{Sms,Email}Sender` +
  `NotificationOutbox`; new repo interfaces + in-memory store/repos; extended card/engagement repos with
  `GetCurrentWithExpiryAsync`/`GetActiveByPersonAsync`; DI `AddExpiryCore`/`AddInMemoryExpiryStore`.
- ✅ **DataAccess**: Dapper `NotificationLogRepository` (idempotent exists/add), `JobRunRepository`
  (ANSI `OFFSET…FETCH` paging); `003_notifications.sql` for both engines (**unique index on
  (CardId,Stage,Channel,Recipient)** = SF-9 idempotency) + `JobRuns`; registrations extended.
- ✅ **API**: `ExpirySchedulerHostedService` (`BackgroundService`, gated by `Jobs:SchedulerEnabled`);
  `/api/jobs/{expiry-scan,weekly-digest,heartbeat-check,runs,outbox}` + `/api/expiry/upcoming`;
  composition root wires the engine + store/scheduler.
- ✅ **Client**: none this phase (engine + endpoints are the increment); a Dashboard/Notifications panel
  over `/api/expiry` is a logged follow-up. `Mock` stays default — no page regression.
- ✅ **Tests**: `ExpiryWarningScheduleTests` (SF-9 boundaries/catch-up); `ExpiryWarningJobTests`
  (worker+admin at 30 days; second run same day sends nothing), `WeeklyDigestJobTests`,
  `JobHeartbeatMonitorTests`; `JobApiTests` (scan idempotency + heartbeat, scheduler disabled);
  skip-guarded `NotificationLogRepositoryTests`. Result: **55 passed, 3 skipped**.
- ✅ Verified live (mock): card 20 days out → scan sent **4** (SMS + email at the 60- and 30-day stages);
  second scan sent **0** (SF-9 idempotent); runs recorded Succeeded (SF-21); heartbeat flagged only the
  never-run weekly-digest (R12).

### Phase 11 — Sites, boundaries & dispersed schemes (SF-6, SF-14, SF-25, SF-26) (previous change)
- ✅ **Domain**: `Geofence` value object (centre + radius, haversine `Contains`/`DistanceMetresTo`, SF-14);
  `Site` (owner company, boundary, `HasCompound` for SF-25, `IsDispersed` for SF-26) and `SiteProperty`
  (own address + geofence, SF-26).
- ✅ **Abstractions**: neutral `RiskState`; site DTOs (`SiteSummary` incl. `IsDispersed`/`PropertyCount`,
  `SiteDetailDto`, `SitePropertyDto`, `GeofenceDto`, `CreateSiteRequest`, `AddSitePropertyRequest`);
  `ISiteService`.
- ✅ **Application**: store-agnostic `SiteService` (record sites unlimited/never billed SF-6; boundary
  round-trip SF-14; adding a property marks the scheme dispersed SF-26; compliance `Pending` until
  attendance exists); repo interfaces + seeded in-memory store (a boundaried site + a dispersed
  no-compound retrofit scheme); DI `AddSiteCore`/`AddInMemorySiteStore`.
- ✅ **DataAccess**: Dapper `SiteRepository` (boundary as 3 nullable columns) + `SitePropertyRepository`;
  `004_sites.sql` for both engines; registrations extended.
- ✅ **API**: `/api/sites` endpoints (list, detail, record, add-property); composition root wires the
  service + store alongside the others.
- ✅ **Client**: `ApiSiteService` (HTTP) + `ClientMockSiteService` (wraps sample data) + `RiskStateView`;
  `DataSource:Mode` switch; **Sites** list migrated to `ISiteService` (same columns → mock visually
  identical; SiteDetail migration deferred). `Mock` stays default — no regression.
- ✅ **Tests**: `GeofenceTests` (SF-14 in/out + validation); `SiteServiceTests` (SF-6 unlimited, SF-14
  boundary, SF-25/26 dispersed on add-property); `SiteApiTests` (dispersed seed, no-compound detail,
  create+add-property); skip-guarded `SiteRepositoryTests`. Result: **70 passed, 4 skipped**.
- ✅ Verified live (mock): `GET /api/sites` shows the dispersed no-compound scheme (2 geofenced properties);
  creating a scheme and adding a property over HTTP marks it dispersed with nothing installed on site.

### Phase 17 — Site-entry decision, competency cover & muster (MC-8–MC-14, MC-28, R2, R3, R10, R14) (previous change)
- ✅ **Domain**: pure `SiteEntryPolicy` — fail-closed admission (admitted only if no check failed, R2) +
  actionable block reason from failed checks (MC-9). Reuses the Phase-13 `SiteEntryDecision`/`DecisionCheck`
  store for the record (R10).
- ✅ **Abstractions**: site-entry DTOs (`DecideEntryRequest` with optional manager override, `EntryDecisionResultDto`
  with admission/reason/checks/decision-id/**elapsed-ms** R14, muster with data-age + competency cover);
  `ISiteEntryService`.
- ✅ **Application**: `SiteEntryService` — the **five checks against current data** (registered / not-elsewhere /
  induction valid / cards in date & confirmed / RAMS-where-held, R3), each wrapped so any error becomes a failed
  check (**fail-closed**, R2); RAMS recorded **NotRun** when the module isn't held (R10); day-only manager override
  (MC-11); self-reconstructing record written through `IDecisionService` (R10); timed against the **<3s** budget
  (R14); plus the **muster** — on-site people resolved to property (MC-12), competency cover with holder count
  (MC-13), and a generated timestamp for offline data-age (MC-14). Added `IInductionSessionRepository.
  GetLatestPassedForPersonAsync` (in-memory + Dapper). DI `AddSiteEntryCore` (aggregates existing slices — no new
  store).
- ✅ **API**: `/api/site-entry/decide` (always 200 with the full result incl. every check, MC-28 needs no compound)
  and `/api/site-entry/muster/{siteId}`; composition root wires the service.
- ✅ **Client**: `ApiSiteEntryService` + self-contained `ClientMockSiteEntryService` (three demo operatives — clear
  / expired-card / no-induction) behind `ISiteEntryService`; new **Site Gate** page (`/site-gate`, added to nav) —
  check-entry with the five-check breakdown + block reason + manager-override, and a live muster with competency
  cover and data-age — identical across the mock↔API switch.
- ✅ **Tests**: `SiteEntryPolicyTests` (fail-closed rule, block reason); `SiteEntryServiceTests` (clear admit +
  R10 record, expired-card block reason, unregistered block, **error→fail-closed R2**, override MC-11, muster
  competency cover — against real in-memory repos); `SiteEntryApiTests` (blocked-with-all-checks + RAMS NotRun +
  timing, override admits, muster). Result: **165 passed, 11 skipped**.
- ✅ Verified live (mock): an unknown worker is **blocked** with all five checks (Registered/Induction failed,
  RAMS **NotRun** — R10) and a specific reason (MC-9) in **16 ms** (R14); the decision **reconstructs from the
  store** with all five checks (R10); a manager override admits and is flagged (MC-11); the muster returns a
  generated timestamp (data-age, MC-14) and competency cover (MC-13). *Main Contractor MVP complete (product
  saleable). Shared foundation + both MVPs delivered.*

### Phase 16 — Digital induction & consent (MC-1–MC-7, MC-15, MC-20, R5)
- ✅ **Domain**: `InductionTemplate` (configurable steps + quiz + pass mark + validity, MC-3); `InductionStep`
  (data-driven capture, MC-3/MC-4); `InductionQuizQuestion` (**correct answer server-side only**, R5); pure
  `InductionQuiz.Score` (server-side scoring, R5); `InductionSession` (stateful lifecycle, MC-1–MC-7, consent
  MC-20, `IsValid`); enums `InductionStatus` (InProgress/Failed/Passed/Superseded) and `InductionStepKind`.
- ✅ **Abstractions**: device-facing DTOs — `InductionSessionDto`/`InductionQuizQuestionDto` carry **prompt +
  options but no answers** (R5); start/step/quiz/finalize/reset requests; `QuizResultDto` (score only, no
  answers); `InductionSummaryDto`; `IInductionService`.
- ✅ **Application**: `InductionService` (start with re-induction supersede MC-7; step gating MC-4; **server-side
  quiz scoring** R5; failed-attempt handling + manager reset MC-6; completion reference + configurable validity
  MC-5/MC-7; separate optional consent MC-20); `DefaultInductionTemplate` seed (answers held server-side);
  template + session repo interfaces + seeded in-memory store; DI helpers.
- ✅ **DataAccess**: Dapper `InductionTemplateRepository` (steps/quiz as JSON) + `InductionSessionRepository`
  (completed-steps JSON, supersede query); `009_inductions.sql` for both engines; `InductionTemplateSeeder`
  (idempotent); registrations extended; seeder runs in DB mode.
- ✅ **API**: `/api/inductions` — templates, start, device session, complete-step, **server-scored quiz**,
  finalize (409 when required steps/quiz incomplete, MC-4), manager reset, company records. Composition root
  wires the service + seeded store + DB-mode seeder.
- ✅ **Client**: `ApiInductionService` + self-contained `ClientMockInductionService` (holds answers privately so
  R5 holds; full in-proc flow) behind `IInductionService`; new **Induction Records** page (`/induction-records`,
  added to nav) — completions, validity, consent flag, and manager Reset for failed inductions — identical across
  the mock↔API switch.
- ✅ **Tests**: `InductionQuizTests` (R5 scoring); `InductionServiceTests` (**JSON-serialised device session
  asserts no `CorrectOptionIndex`**, fail→pass scoring, MC-4 gate throws, completion ref/validity/consent, reset,
  supersede); `InductionApiTests` (wire response has no answers, 409 gate → full flow → completion over HTTP);
  skip-guarded `InductionRepositoryTests`. Result: **151 passed, 11 skipped**.
- ✅ Verified live (mock): the started-session wire response contains **0 occurrences of the correct-answer field**
  (R5); finalize before ready → 409 (MC-4); a wrong quiz scores 0/fails, the correct quiz scores 3/passes
  (server-side); finalize issues a completion reference (`IND-…`) with 365-day validity and stores consent. *Main
  Contractor MVP begins.*

### Phase 15 — Compliance pack (SUB-13–SUB-26, R7, R8, R9)
- ✅ **Domain**: `CompliancePack` (fixed-at-send snapshot with token + passcode hash + expiry + status, R7/R8/R9)
  with `EffectiveStatus`/`IsAccessible` (expiry & revoke gate, SUB-18/SUB-21); `PackSubject`/`PackCard` (frozen
  snapshot, R7); `PackAccessEvent` (open/download tracking, SUB-20); pure `PackReadiness` (expired = blocking,
  expiring-soon = warning, SUB-14); enums `PackStatus` (Active/Revoked/Superseded/Expired) and `PackAccessKind`.
- ✅ **Abstractions**: pack DTOs (build request/result with readiness issues, list item with tallies, recipient
  view, access result); `ICompliancePackService`.
- ✅ **Application**: `CompliancePackService` (snapshots each operative's cards via `IQualificationService` at
  send, R7; **gates sending on acknowledged blocking issues**, SUB-14; token+passcode+expiry authorisation with
  open/download tracking, R8/SUB-20; revoke SUB-21; **re-issue supersedes** the prior pack, R7); `PackPasscode`
  (salted SHA-256, never stores plaintext, SUB-18), `PackToken` (256-bit opaque, no guessable URLs, R9),
  `PackComposer` (one snapshot → CSV/ZIP/PDF, identical content, SUB-16); a framework-only **`PdfWriter`**
  (valid multi-page PDF via hand-built xref, no dependency) alongside the Phase-14 `XlsxWriter`; in-memory
  store/repo; DI helpers.
- ✅ **DataAccess**: Dapper `CompliancePackRepository` (snapshot as JSON; access events table; token unique
  index, R9); `008_compliance_packs.sql` for both engines; registration extended.
- ✅ **API**: `/api/packs` — readiness preview, build (409 carrying issues when unacknowledged, SUB-14), company
  list, revoke; account-free recipient routes `view`/`download.{csv,zip,pdf}` gated by token+passcode (403 on
  refusal, R8/R9). Composition root wires the service + store.
- ✅ **Client**: `ApiCompliancePackService` (HTTP) + self-contained `ClientMockCompliancePackService` (fabricates
  operatives incl. an expired card so the readiness gate is demonstrable) behind `ICompliancePackService`; new
  **Compliance Packs** page (`/compliance-packs`, added to the nav) — build with readiness banner + acknowledge,
  status pills, open/download tallies, revoke — identical across the mock↔API switch.
- ✅ **Tests**: `PackReadinessTests` + `CompliancePackTests` (SUB-14 severities, SUB-18/21 accessibility);
  `CompliancePackServiceTests` (readiness gate, passcode-gated view + tracking, revoke, supersede, CSV/valid-zip
  ZIP/valid `%PDF` — SUB-16); `CompliancePackApiTests` (capture an expired card → 409 gate → ack → send → view →
  PDF download → revoke blocks, over HTTP); skip-guarded `CompliancePackRepositoryTests`. Result: **140 passed,
  10 skipped**.
- ✅ Verified live (mock): building a pack for an operative with an expired card returns 409 with the blocking
  issue; acknowledging sends it with a token + passcode; the recipient view needs the passcode (bad token → 403);
  a downloaded PDF is a structurally valid document (`%PDF` header, xref, `startxref`, `%%EOF`); revoke makes the
  link 403. *Subcontractor MVP complete (product saleable).*

### Phase 14 — Timesheets (SUB-7–SUB-12, SUB-27, MC-24, R16, R18)
- ✅ **Domain**: `Timesheet` (first-class, stateful, approvable header — SUB-7/SUB-8), `TimesheetEntry`
  (append-only line; a correction is a new line referencing the original, R16), `TimesheetWorkflow` (pure
  lifecycle guards); enums `TimesheetStatus` (Draft/Submitted/Approved/**Returned** — never "denied", R18/SUB-12)
  and `ApprovalScope` (Line/Site/Project/All, SUB-9).
- ✅ **Abstractions**: neutral `TimesheetState`/`ApprovalScope`; timesheet DTOs (summary for MC-24, full with
  folded effective lines, operative hours for SUB-27, correct/approve/return requests); `ITimesheetService`.
- ✅ **Application**: pure `TimesheetHoursCalculator` (pairs sign-in/out per site/day from the append-only
  attendance log, SUB-7); reusable `Export/` helpers — `TabularSheet`, `CsvWriter`, and a **framework-only
  `XlsxWriter`** (valid .xlsx via `ZipArchive`, numeric hours cells, no external dependency) for SUB-10;
  `TimesheetService` (roll-up get-or-create, submit/approve/return with recorded who/when/scope, corrections
  folded into effective hours while originals are retained, worker hours, CSV+Excel export); repo interface +
  seeded in-memory store/repo; DI `AddTimesheetCore`/`AddInMemoryTimesheetStore`. Extended `IAttendanceRepository`
  with `GetByPersonInRangeAsync` (in-memory + Dapper).
- ✅ **DataAccess**: Dapper `TimesheetRepository` (stateful header + append-only entries, ANSI-portable);
  `007_timesheets.sql` for both engines (Timesheets with unique (Company,Person,Week) index + TimesheetEntries);
  registration extended.
- ✅ **API**: `/api/timesheets` — company-week list (MC-24), get, operative get-or-create + hours (SUB-27),
  submit/approve/return/correct, and CSV/`.xlsx` export at timesheet and company-week level (SUB-10). Composition
  root wires the service + seeded store.
- ✅ **Client**: `ApiTimesheetService` (HTTP) + self-contained `ClientMockTimesheetService` (fabricates the same
  demo week the API seeds, in-proc workflow) behind `ITimesheetService`; replaced the **Time & Attendance**
  placeholder with a real valuation-period timesheet screen — status pills, Approve/Return actions (ConfirmDialog),
  and CSV export — identical across the mock↔API switch.
- ✅ **Tests**: `TimesheetWorkflowTests` (SUB-8 transitions); `TimesheetHoursCalculatorTests` (SUB-7 pairing,
  refusals/unmatched excluded); `TimesheetServiceTests` (roll-up, submit→approve with scope, guarded approve,
  return-not-deny R18, correction retained + effective hours R16, worker hours SUB-27, CSV + valid-zip XLSX SUB-10);
  `TimesheetApiTests` (MC-24 list, approve+guarded return, CSV/XLSX); skip-guarded `TimesheetRepositoryTests`.
  Result: **126 passed, 9 skipped**.
- ✅ Verified live (mock): company-week list shows 3 seeded operatives with rolled-up totals (MC-24); approving a
  submitted sheet records scope=Project (SUB-9); correcting a line drops the total and flags the line while
  retaining the original (R16); CSV export and a genuine .xlsx (validated as a ZIP workbook with numeric hours
  cells) both download (SUB-10). *Subcontractor MVP begins.*

### Phase 13 — Roles, audit, module entitlements & decision store (SF-2, SF-20, SF-22, SF-23, R10, R15, Q2)
- ✅ **Domain**: `AccessRole` enum (Administrator/ComplianceManager/SiteManager/Auditor) + `RolePermissions.CanWrite`
  (Auditor is read-only, SF-23); `ModuleEntitlement` (per-company module override, Q2); `AuditEntry` (SF-20,
  company-scoped R15); `SiteEntryDecision` + `DecisionCheck`/`DecisionCheckOutcome` — reconstructable
  decision record (R10), introduced here ready for the Phase 17 gate.
- ✅ **Abstractions**: entitlement/audit/decision DTOs; `IEntitlementService` (fail-closed), `IAuditService`
  (search + CSV export + record), `IDecisionService` (read by person/site + record).
- ✅ **Application**: `ModuleCatalog` (single list of modules + default-enabled) + `EntitlementService`
  (override-over-default, unknown module fails closed — the one authoritative answer, Q2); `AuditService`
  (free-text/date-range search, RFC-4180 CSV export, append record, SF-20); `DecisionService` (records +
  reconstructs checks, unknown outcome → NotRun, R10); repo interfaces + in-memory stores (audit seeded under
  a fixed demo company); DI `AddConsoleFoundationCore`/`AddInMemoryConsoleFoundationStore`.
- ✅ **DataAccess**: Dapper `EntitlementRepository` (portable update-then-insert upsert), `AuditRepository`
  (null-tolerant search SQL, `LOWER(...) LIKE` for portable case-insensitivity, date-range boundaries),
  `DecisionRepository` (checks serialised as JSON); `006_console.sql` for both engines (ModuleEntitlements
  with unique (CompanyId, ModuleKey) index Q2, AuditEntries, Decisions); registrations extended.
- ✅ **API**: `/api/entitlements` (catalogue for a company + is-enabled), `/api/audit` (search, `/export` CSV
  download, record), `/api/decisions` (by person, by site, record); composition root wires the services + store.
- ✅ **Client**: `ApiAuditService` (HTTP incl. query-string filters) + `ClientMockAuditService` (wraps sample
  audit entries, in-proc filter + CSV) behind `IAuditService`; migrated **`AuditLog.razor`** to `IAuditService`
  with server-side date-range filtering. `Mock` stays default — visually identical.
- ✅ **Tests**: `RolePermissionsTests` (SF-23 read-only auditor); `EntitlementServiceTests` (default/override/
  fail-closed/company-scoped, Q2); `AuditServiceTests` (record→search, free-text over actor/action/entity/
  reference, company scope, CSV header + quoting, SF-20); `DecisionServiceTests` (reconstructable checks,
  by-site, unknown-outcome→NotRun, R10); `ConsoleFoundationApiTests` (entitlements + audit + decision over
  HTTP); skip-guarded `ConsoleFoundationRepositoryTests`. Result: **98 passed, 8 skipped**.
- ✅ Verified live (mock): `GET /api/entitlements/{company}` returns the catalogue with foundation modules on
  and paid modules off; unknown module → `false` (fail-closed, Q2); audit search by text and CSV export return
  the seeded trail (SF-20); a posted decision reconstructs its full check set by person and by site (R10).
  *Shared foundation (PRD §5.1) complete.*

### Phase 12 — Sign-in / sign-out & attendance (SF-13–SF-19, SF-25, R3, R4, R11)
- ✅ **Domain**: `AttendanceRecord` (append-only, R4; UTC instants, R11); enums `AttendanceEventType`
  (SignIn/SignOut/OvernightFlag), `AttendanceOutcome` (Accepted/Flagged/Refused), `SignInMethod`
  (QrScan/AssignmentLink, SF-13/SF-25), `LocationPolicy` (RecordAndFlag/Refuse, SF-15); `Site.LocationPolicy`
  added (per-site customer setting).
- ✅ **Abstractions**: neutral `SignInMethod`; attendance DTOs (sign-in/out requests + results, on-site,
  record); `IAttendanceService`.
- ✅ **Application**: store-agnostic `AttendanceService` — boundary verification against site/property
  geofence (SF-14), location policy when absent (SF-15), every attempt recorded incl. refusals (SF-16),
  no double-site naming the other (SF-18/Q4), sign-out duration (SF-17), QR + link identical records
  (SF-13/SF-25); `OvernightSignInJob` (SF-19, flags-not-truncates + alerts, reuses Phase 10 `JobRunner`/
  email); repo interface + in-memory store/repo; DI `AddAttendanceCore`/`AddInMemoryAttendanceStore`.
- ✅ **DataAccess**: Dapper `AttendanceRepository` (ANSI open-sign-in / overnight queries); `SiteRepository`
  extended for `LocationPolicy`; `005_attendance.sql` for both engines (ALTER Sites + Attendance table +
  indexes); registrations extended.
- ✅ **API**: `/api/attendance` (sign-in, sign-out, on-site, records) + `/api/jobs/overnight-check` and a
  scheduler hook; composition root wires the service + store.
- ✅ **Client**: none this phase — sign-in is worker/phone-facing (proven via API + tests); a console
  "on-site"/attendance view is a logged follow-up. `Mock` stays default — no regression.
- ✅ **Tests**: `AttendanceServiceTests` (SF-14/15/16/17/18 + property boundary), `OvernightSignInJobTests`
  (SF-19 flag + alert + idempotent), `AttendanceApiTests` (sign-in→double-site→sign-out over HTTP),
  skip-guarded `AttendanceRepositoryTests`. Result: **79 passed, 5 skipped**.
- ✅ Verified live (mock): inside boundary → Accepted + present; second site while present → Refused naming
  "Meridian Tower" (SF-18); outside boundary → Refused (SF-14); sign-out returns duration (SF-17); the log
  shows all attempts including the refusal (SF-16, append-only R4).

## Outstanding / known issues
- ❗ **MUD0002 warnings** (pre-existing, in `TedwrenStepper.razor`, `AddOperative.razor`,
  `Inductions.razor`): `Variant`/`Linear`/`Color` on `MudStepper` and `Icon` on `MudStep` flagged
  by the MudBlazor analyzer. Not backend scope. Review against the current MudBlazor API in a UI pass.
- ❗ **DB path executes in CI/dev, not this container** (no SQL Server / LocalDB / Docker daemon).
  Dapper repositories + migrations are authored and the integration test is skip-guarded; run it in
  CI/dev with `DataSource:Mode=Database` and `TEDWREN_TEST_SQLSERVER` set. PostgreSQL parity is the
  Phase 18 gate.
- ❗ **WASM in-browser render not asserted this session** (no browser harness set up). The seam is
  proven at the HTTP/contract level (live API + `WebApplicationFactory` tests + shared DTOs + build);
  a bUnit/Playwright smoke test of the Organisation page in `DataSource=Api` is a small follow-up.

## Planned (next)
- ⏸️ **PostgreSQL launch gate — deferred for now (per request).** The full PostgreSQL parity suite and the
  dual-engine pre-launch gate are intentionally out of scope for the current push. The EF model already
  lower-cases identifiers for PostgreSQL and the migration scripts exist, so this is a run/verify task when
  picked up, not new build.
- ⏳ **Phase 18 — Hardening (excluding the PG gate above).** Done: **bUnit** render smoke suite
  (`tests/Tedwren.Client.Tests`) closing the "WASM render not asserted" gap; an **R14 latency assertion**
  (`SiteEntryLatencyTests`); a **pack-link security review** (`docs/security-pack-link-review.md`) **and its
  concrete hardening actions** — PBKDF2 passcode hashing (legacy-compatible), per-token passcode
  **rate-limiting** (`IPackAccessThrottle`), `no-store` headers on the recipient endpoints, and a verified
  256-bit token. Still outstanding and non-codeable in this environment: sustained **load/soak testing**
  against R14, the **independent** security review (sender-alerting, message unification, HSTS, PII
  minimisation, single-use links), a broader **accessibility** audit, and backup/restore rehearsal.
- ✅ **Attendance console view**, **Notifications over `/api/expiry`** — done previously.
- ✅ **Navigation gating over `/api/entitlements`** (SF-22) — `MainLayout` hides unpurchased modules; client
  `ApiEntitlementService` + `TenantContext`; fails open on lookup error. (Done this change.)
- ✅ **Compliance-pack recipient view page** (`/pack`, token+passcode, snapshot + CSV/ZIP/PDF) on a minimal
  recipient layout; **re-issue** action (R7) and **role-gated send** (SUB-22). (Done this change.)
- ✅ **Induction take-flow phone UI** (`/induct`: start→steps→server-scored quiz→sign) over the API, linked
  from the Induction Records console. (Done this change.)
- ✅ **Timesheet line-level correction UI** (R16) and **scope-configurable approval** (SUB-9) via a timesheet
  detail dialog. (Done this change.)
- ✅ **Qualification cards on real operatives** — `CompanyOperativeDto` now carries `PersonId`; a cards dialog
  on the company detail page shows an operative's server-computed cards (SF-7/SF-8). (Done this change.)
- ✅ **Module entitlements persist** from System Configuration (`SetEnabledAsync` + PUT endpoint), driving the
  nav gating above. (Done this change.)
- ✅ **Operative self-service hours view** (SUB-27) — `/my-hours` page over `GetOperativeHoursAsync`
  (person+week from the link, week paging), reachable from the company operatives list. (Done this change.)
- ✅ **Compliance % roll-up** — new `ComplianceRollup` derives company/operative state + % from current card
  statuses; `OrganisationService` now reports it instead of `Pending`. (Done this change.)
- ✅ **Company edit persists** — `UpdateCompanyAsync` across the stack + an `EditCompanyDialog`; the
  CompanyDetail "Edit" action now saves. (Done this change.)
- ⏳ **Follow-ups (non-blocking), remaining:**
  - **Persist the other demo write actions**: operative "Edit"/"Send update link", **site** "Edit", System
    Configuration **general settings**, and Permits "Save" — each needs a dedicated write endpoint/service
    (company edit + module entitlements persist today; site repo already has `UpdateAsync` so site edit is a
    small next step).
  - Real **SMS** provider (PRD-Phase 7) — email is done (Resend + branded template + invite delivery, see
    Completed); SMS is the remaining channel (and the natural route for onboarding links). Company
    insurance/accreditation docs in the digest (needs SUB-4); real card-image storage (R9).
- ✅ *Done previously:* audit "Export CSV"; SiteDetail over `ISiteService`; Users management page;
  `/sites/add`; Dashboard export/date-range; EF migrations tooling; Mock→Database default; Mock mode removed.

## Later (per `docs/plan-and-scope.md`)
- ⏳ Phases 9–13 — Shared Foundation (cards & competency; expiry engine & job heartbeat; sites,
  boundaries & dispersed schemes; sign-in/out & attendance; roles, audit & module entitlements).
- ⏳ Phases 14–15 — Subcontractor MVP (timesheets; compliance pack). *Product saleable.*
- ⏳ Phases 16–17 — Main Contractor MVP (induction & consent; site-entry decision, competency
  cover & muster). *Product saleable.*
- ⏳ Phase 18 — Hardening + PostgreSQL launch gate.
- ⏳ Phases 19+ — PRD commercial modules (CSCS, HSE, QA, Pay, Identity, Sharing, Integrations).
- 📋 **Forms Library (Phases 19–25).** Customer-built, per-tenant form engine. Detailed plan of works
  in [`docs/forms-library-plan.md`](docs/forms-library-plan.md). Clones the `InductionTemplate`
  pattern; reuses the Forms suite + `DataTable`. **Not part of the MVP.**
  - ✅ **Phase 19 — Domain & persistence.** `FormFieldKind` (full field spectrum), `FormTemplateStatus`,
    `FormField`/`FormSectionDef`/`FormTemplate` (per-tenant, R15; versioned & append-only via `FamilyId`,
    R4/R10/R16). `IFormTemplateRepository` + Dapper `FormTemplateRepository` (sections as `SectionsJson`),
    `InMemoryFormStore` + in-memory repo. Schema: `FormTemplateRecord` + `DbContext` mapping, EF migration
    `AddFormsLibrary`, and idempotent `018_forms_library.sql` for SQL Server + Postgres. Domain (4) +
    skip-guarded repository tests.
  - ✅ **Phase 20 — Template service & API.** `Contracts/Forms` DTOs, `IFormTemplateService` +
    `FormTemplateService` (tenant-scoped like `SiteService`; create→Draft v1, publish freezes, edit-published
    creates new draft v2, archive; latest-per-family listing). `FormEndpoints` (`/api/forms/templates`,
    reads authed, writes `RequireWrite`, `/fill` serves Published only; no anonymous route). DI wired in
    `Program.cs`. Application (8) + API (3) tests. `dotnet build`/`dotnet test` green.
  - ✅ **Phase 21 — Builder UI & field wrappers.** New `Tedwren*` field wrappers (`TedwrenNumericField`,
    `TedwrenDatePicker`, `TedwrenRadioGroup`, `TedwrenRagInput`, canvas `TedwrenSignaturePad` with JS interop),
    catalogued. `FormBuilder` component + `FormEditModel`; `FormsLibrary.razor` (list/edit/publish/archive) and
    `FormBuilderPage.razor` (new/edit) over a client `ApiFormTemplateService`; nav in `ShellChrome`. bUnit
    render + interaction tests for the RAG input.
  - ✅ **Phase 22 — Fill & submissions.** `FormSubmission`/`FormSubmissionFile` domain, repos (Dapper +
    in-memory), `019_form_submissions.sql` (both engines) + EF migration; `IFormSubmissionService` +
    `FormSubmissionService` (required-by-default validation, published-only, append-only R4/R10, review flow,
    DB-stored files). Submission endpoints (`/api/forms/submissions`, writes/review `RequireWrite`, file
    download). `DynamicFormRenderer` (renders per `FormFieldKind`, collects answers + base64 files),
    `FormFill.razor` (org/site level, requirement 7), `FormSubmissions.razor` + `FormSubmissionDialog`
    (view/approve/reject) over a client `ApiFormSubmissionService`. Application (6) + API (2) + skip-guarded
    DataAccess tests. Whole solution builds warning-clean; all suites green.
  - ✅ **Phase 23 — PDF & email.** Added **QuestPDF** (Community licence) with an embedded Tedwren logo;
    `FormPdfRenderer` builds a branded A4 PDF — logo top-left, form/submitter/UTC-stored-UK-displayed header
    (R11), answers per section with embedded photos and signature images, status badge, page footer. Verified
    it renders valid PDF headless. `IEmailSender` gained an attachment overload (`SendHtmlWithAttachmentsAsync`,
    Outbox + Resend base64 attachments); `FormSubmissionEmail` template. Service `GeneratePdfAsync` +
    `EmailAsync` (site name resolved for the PDF); endpoints `GET /submissions/{id}/pdf` and
    `POST /submissions/{id}/email` (`RequireWrite`). Client download-PDF + email actions in the submission
    dialog. Application (PDF render, service PDF + email-with-attachment) + API (pdf/email endpoint) tests.
    Note: the framework-only `PdfWriter` is retained for the tabular compliance-pack exports; QuestPDF is used
    only for branded form output.
  - ✅ **Phase 24 — Assignment & induction integration.** `FormAssignment` domain (+ `FormSchedule`),
    repos (Dapper + in-memory), `020_form_assignments.sql` (both engines) + EF migration.
    `IFormAssignmentService` + `FormAssignmentService` (tenant-scoped assign to organisation/site/operator/
    induction, snapshots form name, validates target; delete). Endpoints `/api/forms/assignments`
    (GET/POST/DELETE, writes `RequireWrite`). **Failed-check alert**: a red RAG answer emails the assignment's
    alert address with the branded PDF attached (PRD §8), wired into the submission flow. **Induction
    integration (authoring)**: `InductionStepKind.Form` + optional `InductionStep.Reference` (form family id,
    backward-compatible); the induction builder attaches published forms as steps. Assign dialog + assignments
    table in the Forms Library; client `ApiFormAssignmentService`. Application (assignment CRUD + failure-alert)
    + API (assignment round-trip) + skip-guarded DataAccess tests. Whole solution builds warning-clean; all
    suites green.
  - ✅ **Phase 25 — Hardening.** **Entitlement gate**: added the paid `forms` module to `ModuleCatalog`
    (default off); **server-side, fail-closed** enforcement via a reusable `ModuleGate.Require("forms")` endpoint
    filter on the whole `/api/forms` group (the first server-side entitlement enforcement in the app — client
    nav is only cosmetic); client nav gating for `/forms` + `/form-submissions`; the demo/test company is seeded
    with `forms` enabled so the mock and API tests exercise the module. **Default template library**:
    `DefaultFormTemplates` ships three ready-made forms (Daily Site Diary, Plant & Equipment Checklist, Welfare
    Check); `SeedStarterTemplatesAsync` creates+publishes them idempotently; endpoint
    `POST /api/forms/templates/seed-starter` + an "Add starter forms" action in the library. **PostgreSQL
    parity**: the `018`/`019`/`020` scripts exist for both engines with verified column parity (the full PG
    parity run remains the pre-launch gate, deferred with the other PG work). Application (starter seed) + API
    (module gate 200→403→200, starter seed) tests. Whole solution builds warning-clean; all suites green.
    - ✅ **Recurring form reminder job (was deferred) — Daily/Weekly/Monthly cadences (R12).** `RecurringFormReminderJob`
      (`Application/Forms`) scans every recurring assignment: for each cadence it computes the current period (day /
      ISO week from Monday / calendar month) and, when no submission of that form family exists in the period, emails a
      "form due" reminder to the assignment's alert address and the company administrator. Idempotent per period via a
      new `FormAssignment.LastReminderUtc` marker (mirrors SF-9's notification log) — running the scan twice in a period
      never reminds twice. Added `IFormAssignmentRepository.GetRecurringAsync` + `UpdateLastReminderAsync` (Dapper +
      in-memory), migration `021_form_assignment_reminders.sql` (both engines) + EF migration `AddFormAssignmentReminder`
      + `LastReminderUtc` schema record. Wired into `ExpirySchedulerHostedService` (runs each tick under `JobRunner`,
      `JobNames.FormReminder`) with a manual `POST /api/jobs/form-reminders` trigger. Application (3) + API (1) +
      skip-guarded DataAccess coverage; all suites green.
    - ✅ **Induction-embedded form fill (was deferred) — anonymous take-flow (requirement 5, R5).** The worker's
      `InductionTake` flow now renders a `Form`-kind step's published form and submits it. New anonymous, session-scoped
      routes `GET/POST /api/inductions/sessions/{sessionId}/forms/{stepId}` — the anonymous surface only ever exposes
      forms actually attached to the induction being taken, resolved through the session (never arbitrary forms), so the
      secure-by-default/R5 boundary stays tight. `IInductionService.GetSessionFormAsync` + `SubmitSessionFormAsync`
      (resolve the family's latest published version scoped to the session's company, R15; submit server-side with the
      scope/person fixed by the session so the caller cannot retarget). `IFormSubmissionService.SubmitForContextAsync`
      submits on behalf of the anonymous flow with company/person/submitter supplied explicitly (reuses the same
      required-by-default validation, file capture and failure-alert pipeline). Client `ApiInductionService` +
      `InductionTake.razor` reuse `DynamicFormRenderer`. Application (4) + API (3, incl. an anonymity assertion) tests;
      all suites green.

  The Forms Library (Phases 19–25) is now feature-complete against requirements 1–11 with both former follow-ups
  delivered; the remaining Forms work is the pre-launch PostgreSQL parity run (shared with the wider PG launch gate).

## Deferred (PRD-directed)
- ⏸️ Cross-company sharing surface (PRD-Phase 6) — consent capture (MC-20) is in the MC MVP because
  it cannot be retrofitted, but the sharing surface needs market density. Decision open (PRD §6.3).
- ⏸️ Bulk/spreadsheet import (SUB-3) — start without it; add only if a real customer is blocked.
- ⏸️ Worker self-service app (R1/Q8) — a question, not a prohibition; not in either MVP.
