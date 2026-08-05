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

## Outstanding / known issues
- ❗ **MUD0002 warnings** (pre-existing, in `TedwrenStepper.razor`, `AddOperative.razor`,
  `Inductions.razor`): `Variant`/`Linear`/`Color` on `MudStepper` and `Icon` on `MudStep` flagged
  by the MudBlazor analyzer. Not addressed in Phase 7 (backend scope). Review against the current
  MudBlazor `MudStepper`/`MudStep` API and correct in a focused UI pass.

## Planned (next)
- ⏳ **Phase 8 — Prove the data-access seam on one slice (SF-1, SF-2, SF-3).** Company + Person +
  Engagement (person keyed by normalised mobile number; per-company isolation; archive/reactivate),
  implemented mock + Dapper (SQL Server + PostgreSQL) behind the shared interface, with the
  Organisation/detail pages served over the database when `DataSource=Api`/`Database`. Establish the
  `PhoneNumber` value object (Q9), `IDbConnectionFactory`, shared repository base + dialects, and
  the LocalDB transaction-rollback integration harness. This locks the pattern later phases copy.

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
