# Tedwren — Phased Development Plan & Scope of Works

## Context

Tedwren Ltd is building a UK construction **workforce compliance platform** delivered as
two independently saleable products (a subcontractor *time-and-attendance* product and a
main contractor *workforce-management* product) sharing one data foundation, followed by
seven numbered commercial modules. The definitive brief is **PRD v6.4**
(`TedwrenPRDv6_4.docx`, attached), which supersedes all prior specs.

> **Delivery status (updated):** the backend is now built out. Phases **7–17** landed the API, data
> access, and the console-over-API migration (**M1–M6**: every page renders live API data; the
> `UiComponents.SampleData` project has been removed). The post-migration follow-ups **D1–D6** are also
> delivered: **D1** console authentication (JWT, roles, SF-23 auditor read-only), **D2** self-service
> operative onboarding link (SF-4/SUB-2), **D3** induction template authoring (MC-15), **D4** real per-site
> operatives/compliance (MC-12/13), **D5** removal of the last demo constants, **D6** N+1 batching. See
> `TODO.md` for the running log and the outstanding production hardening items (secrets, invite email,
> rotating the committed DB credential). The original UI-phase context below is retained for history.

The repository began as a **UI/UX foundation only**. **Six UI phases were complete on `origin/main`**
(PRs #1–#10), all of them **front-end/component work over mock data**:

- **Phase 1** Shell & theme · **Phase 2** Dashboard · **Phase 3** generic `DataTable<TItem>` +
  list pages · **Phase 4** full forms suite (`FormField`, `TedwrenTextField/Select/Autocomplete/
  Toggle/DateRangePicker/FileUpload/Stepper`, `FormSection`, `FormActions`) + add/invite/stepper
  flows · **Phase 5** feedback/state components (`BannerAlert`, `ConfirmDialog`, `EmptyState`,
  `LoadingSkeleton`) + polish · **Phase 6** entity **detail pages** (company, operative, site) +
  `DetailHeader`, `Flyout`, `NotificationsMenu`, `KeyValueList`.
- Mock data now sits behind **five interfaces** — `IShell/IDashboard/IList/IForm/IDetailSampleDataService` —
  already modelling `Company`, `Operative`, `Site`, `QualificationType`, `AuditEntry`,
  `ModuleEntitlement`, `GeneralSettings`, etc. This is the exact seam the backend plugs into.

There is still **no backend, no Web API, no data model, no persistence, no governance docs**
(CLAUDE.md / TODO.md), and the PRD is not stored in the solution. Everything above validates and
toggles visually but nothing persists — the README itself states backend is "explicitly out of
scope and follows later." **My server-side plan below is therefore greenfield; the six completed
phases are the UI it will drive.**

> **Correction (this session):** an earlier draft of this plan said only Phases 1–2 were done —
> my working branch was stale at PR #2. `origin/main` is actually at PR #10 with Phases 1–6 merged.
> The designated branch `claude/dev-plan-phased-scope-4n3vh9` is behind `origin/main` and **must be
> reset onto the latest `origin/main` before any work begins** (`git fetch origin main &&
> git checkout -B claude/dev-plan-phased-scope-4n3vh9 origin/main`).

This plan turns that UI shell into the full product **incrementally**, following the PRD's
own Section 11 sequencing (shared foundation → subcontractor MVP → main contractor MVP →
hardening → Phases 1–7), while satisfying the mandated engineering standards (separate Web
API, Dapper over SQL Server *and* PostgreSQL, mock↔DB switch behind a shared abstraction,
SRP, async, per-class/per-method summary comments, reuse of existing components). **No code
is written until this plan is approved** — this document is the deliverable.

Phase numbering **continues the existing sequence**: Phases 1–6 are done, so new work
starts at **Phase 7**. The PRD's own "Phase 1–7" are the later *commercial modules* and are
referred to here as **"PRD-Phase n"** to avoid collision.

---

## Confirmed architectural decisions

| Decision | Choice |
|---|---|
| API ↔ WASM topology | **Fully separate**: `Tedwren.Api` is API-only; the WASM is served independently and calls the API cross-origin via **CORS**. Two deployables. |
| Integration-test DB | **SQL Server LocalDB / shared instance**, every test in a **rolled-back transaction** (no existing records touched). **PostgreSQL** proven by a dedicated smaller suite as a **pre-launch gate** (Phase 18). |
| Neutral codename (§1.1) | Keep **`Tedwren`** as the internal codename everywhere (repos, resources, DB, service names). The customer-visible product name stays in **configuration** + one front-end surface, so a rename is a settings change, not a migration. |
| Data-access strategy | **Dapper** with a shared abstract repository base + an `ISqlDialect`/provider abstraction; SQL Server is primary, PostgreSQL supported from day one so pre-launch support is *validation*, not a rewrite. |
| Settings/schemas storage | **JSON** (System.Text.Json) in `nvarchar(max)` / `jsonb` columns for settings, integrations, onboarding schemas, module config (Q2, MC-3). |
| Time & residency | Store unambiguous instants (UTC/`DateTimeOffset`), **display UK local time** (R11); personal data stays in the UK (R13). |
| Append-only evidence | Attendance, induction, decision and correction records are **added to, never edited or deleted** (R4, R10, R16). |

---

## Target solution structure

Additions to the existing three projects (existing WASM/UiComponents/SampleData are **kept
and extended, never replaced**):

```
src/
  Tedwren.Client              (existing) Blazor WASM — UI only, consumes interfaces
  Tedwren.UiComponents        (existing) reusable MudBlazor component kit + theme
  Tedwren.UiComponents.SampleData (existing) in-proc mock implementations
  Tedwren.Abstractions        NEW  shared service interfaces + DTOs (referenced by WASM & API)
  Tedwren.Domain              NEW  entities, value objects, enums (no external deps)
  Tedwren.Application         NEW  business services (interfaces + implementations), SRP
  Tedwren.DataAccess          NEW  Dapper repositories: shared base + SqlServer/Postgres dialects
  Tedwren.Api                 NEW  ASP.NET Core Web API (.NET 10), CORS, mobile-ready
tests/
  Tedwren.Domain.Tests        NEW  value objects / business rules (unit)
  Tedwren.Application.Tests   NEW  services against mocked repositories (unit)
  Tedwren.DataAccess.Tests    NEW  repositories vs LocalDB, transaction-rollback (integration)
  Tedwren.Client.Tests        NEW  bUnit component tests (where valuable)
db/
  sqlserver/  postgres/       NEW  idempotent migration scripts per engine (runner in DataAccess)
docs/
  TedwrenPRDv6_4.docx         NEW  source of truth, stored in-repo
  plan-and-scope.md           NEW  this document, versioned
  component-catalogue.md      (existing) extended per phase
```

### The mock↔DB switch (the seam that must not leak)

A single shared abstraction so switching **never** touches UI or business logic:

- The **five** existing `I*SampleDataService` interfaces (`Shell`, `Dashboard`, `List`, `Form`,
  `Detail`) are generalised into domain service interfaces in **`Tedwren.Abstractions`**,
  referenced by both the WASM and the API. Their existing record shapes (`Company`, `Operative`,
  `Site`, `QualificationType`, `AuditEntry`, `ModuleEntitlement`, `GeneralSettings`, …) seed the
  DTO/domain model, so the backend fills contracts the UI already consumes.
- **WASM side** — `appsettings.json` key `DataSource`:
  - `Mock` → registers the existing in-proc `SampleData` implementations (today's behaviour).
  - `Api` → registers **typed `HttpClient`** implementations of the same interfaces calling `Tedwren.Api`.
  - The UI injects the interface only, so it is byte-for-byte identical either way.
- **API side** — `appsettings.json` key `DataSource`:
  - `Mock` → in-memory implementations (deterministic, for demos/tests).
  - `Database` → **Dapper** implementations, provider chosen by `Database:Provider` = `SqlServer` | `PostgreSql`.

This satisfies: "an appsetting that switches between the mock-data service and the database
connection," and "mock and database-backed implementations share an abstraction so switching
requires no UI/business-logic changes."

### Data-access shape (Dapper, dual-engine)

- `IDbConnectionFactory` returns `SqlConnection` or `NpgsqlConnection` from config.
- `RepositoryBase` holds shared query/exec/transaction helpers (async).
- Per-entity repositories derive from the base; **dialect-specific SQL** lives behind an
  `ISqlDialect` abstraction (or paired `SqlServerXRepository`/`PostgresXRepository` over a
  shared abstract `XRepositoryBase`) so common logic is written once. Chosen concretely in
  Phase 8 on the first slice, then reused verbatim.
- Migrations: idempotent scripts under `db/<engine>/`, applied by a small runner at startup
  (dev) / by CI. JSON columns via `nvarchar(max)` (SQL Server) / `jsonb` (Postgres).

---

## Phased roadmap

Each phase: **independently testable**, **no breaking change** to prior phases (mock stays
the default until a slice's DB path is proven), and delivers a **usable increment**. Feature
phases build strictly on the shared abstraction so the UI is stable across the mock↔DB flip.

### Enabling phases

**Phase 7 — Governance & backend scaffolding** *(no visible behaviour change)*
- Store `TedwrenPRDv6_4.docx` and this plan in `docs/`; refresh the README's "backend follows
  later" framing to point at this plan.
- Create **CLAUDE.md** (PRD v6.4 is authoritative; architecture map; standards; the mock↔DB
  switch; phase discipline) and **TODO.md** (planned / in-progress / completed / deferred /
  outstanding, updated every session; completed items carry a concise change note + phase;
  back-fill Phases 1–6 as done).
- Add empty, wired **`Tedwren.Abstractions`, `.Domain`, `.Application`, `.DataAccess`,
  `.Api`** projects + test projects to the solution. `Tedwren.Api` exposes a health endpoint
  and CORS for the WASM origin. `DataSource` defaults to `Mock` everywhere — app behaves exactly as today.
- **Testable:** solution builds clean (no new warnings); WASM runs unchanged; API health check passes.

**Phase 8 — Prove the data-access seam on one slice (SF-1, SF-2, SF-3)**
- First vertical slice end-to-end: **Company + Person + Engagement** (person keyed by
  normalised mobile number; one person across companies; per-company view isolation; archive/
  reactivate). Includes the settle-once **`PhoneNumber`** value object / normaliser (Q9) and a
  reusable tenant-isolation guard (R15).
- Implement the slice **three ways** behind the shared interface: SampleData (exists), API-Mock,
  API-Dapper (SQL Server + Postgres SQL). The existing **Organisation list + `CompanyDetail`/
  `AddCompany`** pages already consume `IList`/`IDetail`/`IForm` services, so they light up over the
  DB with no UI change. Flip `DataSource` to exercise all paths.
- **Testable:** the same UI works under Mock and Database; DataAccess integration tests (LocalDB,
  rolled-back transactions) prove SQL-Server CRUD + isolation; Postgres SQL compiles and runs the
  same suite locally. This phase locks the pattern every later phase copies.

### Shared Foundation (PRD §5.1 — ~60% of both products)

**Phase 9 — Qualification cards & competency (SF-5–SF-8, SF-10–SF-12, SF-16 card side)**
Card capture by photo with suggested-not-silent reads; three states (read / customer-checked /
CSCS-verified) visibly distinct; named confirmation with who/when; **status computed from
expiry** (never typed); trade→required-qualification lists; default library (Q21 list supplied
by client); renewed card supersedes without overwrite. Reuse `StatusPill`/`RiskChip`/`ExpiryList`.

**Phase 10 — Expiry engine, warning schedule & job heartbeat (SF-9, SF-21, SUB-5, R12)**
Scheduled expiry evaluation; warnings at 60/30/7/0/+1 days (worker SMS, admin email);
idempotent (no double-send); weekly 60-day digest (cards + company docs together). Every job
**reports whether it ran and alerts on silent stop** (R12 — the most dangerous silent failure).
SMS/email behind provider interfaces (stubbed now; real providers PRD-Phase 7).

**Phase 11 — Sites, boundaries & dispersed schemes (SF-6, SF-14, SF-25, SF-26)**
Site records (incl. subcontractor-recorded sites — free/unlimited, never billable); a site
carries a **geofence boundary**; a site may be a set of **dispersed properties** each with its
own geofence grouped under one site for management/billing; adding a property requires nothing
printed/installed/attached. Foundation for verified sign-in.

**Phase 12 — Sign-in / sign-out & attendance (SF-13–SF-19, SF-25, R3, R4, R11)**
QR-scan sign-in (no app/account) **and** the no-compound route (assignment-scoped link active
only inside the boundary) producing an **identical-weight** record; location+time recorded;
location-unavailable behaviour is a **customer setting** (record-and-flag default, Q5/SF-15);
every attempt incl. refusals stored **append-only**; no worker present at two sites at once
(cross-customer name-only check, Q4); overnight-still-in alert + flag-not-truncate. UK-time
storage/display (R11).

**Phase 13 — Roles, audit, module entitlements & console gating (SF-2, SF-20, SF-22, SF-23, R10, R15, Q2)**
Roles incl. read-only auditor (SF-23); audit search by name/reference/date-range + export
(SF-20); **module entitlements checked server-side, fail-closed, one authoritative answer**
(Q2); navigation shows only purchased modules — no "locked door" (SF-22); the reconstructable
**decision-record store** is introduced here (R10) ready for the gate. *Shared foundation complete.*

### Subcontractor MVP (PRD §5.2 — ships first, G8)

**Phase 14 — Timesheets (SUB-7–SUB-12, SUB-24, SUB-27–SUB-30, MC-24 view, R16)**
The **timesheet as a first-class, stateful, approvable object** (not a view over rows):
attendance rolls up per operative per week; corrections are **new records** referencing the
original with author/when/why (R4/R16); **configurable approval** at line/site/project/all
(SUB-9); worker sees own real-time hours (SUB-27); CSV **and** Excel export at operative and
site level (SUB-10, first-class). MC-24 is delivered as a **company/valuation-period view over
the same object**, not a second implementation. Never uses "permitted/denied" (R18, SUB-12).

**Phase 15 — Compliance pack (SUB-13–SUB-26, R7, R8, R9)**
Pack builder over selected operatives/site/date-range; **not-site-ready problems surfaced before
send**, with explicit acknowledgement required (SUB-14); contents chosen not automatic; output as
web link + PDF + ZIP with identical content, 25 operatives ready < 1 min (SUB-16); **recipient
needs no account** (R8); passcode + sender-set expiry (30-day default, SUB-18/Q12); open/download
tracking (SUB-20); revoke (SUB-21); **fixed-at-send, re-issue supersedes** (R7); no permanent
public asset URLs (R9); send restricted to nominated roles (SUB-22). *Subcontractor product saleable.*

### Main Contractor MVP (PRD §5.3)

**Phase 16 — Digital induction & consent (MC-1–MC-7, MC-15, MC-20, R5)**
Whole induction in a phone browser (identity, cards, emergency contact, declarations,
video/document, quiz, signature); **configurable capture** (MC-3, JSON schema); can't continue
until content demonstrably completed; **quiz scored server-side, answers never sent to device**
(R5); failed-attempt handling + manager reset with recorded reason (MC-6); completion reference +
configurable validity, re-induction supersedes (MC-7); **separate, optional, non-pre-ticked
consent** (MC-20 — cannot be retrofitted; stored, consumed in PRD-Phase 6).

**Phase 17 — Site-entry decision, competency cover & muster (MC-8–MC-14, MC-16–MC-23, MC-25, MC-28, R2, R3, R10, R14)**
The **five-check decision** (registered / not-elsewhere / induction valid / cards in date+
confirmed / RAMS where module held) against **current data** (R3); **fail-closed** (R2 — any
error ⇒ no); specific actionable block reason (MC-9); **self-reconstructing decision record**
(R10) incl. checks not run and why; day-only manager override with reason (MC-11); live on-site
view filterable, resolving to property on dispersed schemes (MC-12); **competency cover** present/
alert-on-last-out (MC-13); **offline-capable muster** with data-age (MC-14); decision reachable on
no-compound schemes (MC-28); instrument the **<3s** budget from the start (R14). *Main contractor product saleable.*

### Hardening & commercial modules

**Phase 18 — Hardening & PostgreSQL launch gate**
Accessibility pass; load/latency testing against R14; **independent security review of the public
pack link** (the only public route to personal data); backup/restore rehearsal; **full PostgreSQL
parity suite** run green (proves the dual-engine promise before production).

**Phases 19+ — PRD commercial modules (sell-before-build; sketched, sequenced per PRD §8):**
PRD-Phase 1 Live CSCS verification (external agreement started day one) · PRD-Phase 2 Health,
Safety & Compliance (RAMS, doc distribution, checklists, permits, plant register, HAVs, near-miss,
carbon) · PRD-Phase 3 Quality Assurance (forms, snagging, plot tracker) · PRD-Phase 4 Pay & cost ·
PRD-Phase 5 Verified identity (biometrics under R17 + DPIA) · PRD-Phase 6 Cross-company sharing
(consumes MC-20 consent) · PRD-Phase 7 Integrations & report library. *The asset register entity
(PRD-Phase 2) is stubbed in the Phase 8 data model so it is an addition, not a rewrite.*

The **Forms Library** (the customer-built checklist/inspection engine at the heart of PRD-Phase 2,
reused by PRD-Phase 3) has its own detailed plan of works — **Phases 19–25** — in
[`forms-library-plan.md`](forms-library-plan.md): per-tenant form builder, versioned templates,
DB-stored submissions, branded PDF (QuestPDF) + email, and assignment to sites/operators/inductions.

### Admin area & GoCardless billing (Tedwren platform operations)

A platform-operator area inside the existing console (`Tedwren.Client`), shown to Tedwren **platform
administrators** (an `Administrator` in the Tedwren seed tenant — the accounts seeded by
`AdminUserSeeder`) in place of the tenant console. Gated server-side by a `PlatformAdmin` policy and
enabled per deployment by an `Admin:Enabled` client flag; the menu swap is UI only, never the security
boundary. Phased, each independently shippable:

- **Admin Phase A — shell & read-only views (done).** Platform-admin gate + menu swap; `/admin/*`
  companies/users/dashboard over a dedicated `PlatformAdmin`-gated `/api/admin` surface; placeholders for
  the billing surfaces below.
- **Admin Phase B — GoCardless mandates & payments (done).** `GoCardlessOptions` + typed `HttpClient`
  (mirroring the Resend email integration) and `IGoCardlessClient`; a `Mandate`/`Subscription`/`Payment`
  domain slice (Dapper, dual-engine, migration `022`) tied to `CompanyId` (R15); create/cancel mandate
  (hosted Billing Request Flow — we never handle raw bank details), take payment, **retry after a return**.
  Meter/band are **configuration, not hard-coded numbers** (PRD §9). Reads work with no token; collection
  actions require a configured provider (sandbox by default).
- **Admin Phase C — webhooks, returns/retries & reconciliation (done).** `AllowAnonymous` webhook endpoint
  with `Webhook-Signature` HMAC verification (fails closed) + event dedupe (`WebhookEvent`, migration `023`);
  events update mandate/payment status and record a returned payment's reason for re-taking; a reconciliation
  `BackgroundService` (`BillingReconciliationHostedService`) modelled on `ExpirySchedulerHostedService`
  backstops missed webhooks. `/admin/events` shows each event's outcome. (Mandate-active → entitlement flip
  is left as a deliberate seam — which module a mandate gates is a §9 commercial decision, not yet specified.)
- **Admin Phase D — BACS payouts (done).** Payout/settlement reads (`IGoCardlessClient.ListPayoutsAsync`,
  `Payout` entity, migration `024`) mirrored by `PayoutSyncService` (folded into the reconciliation hosted
  service); `GET /api/admin/billing/payouts` + `POST .../payouts/sync` under `PlatformAdmin`, and a live
  `/admin/payouts` view with a "Refresh from GoCardless" button. Not tenant-scoped (Tedwren's own settlement).

> **PRD gap (raise, don't work around).** GoCardless / direct-debit collection is **not in PRD v6.4**:
> §9 sets the commercial model (metered by sites/operatives) but names no collection rail, and §12.8 cites
> Stripe card checkout only for the separate Worker Passport product. GoCardless as the SaaS billing rail
> is confirmed with the product owner and should be reconciled into §9 in a future PRD revision.

---

## Cross-cutting engineering standards (apply every phase)

- **SRP throughout**; no god classes; one responsibility per class. **Summary comment on every
  class and every method.** **Async** wherever practical.
- **Reuse first**: the Phase 1–6 kit already covers most needs — `DataTable<TItem>`, the full
  **Forms** suite (`FormField`, `TedwrenTextField/Select/Autocomplete/Toggle/DateRangePicker/
  FileUpload/Stepper`, `FormSection`, `FormActions`, `InlineValidationMessage`), **Feedback**
  (`BannerAlert`, `ConfirmDialog`, `EmptyState`, `LoadingSkeleton`), `DetailHeader`, `Flyout`,
  `KeyValueList`, plus the Phase 2 cards/charts (`KpiCard`, `DashboardCard`, `ExpiryList`,
  `StatusPill`, `RiskChip`, `DonutStat`). Backend phases wire these to real data via the shared
  interfaces rather than building new UI. Any genuinely new component follows established naming,
  scoped CSS, `tokens.css`-only colour/spacing, and a `component-catalogue.md` entry — no
  alternative patterns where one can be extended.
- **Latest compatible MudBlazor** (currently 8.5.x) — upgrade only if compatible and warning-clean.
- **Tests:** unit (Domain rules, Application vs mocked repos) + integration (DataAccess vs LocalDB,
  **transaction-rollback, purpose-created data, never mutate existing records**); Postgres suite for
  the launch gate; bUnit where valuable. Build **and** test the whole solution before any PR;
  resolve all compile errors; investigate warnings rather than blanket-suppressing.
- **Governance:** treat PRD v6.4 as source of truth (reference, don't reproduce). Update **TODO.md**
  whenever work starts/completes/defers/surfaces; completed items get a concise change note + phase.
  End each change set with a concise summary (changes, test results, outstanding, **next logical
  step** aligned to this phased plan) — no invented requirements beyond PRD/existing solution.
- **Branch discipline:** all work on `claude/dev-plan-phased-scope-4n3vh9`.

---

## Verification (per phase and cumulatively)

1. **Build:** `dotnet build Tedwren.sln` — clean, no new warnings.
2. **Test:** `dotnet test` — unit suites always; DataAccess integration vs LocalDB
   (`Database:Provider=SqlServer`) with per-test transaction rollback.
3. **Mock↔DB flip:** run the WASM with `DataSource=Mock` then `DataSource=Api`, and the API with
   `DataSource=Mock` then `Database`; confirm the exercised screens are **behaviourally identical**
   and the UI/business layers are untouched by the flip.
4. **Run:** `dotnet run --project src/Tedwren.Api` + `dotnet run --project src/Tedwren.Client`;
   verify the phase's increment through its screen(s) and, where relevant, the API endpoints.
5. **Postgres (Phase 18 gate, but SQL authored from Phase 8):** run the DataAccess suite with
   `Database:Provider=PostgreSql` against a Postgres instance; confirm parity.
6. **Regression:** previously completed phases still pass their tests and the default (`Mock`) path
   still renders the shell + dashboard exactly as before.

---

# Phase 8 — Data-access seam on the first slice (SF-1, SF-2, SF-3) — DETAILED PLAN

## Context

Phase 7 added empty backend projects and the `DataSource` switch, all defaulting to `Mock`.
Phase 8 makes the switch *real* on one vertical slice — **Company + Person + Engagement** — so
every later phase copies a proven pattern rather than inventing one. It also lands the three
foundational primitives the whole product depends on: the **`PhoneNumber`** identity normaliser
(SF-1, PRD Q9), **per-company isolation** of a shared person (SF-2, R15), and **archive/
reactivate** (SF-3). The visible proof is the existing **Organisation** screens served over the
API; the person/engagement rules are proven by unit + integration tests and API endpoints.

### Constraints discovered (drive the design)
- The sample services (`IListSampleDataService` etc.) are **synchronous** and their DTOs live in
  `src/Tedwren.UiComponents.SampleData`. Blazor WASM cannot block on async HTTP, so the API-backed
  path needs an **async** contract. → Introduce async contracts in `Tedwren.Abstractions` and
  migrate **only** the three Organisation pages (strangler pattern); all other pages keep their
  sync sample interfaces **unchanged** (no regression).
- Mock mode must stay **visually identical**. → The mock implementation of the new contract
  **wraps the existing sample services** (as `DetailSampleDataService` already wraps
  `IListSampleDataService` at `src/Tedwren.UiComponents.SampleData/DetailSampleDataService.cs:14`)
  and maps their records to the new DTOs. No sample data is duplicated.
- **No SQL Server/LocalDB and no Docker daemon in this container.** → The Dapper path is authored,
  unit-mocked, and covered by integration tests that **skip when no connection string is
  configured**, so they run in CI/dev (LocalDB / shared instance) per the agreed test strategy.
  In this session the seam is proven end-to-end in **Mock** and via the **API's mock path**
  (`WebApplicationFactory`), which need no database.

## Approach

### 1. Domain — `src/Tedwren.Domain`
- `ValueObjects/PhoneNumber.cs` — the settle-once normaliser (SF-1, Q9). `Parse`/`TryParse`,
  normalises UK (`07…`, `+44`, spaces/punctuation) and international forms to a canonical E.164-ish
  string; value equality on the normalised form. This is the person identity key. Names are
  **never** reconciled (PRD §5.1) — they live on the engagement, not the person.
- `Entities/Company.cs` — `Id` (Guid), Name, Type, Trade, RegistrationNumber, Address, contact
  fields, timestamps. (Compliance% is derived from cards, which arrive Phase 9 — see mapping note.)
- `Entities/Person.cs` — `Id` (Guid) + `PhoneNumber` (identity) + created timestamp only.
- `Entities/Engagement.cs` — `Id`, `CompanyId`, `PersonId`, `Name` (as recorded by that company),
  Trade, InternalReference, `EngagementStatus`, timestamps. Encodes SF-2 and SF-3.
- `Enums/EngagementStatus.cs` — `Active`, `Archived`. (Company type/trade stay free-text strings to
  match the existing option lists and avoid inventing a closed set the PRD leaves open.)

### 2. Abstractions — `src/Tedwren.Abstractions`
- `Contracts/Organisation/*` DTOs: `CompanySummaryDto`, `CompanyDetailDto`, `CreateCompanyRequest`,
  `AddOperativeRequest`/`AddOperativeResult`, `OperativeSummaryDto`. **Property names mirror the
  existing records** (`src/Tedwren.UiComponents.SampleData/IListSampleDataService.cs`,
  `IDetailSampleDataService.cs`) so the razor markup barely changes.
- `Services/IOrganisationService.cs` — async: `GetCompaniesAsync`, `GetCompanyAsync(slug)`,
  `CreateCompanyAsync`, plus `AddOperativeAsync`, `ArchiveEngagementAsync`,
  `ReactivateEngagementAsync` (SF-1/2/3; the latter three are API+test-proven this phase, UI later).
- ~~`ClientDataSourceMode` enum (`Mock`, `Api`) for the client switch~~ — **superseded:** runtime Mock mode
  was removed. The client always calls the API; the server-side `DataSourceMode` retains only a test-only
  `InMemory` value used by the API test host (the in-memory repositories are now purely a test double).

### 3. Application — `src/Tedwren.Application`
- `Organisation/OrganisationService.cs` (real impl) — depends on repository interfaces; holds the
  SF rules: `AddOperativeAsync` **creates-or-reuses** a `Person` by normalised phone (SF-1),
  **refuses a duplicate engagement in the same company naming the existing record** (SF-2 accept-
  when), `Archive`/`Reactivate` toggle `EngagementStatus` while retaining history (SF-3); reads
  never cross companies (R15).
- `Organisation/MockOrganisationService.cs` — implements `IOrganisationService` by **wrapping the
  existing sample services** and mapping to DTOs; keeps Mock mode identical.
- `Abstractions/Persistence/I{Company,Person,Engagement}Repository.cs` — repository interfaces
  (defined in Application or Abstractions) the DataAccess layer implements.

### 4. DataAccess — `src/Tedwren.DataAccess` (Dapper, dual-engine)
- `Connections/IDbConnectionFactory.cs` + `DbConnectionFactory.cs` — returns `SqlConnection` or
  `NpgsqlConnection` from `BackendOptions.Provider` + `ConnectionStrings`.
- `Dialects/ISqlDialect.cs` + `SqlServerDialect.cs` + `PostgresDialect.cs` — encapsulate the few
  real differences (identifier quoting, `uniqueidentifier`/`uuid`, `nvarchar(max)`/`jsonb`,
  paging). Most SQL is shared ANSI.
- `Repositories/RepositoryBase.cs` — shared async Dapper helpers over the factory + dialect.
- `Repositories/{Company,Person,Engagement}Repository.cs` — derive from the base.
- `Migrations/` + `db/sqlserver/*.sql` + `db/postgres/*.sql` — idempotent `Companies`, `Persons`,
  `Engagements` tables; **unique index on `Persons.PhoneNumber`** (SF-1) and on
  `Engagements(CompanyId, PersonId)` (SF-2); a small `MigrationRunner` applies them at API startup
  in `Database` mode (dev).

### 5. Api — `src/Tedwren.Api`
- `Endpoints/OrganisationEndpoints.cs` — minimal-API group `/api/organisation`: `GET /companies`,
  `GET /companies/{slug}`, `POST /companies`, `POST /operatives`, `POST /operatives/{id}:archive`
  / `:reactivate`; each calls `IOrganisationService`.
- `Program.cs` — bind `BackendOptions` (done Phase 7); register `IOrganisationService` = Mock impl
  when `Mode=Mock`, else real impl + repositories + `IDbConnectionFactory` + dialect for the
  configured provider; run migrations in `Database` mode.

### 6. Client — `src/Tedwren.Client`
- `Services/ApiOrganisationService.cs` — typed `HttpClient` impl of `IOrganisationService` calling
  `/api/organisation` (base URL from `wwwroot/appsettings.json:Api:BaseUrl`).
- `Program.cs` — read `DataSource:Mode`; register `MockOrganisationService` (in-proc, wraps sample
  data) when `Mock`, else `HttpClient` + `ApiOrganisationService`. Keep the five existing sample
  registrations for the not-yet-migrated pages.
- Migrate `Pages/Organisation/{Organisation,CompanyDetail,AddCompany}.razor` to inject
  `IOrganisationService` and `await` it; `AddCompany.Save` now calls `CreateCompanyAsync` (real
  create in Api mode; in-proc add in Mock mode). Option lists stay on `IFormSampleDataService`.
- **Mapping note:** DB-mode companies have no cards yet (Phase 9), so `CompliancePercent`/`Status`
  render as a neutral "pending" until Phase 9 — honest, not invented. Mock mode is unchanged.

### 7. Tests
- `Tedwren.Domain.Tests/PhoneNumberTests.cs` — normalisation & equality (UK/international/messy input).
- `Tedwren.Application.Tests/OrganisationServiceTests.cs` — SF-1/2/3 against mocked repositories:
  same phone / two companies → one person + two engagements, no cross-company read; same phone
  twice in one company → refused naming the existing; archive hides from register but keeps
  history; reactivate restores.
- `Tedwren.Api.Tests` (new, `WebApplicationFactory<Program>` in **Mock** mode) — `/api/organisation`
  happy paths; needs no DB, runs in CI and here.
- `Tedwren.DataAccess.Tests/OrganisationRepositoryTests.cs` — LocalDB integration, **per-test
  transaction rollback**, purpose-created data; a fixture probes a `TEDWREN_TEST_SQLSERVER`
  connection string and **skips** when absent (runs in CI/dev).

## Verification (Phase 8)
1. `dotnet build Tedwren.sln` — 0 errors, no new warnings.
2. `dotnet test` — Domain/Application/Api(mock) suites pass here; DataAccess integration skips
   without a SQL Server (runs green in CI/dev with LocalDB).
3. **Seam proof (this session, no DB):** run the API in `Mock`; run the client with `DataSource=Api`
   → Organisation list/detail/create are served over HTTP from the API's mock service and look
   identical to `DataSource=Mock`. Confirms UI + business layers are untouched by the flip.
4. **DB path (CI/dev):** set `DataSource:Mode=Database`, `Provider=SqlServer`, connection string →
   migrations create the schema; integration tests prove CRUD + SF-1/2/3 constraints; Postgres SQL
   validated where a Postgres instance is available (full parity is the Phase 18 gate).
5. **Regression:** all other pages (Workforce/Sites/Compliance/Audit/detail) and the default
   `Mock` client path render exactly as before.

## Out of scope for Phase 8 (deferred to their phases)
Cards/compliance computation (Phase 9); migrating Workforce/Sites/Compliance/Audit pages to async
contracts; real SMS/email; auth. Phase 8 proves the pattern on one slice only.
```
