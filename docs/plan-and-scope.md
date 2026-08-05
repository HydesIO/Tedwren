# Tedwren — Phased Development Plan & Scope of Works

## Context

Tedwren Ltd is building a UK construction **workforce compliance platform** delivered as
two independently saleable products (a subcontractor *time-and-attendance* product and a
main contractor *workforce-management* product) sharing one data foundation, followed by
seven numbered commercial modules. The definitive brief is **PRD v6.4**
(`TedwrenPRDv6_4.docx`, attached), which supersedes all prior specs.

The repository today is a **UI/UX foundation only**. **Six phases are complete on `origin/main`**
(PRs #1–#10), all of them **front-end/component work over mock data** — there is no server-side
code yet:

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
```
