# TODO — Tedwren development checklist

Working checklist for delivery. **Source of truth is `docs/TedwrenPRDv6_4.docx` (PRD v6.4)**;
delivery sequence and phase definitions are in `docs/plan-and-scope.md`. Update this file whenever
work is started, completed, deferred or newly identified. Completed items note what changed and
the phase/area. PRD requirement/rule IDs (SF-/SUB-/MC-/R-) are referenced, not reproduced.

Legend: ✅ complete · 🔄 in progress · ⏳ planned · ⏸️ deferred · ❗ outstanding/known issue

---

## Completed

### Deferred items, Phase D2: self-service operative onboarding link (this change)
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

### Organisation onboarding wizard (this change)
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

### UX completeness pass — user management, UI defect closure & EF migrations tooling (this change)
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

### Phase 17 — Site-entry decision, competency cover & muster (MC-8–MC-14, MC-28, R2, R3, R10, R14) (this change)
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
  - Real SMS/email providers (PRD-Phase 7); company insurance/accreditation docs in the digest (needs
    SUB-4); real card-image storage (R9).
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

## Deferred (PRD-directed)
- ⏸️ Cross-company sharing surface (PRD-Phase 6) — consent capture (MC-20) is in the MC MVP because
  it cannot be retrofitted, but the sharing surface needs market density. Decision open (PRD §6.3).
- ⏸️ Bulk/spreadsheet import (SUB-3) — start without it; add only if a real customer is blocked.
- ⏸️ Worker self-service app (R1/Q8) — a question, not a prohibition; not in either MVP.
