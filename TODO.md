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

### Phase 9 — Qualification cards & competency (SF-5–SF-8, SF-10–SF-12) (this change)
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

### Phase 10 — Expiry engine, warning schedule & job heartbeat (SF-9, SF-21, SUB-5, R12) (this change)
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

### Phase 11 — Sites, boundaries & dispersed schemes (SF-6, SF-14, SF-25, SF-26) (this change)
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

### Phase 14 — Timesheets (SUB-7–SUB-12, SUB-27, MC-24, R16, R18) (this change)
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
- ⏳ **Phase 15 — Compliance pack (SUB-13–SUB-26, R7, R8, R9).** Pack builder over selected operatives/site/
  date-range; **not-site-ready problems surfaced before send** with explicit acknowledgement (SUB-14); contents
  chosen not automatic; output as web link + PDF + ZIP with identical content, 25 operatives ready < 1 min (SUB-16);
  **recipient needs no account** (R8); passcode + sender-set expiry (30-day default, SUB-18/Q12); open/download
  tracking (SUB-20); revoke (SUB-21); **fixed-at-send, re-issue supersedes** (R7); no permanent public asset URLs
  (R9); send restricted to nominated roles (SUB-22). Completes the Subcontractor MVP (product saleable). Follow the
  Phase 8–14 layered pattern.
- ⏳ **Follow-ups (non-blocking):** timesheet detail/correction UI (line-level edit) and operative self-service
  hours view (SUB-27) over the API; a "configurable approval" settings surface (SUB-9 line/site/project/all);
  navigation gating over `/api/entitlements` (SF-22 — no locked door);
  audit "Export CSV" button + free-text box on `AuditLog.razor` (backend supports both already);
  a console "on-site"/attendance view over `/api/attendance`; SiteDetail page
  over `ISiteService`; qualification cards on the
  Operative-detail page and a Dashboard/Notifications panel over `/api/expiry` (backends + tests already in
  place); wire company/operative compliance % from cards into the Phase 8 `OrganisationService`; real
  SMS/email providers (PRD-Phase 7); company insurance/accreditation docs in the digest (needs SUB-4);
  real card-image storage (R9).

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
