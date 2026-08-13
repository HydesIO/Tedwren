# Forms Library — development plan & scope

> **Status:** Planned (not started). Sits in the PRD commercial-modules band and is **not part of
> either MVP**. This document is the phased plan of works for a customer-built, per-tenant Forms
> Library. It is subordinate to the PRD (`docs/TedwrenPRDv6_4.docx`, mirror `TedwrenPRDv6_4.md`) —
> where the two disagree, the PRD wins and the discrepancy is raised, not worked around.

## 1. Why this is being built

An Organisation (a tenant — modelled as **`Company`** in this codebase) needs to build its own
forms without developer help, store them per-tenant, complete them at organisation/site level and
inside the induction wizard, keep completed submissions as durable evidence in the database, and
optionally render them as clean, branded PDFs that can be emailed.

This maps directly to the PRD's **checklist and inspection engine** (`TedwrenPRDv6_4.md:509`):

> *"A configurable form: checkboxes, free text, numbers, dates, dropdowns, photographs,
> red/amber/green items, and a signature. Templates are built by the customer without developer
> help. Serves welfare checks, site inspections, plant inspections and audit checklists. **This is
> the same engine Phase 3 uses.**"*

It is the heart of **PRD-Phase 2 (Health, Safety & Compliance)** and is reused by **PRD-Phase 3
(Quality Assurance)**. The PRD is explicit that the builder is *"the easy half"*
(`TedwrenPRDv6_4.md:156`) — the assignment, scheduling and failure-alert mechanics are the
substance, so this plan covers the full engine, not just the authoring surface.

### Requirements traceability

The user's brief (11 points) and the PRD requirements it satisfies:

| # | User requirement | PRD / rule anchor |
|---|---|---|
| 1 | Create a customised Form → the org's Forms Library | Checklist engine `:509`; bespoke checklists `:156` |
| 2 | Full field spectrum, required-or-not, validators by default | `:509` (field types); MC-3 configurable capture `:371` |
| 3 | Templates stored in the DB per tenant | R15 tenant isolation; JSON-config convention (induction template) |
| 4 | Submissions stored in DB + optional branded PDF (logo top-left, clean) | QA PDF w/ signatures+timestamps `:522`; forms sync as PDFs `:156` |
| 5 | Assign to Sites, Operators, and the Induction wizard (multiple) | Assignable bespoke checklists `:156`; MC-4/MC-15 induction authoring |
| 6 | Completed forms can be emailed | Failed-check email w/ report attached `:156`; existing email kit |
| 7 | Complete at Organisation and Site level | Scope model (this plan); site-scoped templates precedent (`InductionTemplate.SiteId`) |
| 8 | Fully working code, no hallucinations | Engineering standards (CLAUDE.md); every phase builds & tests green |
| 9 | Professional by design, current design patterns, spacing/margins | `tokens.css`-only, reuse Forms suite, catalogue conventions |
| 10 | Full UI included | Builder + library + fill + submissions pages (§5) |
| 11 | Phases plan | §6 |

**Rules that constrain the design:** R4/R10/R16 (append-only, self-reconstructing evidence — drives
versioning & submission snapshots), R5 (any server-side scoring never sent to device), R11 (store
UTC, display UK local — timestamps on signed records), R12 (every scheduled job reports whether it
ran — scheduled checklists), R15 (tenant isolation), R18 (never "permitted/denied" language),
§10.1 Q2 (paid modules gated server-side, fail-closed — Forms is entitlement-gated).

> **Doc discrepancy raised (per CLAUDE.md):** `CLAUDE.md` still states *"backend work starts at
> Phase 7 … Phases 1–6 are complete."* In fact Phases 7–17, console-migration M1–M6 and follow-ups
> D1–D7 are delivered (`docs/plan-and-scope.md`, `TODO.md`). New work — including this Forms Library
> — therefore continues from **Phase 19**. This note flags the stale line rather than silently
> working around it.

## 2. The precedent to clone — `InductionTemplate`

The induction template is already a per-tenant, JSON-config-backed, site-scopable, wizard-integrated
template with an authoring/runtime DTO split — almost exactly the Forms Library shape. The plan
mirrors it end-to-end rather than inventing a new pattern.

- **Schema as typed records persisted to JSON** — `src/Tedwren.Domain/Entities/InductionTemplate.cs`,
  `InductionStep.cs`, `src/Tedwren.Domain/Enums/InductionStepKind.cs`
  (a `Kind` enum + `record(Id, Kind, Label, Required)` field descriptor).
- **JSON persistence** — `src/Tedwren.DataAccess/Repositories/InductionTemplateRepository.cs`
  serialises `StepsJson`/`QuestionsJson` via `System.Text.Json`; SQL is ANSI-portable so one repo
  class serves both SQL Server and PostgreSQL (the dialect only selects the migration-script folder).
- **Layering per new entity** (Site is the canonical trace): Domain entity →
  `Application/Persistence/I*Repository` → `DataAccess/Repositories/*Repository`
  (+ `Application/Persistence/InMemory/*` test double) → `Abstractions/Services/I*Service` +
  `Abstractions/Contracts/<feature>/*Dtos` → `Application/<feature>/*Service` →
  `Api/Endpoints/*Endpoints` → DI in `ApplicationServiceCollectionExtensions`,
  `DataAccessServiceCollectionExtensions`, `Program.cs`.
- **Tenant scoping is enforced in the service layer**, not the repository —
  `SiteService.ResolveTenantAsync()` → `ICurrentUserService.GetCurrentAsync().CompanyId`, filters
  reads, re-checks ownership on writes, returns 404 (never leaks) across tenants (R15). Repos are
  tenant-agnostic and take a `companyId`.
- **Secure-by-default** — `Program.cs` sets a `FallbackPolicy` requiring an authenticated user.
  Authoring/write endpoints add `.RequireAuthorization("RequireWrite")`; only genuinely worker-facing
  submission routes get `.AllowAnonymous()` (exactly as the induction *session* routes do, while the
  *template* routes stay authorised).

## 3. Data model

New domain under `src/Tedwren.Domain`. Every tenant-owned entity carries
`public required Guid CompanyId { get; init; }` (R15). Field layout and captured answers persist as
JSON columns (`nvarchar(max)` on SQL Server / `text` on PostgreSQL), following the established
`StepsJson` convention — no `jsonb`-specific mapping is used elsewhere yet, so plain string columns
match the codebase.

### Enums & value records (`Domain/Enums`, `Domain/Entities`)

- **`FormFieldKind`** — the full field spectrum, extensible by adding a case (never a rewrite):
  `ShortText`, `LongText`, `Number`, `Date`, `Time`, `Dropdown`, `MultiSelect`, `YesNo` (rendered as
  a switch, house rule), `RagStatus` (red/amber/green), `Photo`, `FileUpload`, `Signature`, and the
  display-only `Heading` / `Instruction`.
- **`FormField`** — `record(string Id, FormFieldKind Kind, string Label, string? HelpText,
  bool Required, string? ValidationJson, string? OptionsJson, int Order)`.
  `ValidationJson` carries per-kind validators (min/max, min/max length, regex, decimal places);
  `OptionsJson` carries dropdown / multi-select / RAG option lists. **Fields are required-validated by
  default** unless the author turns `Required` off (requirement 2).
- **`FormSectionDef`** — `record(string Id, string Title, IReadOnlyList<FormField> Fields, int Order)`.
  Renders through the existing two-column `FormSection` grid.

### Aggregate entities

- **`FormTemplate`** — `Id, CompanyId, Name, Description, int Version,
  FormTemplateStatus (Draft | Published | Archived), IReadOnlyList<FormSectionDef> Sections,
  DateTime CreatedUtc, DateTime UpdatedUtc`. **Versioned and append-only**: publishing a change writes
  a new version row; earlier versions are retained so historic submissions still resolve their exact
  form (R4/R10/R16, and the RAMS "earlier versions intact" rule `:506`).
- **`FormAssignment`** — `Id, CompanyId, FormTemplateId, FormScope (Organisation | Site | Operator |
  Induction), Guid? SiteId, Guid? PersonId, Guid? InductionTemplateId,
  FormSchedule (AdHoc | Daily | Weekly | Monthly), string? DueRule, string? FailureAlertEmail`.
  Drives requirements 5 & 7 and the failure-alert mechanic (`:156`). Multiple assignments per template
  are allowed (a form can go to several sites and the induction at once).
- **`FormSubmission`** — `Id, CompanyId, FormTemplateId, int FormTemplateVersion, FormScope Scope,
  Guid? SiteId, Guid? PersonId, string AnswersJson, string? SignatureJson,
  FormSubmissionStatus (Draft | Submitted | Approved | Rejected), DateTime SubmittedUtc,
  string SubmittedBy, string? ReviewNote`. **Append-only evidence** (R4/R10). `AnswersJson` snapshots
  the answered values keyed by field id; `FormTemplateVersion` is captured so a later PDF reconstructs
  exactly what was asked (R16). Approve/reject with a written reason mirrors the RAMS review flow
  (`:506`).
- **`FormSubmissionFile`** — `Id, CompanyId, SubmissionId, FieldId, FileName, ContentType, byte[] Bytes,
  DateTime UploadedUtc`. DB-stored blobs backing `Photo`/`FileUpload`/`Signature` fields — this is the
  concrete persistence the currently UI-only `TedwrenFileUpload` lacks (requirement 4, "stored on disk
  e.g. in the database").

### Persistence

- **Dapper repositories** mirroring `InductionTemplateRepository` — `FormTemplateRepository`,
  `FormAssignmentRepository`, `FormSubmissionRepository` — each a single ANSI-portable class with a
  private `Row` record and `ToEntity`/`ToParameters` mappers, serialising the JSON columns with
  `System.Text.Json`.
- **In-memory doubles** under `Application/Persistence/InMemory` for fast unit/API tests.
- **Schema** — EF Core owns DDL going forward (`docs/ef-migrations.md`): new `SchemaRecords`
  (`FormTemplateRecord`, `FormAssignmentRecord`, `FormSubmissionRecord`, `FormSubmissionFileRecord`),
  `DbSet`s + `ToTable`/index mappings in `TedwrenDbContext`, and an EF migration. The parallel
  idempotent SQL scripts continue the numbered series:
  `Migrations/Scripts/{SqlServer,Postgres}/018_forms_library.sql`. Tables: `FormTemplates`,
  `FormAssignments`, `FormSubmissions`, `FormSubmissionFiles`.

## 4. Services & API

- **Services** (each behind an interface, SRP, tenant-scoped exactly like `SiteService`):
  `IFormTemplateService` (create/edit/publish/version/archive, list library),
  `IFormSubmissionService` (start/save-draft/submit/approve/reject, list, fetch with files),
  `IFormAssignmentService` (assign to scope, list assignments, resolve "what forms apply here"),
  plus `IFormPdfRenderer` and reuse of `IEmailSender` for output.
- **API** — `src/Tedwren.Api/Endpoints/FormEndpoints.cs`, `MapGroup("/api/forms")`:
  - `GET/POST /templates`, `GET /templates/{id}`, `PUT /templates/{id}`, `POST /templates/{id}/publish`,
    `POST /templates/{id}/archive` — authoring, `.RequireAuthorization("RequireWrite")`.
  - `GET /templates/{id}/fill` — the runtime (answer-free) form shape.
  - `POST /submissions`, `GET /submissions`, `GET /submissions/{id}`,
    `POST /submissions/{id}/approve|reject`, `GET /submissions/{id}/pdf`,
    `POST /submissions/{id}/email` — submission lifecycle & output.
  - `GET/POST/DELETE /assignments` — assignment management.
  - A worker-facing submission route (kiosk/phone at point of work) is the only member marked
    `.AllowAnonymous()`, matching the induction session pattern; everything else inherits the
    authenticated fallback.
- **DTOs** — `Abstractions/Contracts/Forms/*` as `sealed record`s with an **authoring vs runtime
  split** (the runtime "fill" DTO never carries anything scored/answer-side, mirroring the induction
  authoring-vs-device split for R5). Services map Domain ↔ DTO; endpoints only touch DTOs.
- **DI & wire-up** — `AddFormCore()` in `ApplicationServiceCollectionExtensions`, repo registrations
  in `DataAccessServiceCollectionExtensions`, `app.MapFormEndpoints()` + `builder.Services.AddFormCore()`
  in `Program.cs`.

## 5. UI

Reuse-first: render every field on the existing `FormField` chrome + `Tedwren*` wrappers; group with
`FormSection`; list with `DataTable<TItem>`; multi-step build/complete with `TedwrenStepper`; actions
via `FormActions`. `tokens.css` is the only source of colour/spacing (adequate margins per
requirement 9); switches over checkboxes; the **live-binding rule** applies throughout
(`@bind-Value`, or `Value` **paired with** `ValueChanged`; never a bare one-way `Value=` on a mutable
value — the onboarding "Company type" bug).

### New reusable components (catalogued in `docs/component-catalogue.md`)

- **Field wrappers missing from the current Forms suite** — `TedwrenNumericField` (over
  `MudNumericField`), `TedwrenRadioGroup` + a RAG control, `TedwrenSignaturePad`, and a single-date
  `TedwrenDatePicker`. These fill the gaps the exploration found (pages currently use raw MudBlazor
  for numeric/radio, and there is no signature capture).
- **`DynamicFormRenderer`** — switches on `FormFieldKind` to render the right wrapper per field with
  its validators. This is the render-per-kind switch the induction take page
  (`Pages/Inductions/InductionTake.razor`) does not yet have; it is the runtime heart of the library.
- **`FormBuilder`** — the authoring surface: add / reorder / remove sections and fields, choose kind,
  toggle required, set validators, edit options. Modelled on the induction builder page
  (`Pages/Inductions/Inductions.razor`), which already runs an `AddQuestion()` repeater.

### New client pages (`src/Tedwren.Client/Pages/Forms/`)

- **`FormsLibrary.razor`** — the tenant's template library: a `DataTable<FormTemplateDto>` with
  status/version, plus new/edit/publish/archive actions.
- **`FormBuilderPage.razor`** — create/edit a template via `FormBuilder` inside a `TedwrenStepper`
  (Details → Build → Review).
- **`FormFill.razor`** — complete a form at organisation or site level via `DynamicFormRenderer`,
  capturing files/signature.
- **`FormSubmissions.razor`** — submissions list (`DataTable`) with view / download-PDF / email /
  approve-reject actions.
- Navigation entries added through `src/Tedwren.Client/Services/ShellChrome.cs`, gated by the Forms
  entitlement (SF-22 nav gating already exists).

### Induction integration (requirement 5)

- Add `InductionStepKind.Form` referencing a `FormTemplateId` inside `StepsJson`, so a form becomes a
  step in the induction.
- Surface it in the onboarding wizard's Induction `MudStep`
  (`src/Tedwren.Client/Pages/Onboarding/Onboarding.razor`, ~L209) and the induction builder page, so
  one or more forms can be attached to the induction.

## 6. PDF & email

- **PDF (QuestPDF).** Add the **QuestPDF** NuGet package (the repo's first third-party PDF
  dependency; Community licence is free under the revenue threshold) and a new
  `src/Tedwren.Application/Export/FormPdfRenderer.cs`. It renders a branded document: **Tedwren logo
  top-left** (`src/Tedwren.Api/Assets/logo.png`), a header (form name, submitter, site, reference,
  UTC-stored/UK-displayed timestamp per R11), one clean block per section/field with the captured
  answers, embedded photos, and the signature image. Professional layout with adequate spacing and
  margins (requirement 9). The existing framework-only `PdfWriter` is **kept** for the tabular
  compliance-pack exports it already serves; QuestPDF is added specifically for rich, branded form
  output which monospaced text cannot produce.
- **Email.** Reuse `IEmailSender.SendHtmlAsync` and the branded `Notifications/Email/` kit; add a
  `FormSubmissionEmail` template that attaches the generated PDF. The **failure-alert** path emails the
  assignment's `FailureAlertEmail` with the failed report attached when a required or RAG check fails
  (`:156`). Delivery flows through the existing outbox/Resend provider switch.

## 7. Phased plan of works

New work continues the repo's phase numbering at **Phase 19**. Each phase is independently testable,
builds warning-clean, and must not break earlier phases or previously completed work. Tests follow the
established conventions (Domain `[Fact]`/`[Theory]`; Application over in-memory doubles; API over
`WebApplicationFactory<Program>` with the in-memory host; DataAccess skip-guarded against SQL Server;
Client bUnit render smoke).

| Phase | Scope | Key deliverables & tests |
|---|---|---|
| **19 — Domain & persistence** | The schema and its storage. | `FormFieldKind`, `FormField`, `FormSectionDef`, `FormTemplate`; `FormTemplateRepository` (JSON) + in-memory double; `018_forms_library.sql` (both engines) + EF migration + `SchemaRecords`/`DbSet`. Domain + repository (skip-guarded) tests. |
| **20 — Template service & API** | Authoring behind interfaces. | `IFormTemplateService`/impl (tenant-scoped, versioned publish/archive); `Contracts/Forms` DTOs (authoring vs runtime split); `FormEndpoints` template routes (`RequireWrite`); DI wire-up. Application + API tests (incl. cross-tenant 404, R15). |
| **21 — Builder UI & field wrappers** | The authoring surface + missing inputs. | `TedwrenNumericField`, `TedwrenRadioGroup`/RAG, `TedwrenSignaturePad`, `TedwrenDatePicker`; `FormBuilder`; `FormsLibrary.razor` + `FormBuilderPage.razor`; catalogue entries; nav. bUnit render tests. |
| **22 — Fill & submissions** | Capturing completed forms. | `DynamicFormRenderer`; `FormFill.razor`; `IFormSubmissionService` + `FormSubmission`/`FormSubmissionFile` persistence (append-only) + endpoints; `FormSubmissions.razor`. Validation (required-by-default), service, API tests. |
| **23 — PDF & email** | Branded output & delivery. | QuestPDF `FormPdfRenderer` (logo, signature, timestamps); download endpoint; `FormSubmissionEmail`; email action over the outbox. PDF-shape + email-outbox tests. |
| **24 — Assignment, scheduling & induction** | The "hard half". | `FormAssignment` model/service/API; assign to Site/Operator/Organisation; `InductionStepKind.Form` + onboarding & builder surfacing; scheduled checklists + failure-alert respecting R12 (job-ran reporting). End-to-end tests. |
| **25 — Hardening** | Production-readiness. | Entitlement-gating (server-side, fail-closed, §10.1 Q2); PostgreSQL parity run; a small **default template library** shipped (daily site diary, plant checklist, welfare checklist — `:157`, "ship a template library, not an empty builder"); accessibility & spacing review. |

## 8. Reuse map (build on these; do not reinvent)

| Need | Existing asset |
|---|---|
| Per-tenant JSON-config template | `InductionTemplate` + `InductionTemplateRepository` |
| Field chrome / label / reserved validation space | `FormField.razor` |
| Text / select / autocomplete / switch / date-range / file / stepper / section / actions | `Tedwren.UiComponents/Forms/*` |
| Sortable/filterable submission lists | `DataTable<TItem>` + `DataColumn<TItem>` |
| Tenant resolution & ownership checks | `SiteService.ResolveTenantAsync`, `ICurrentUserService` |
| Endpoint group pattern & auth policies | `SiteEndpoints`, `Program.cs` (`RequireWrite`, `FallbackPolicy`) |
| Email delivery + branded templates | `IEmailSender`, `Notifications/Email/*` |
| Colour / spacing tokens & theme | `tokens.css`, `TedwrenTheme` |
| Logo asset | `src/Tedwren.Api/Assets/logo.png`, `wwwroot/images/logo-icon.svg` |

## 9. Open questions to resolve before build

- **QuestPDF licensing** — confirm the Community licence threshold is acceptable, else budget for the
  paid tier (this is the only new third-party dependency introduced).
- **File/photo storage ceiling** — DB blobs are chosen for parity with "stored in the database"; if
  large photo volumes are expected, a later move to object storage with R9-compliant (no permanent
  public URL) access should be considered. Start with DB blobs.
- **Scheduling engine** — whether scheduled checklists reuse the existing job/heartbeat mechanism
  (R12) or need a dedicated scheduler is a Phase 24 design decision.
- **Default template contents** — per §10.2 (Q21/Q22 principle), the shipped starter templates should
  be confirmed with the client rather than invented.
