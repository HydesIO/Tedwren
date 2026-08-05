# TODO — Tedwren development checklist

Working checklist for delivery. **Source of truth is `docs/TedwrenPRDv6_4.docx` (PRD v6.4)**;
delivery sequence and phase definitions are in `docs/plan-and-scope.md`. Update this file whenever
work is started, completed, deferred or newly identified. Completed items note what changed and
the phase/area. PRD requirement/rule IDs (SF-/SUB-/MC-/R-) are referenced, not reproduced.

Legend: ✅ complete · 🔄 in progress · ⏳ planned · ⏸️ deferred · ❗ outstanding/known issue

---

## Completed

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

### Phase 7 — Governance & backend scaffolding (this change)
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

### Phase 8 — Data-access seam on the first slice (SF-1, SF-2, SF-3) (this change)
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
- ⏳ **Phase 9 — Qualification cards & competency (SF-5–SF-8, SF-10–SF-12).** Card capture with
  suggested-not-silent reads; three visibly-distinct states; named confirmation; status computed from
  expiry; trade→required-qualification lists; default library; renewal supersedes. This also lets the
  organisation compliance figures move from `Pending` to computed values. Follow the Phase 8 pattern
  (Domain → Abstractions → Application → DataAccess → API → Client) on the next slice.

## Later (per `docs/plan-and-scope.md`)
- ⏳ Phases 9–13 — Shared Foundation (cards & competency; expiry engine & job heartbeat; sites,
  boundaries & dispersed schemes; sign-in/out & attendance; roles, audit & module entitlements).
- ⏳ Phases 14–15 — Subcontractor MVP (timesheets; compliance pack). *Product saleable.*
- ⏳ Phases 16–17 — Main Contractor MVP (induction & consent; site-entry decision, competency
  cover & muster). *Product saleable.*
- ⏳ Phase 18 — Hardening + PostgreSQL launch gate.
- ⏳ Phases 19+ — PRD commercial modules (CSCS, HSE, QA, Pay, Identity, Sharing, Integrations).

## Deferred (PRD-directed)
- ⏸️ Cross-company sharing surface (PRD-Phase 6) — consent capture (MC-20) is in the MC MVP because
  it cannot be retrofitted, but the sharing surface needs market density. Decision open (PRD §6.3).
- ⏸️ Bulk/spreadsheet import (SUB-3) — start without it; add only if a real customer is blocked.
- ⏸️ Worker self-service app (R1/Q8) — a question, not a prohibition; not in either MVP.
