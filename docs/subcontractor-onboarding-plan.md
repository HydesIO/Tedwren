# Subcontractor onboarding programme — plan & PRD discrepancy note

> Status: in progress. Source of truth is `docs/TedwrenPRDv6_4.docx` (PRD v6.4); this document references PRD
> IDs (SF-/SUB-/MC-/R-) rather than reproducing prose. It implements the attached *"Subcontractor Onboarding —
> Process Map & Build Spec"* (four actors, five gates, three registers, one notification engine) as a
> main-contractor-driven onboarding programme, sequenced into independently-testable phases.

## Why this exists

A signed-in **main contractor** needs a wizard to set up & configure a **subcontractor** (spec Stage 1), plus
the end-to-end journey the spec describes. The centrepiece is the MC-facing config wizard; the full deliverable
is the whole process, phased so each increment is usable and never breaks a completed phase.

## Build posture (agreed with the product owner)

- **Full UI over the spec, PRD-aligned backend.** Every screen is built so it is demoable; PRD-aligned parts
  wire to real backend, and parts beyond PRD v6.4 are built UI-first with stubbed/flagged backend behind a
  `subcontractor-onboarding` entitlement (default off), with the discrepancy documented below.
- **Full platform-admin CRUD** for the compliance master data (document headings, SSIP schemes,
  "other requirements", accreditations, trade→accreditation map), incl. org-scoped custom entries.
- **Reuse-first.** Extend the existing **TradeInvite** vertical, the induction/RAMS/expiry engines, the
  `TedwrenStepper`/Forms/dialog kit; ship both an EF migration (after `AddConsoleRefresh`) and an idempotent
  raw script (`038+`, both engines) per schema change — the `SchemaParityTests` guard enforces this.

## PRD discrepancy note (raised, not worked around — CLAUDE.md)

| Item | Status | Resolution |
|---|---|---|
| "MC configures a subcontractor" as a first-class config object | **Beyond PRD v6.4** | Build UI + persistence; enforcement behind the `subcontractor-onboarding` flag; propose a new PRD §5/§8 requirement. |
| **Access period** (default 12 months) | **Beyond PRD** | Capture + persist; **no enforcement** yet (`// TODO` + flag). |
| **RAMS review cycle** (recurring 6/9/12 months) | **Beyond PRD** — §8.2 RAMS review is per-submission, `hse`-gated | Persist + reminder-only scheduler behind the flag. |
| **Subcontractor-issued induction** | **Conflicts with §6.1 permanent non-goal** | Resolve for the PRD: the operative completes the **MC's** induction template; do **not** build sub-issued induction. |
| Device binding (console config) | Relates to **R17** (biometrics/DPIA = Phase 5) | Console side = config toggle only; real binding is the existing operative OTP/device mechanism. |
| Spec actors SubcontractorAdmin / SiteManagement | Not in the `AccessRole` model | Map onto existing roles (MC Admin → `Administrator`; Site Management → `SiteManager`; Subcontractor Admin → anonymous TradeInvite link holder; Operative → `Person`+`Engagement`); **no new roles**. |

## Phases

1. **Master-data CRUD foundation** — platform-admin + org-scoped lists (document headings, SSIP schemes,
   "other requirements") the wizard/gates consume; accreditations + trade→accreditation map. **[Done]** — see
   below.
2. **MC config/onboarding wizard + `SubcontractorOnboardingConfig`** — the headline `TedwrenStepper` wizard
   (`/subcontractors/onboard`); replaces the hardcoded `RequestedDocumentTypes` in `TradeOnboardingService` with
   a config-driven fallback. **[Done]** — new `SubcontractorOnboardingConfig` vertical + orchestrator reusing the
   trade-invite flow + `/api/subcontractor-onboarding` + client wizard; SSSTS/SMSTS + induction captured (wired
   in Phase 5), access period + RAMS cycle persisted (enforced in Phases 3/4). See `TODO.md` SO-2.
3. **Subcontractor-side upload + Gate 1** — upload against configured headings; all required-before-work docs
   valid → unlock add-operative (fail-closed, R2). **[Done]** — shared `Gate1Evaluator`;
   `EvaluateGate1Async` + `/gate1` endpoint; `TradeInviteViewDto.Gate1`; `AddOperativeByLinkAsync` (fail-closed)
   + `by-link/{token}/operatives`; `TradeOnboard.razor` gate panel + gated add-operative. See `TODO.md` SO-3.
4. **RAMS review cycle** — bridge sub RAMS upload into the existing `RamsSubmission` review; add
   Approved-with-comments + live version; recurring cycle reminder behind the flag. **[Done]** — RAMS bridge in
   `TradeOnboardingService`; `RamsStatus.ApprovedWithComments` + `RamsSubmission.IsLive` (live-version pointer);
   `ApproveWithCommentsAsync`/`RegisterFromDocumentAsync`; flag-gated `RamsReviewCycleReminderJob` + due-list.
   The recurring review cycle is beyond PRD v6.4 (§8.2 is per-submission), so the reminder engine is gated behind
   the `subcontractor-onboarding` module and fails closed (reminder-only, never expires an approval). See
   `TODO.md` SO-4.
5. **Operative induction & accreditation gates (G2/G3/G4)** — reuse OTP/device (G2), server-scored quiz (G4,
   R5), SF-11 mandatory accreditation (G3). Mobile track: `Tedwren.Mobile.Core` + `Tedwren.Web.App` in lockstep.
   **Split into 5a (induction / Gate 4) + 5b (accreditation / Gate 3).**
   - **[5a Done]** Operative induction take-flow (native + emulator, lockstep): config gains `InductionTemplateId`
     (resolve-or-create the MC's own template, §6.1); `AttemptLimit` enforced + `ResetAsync` re-grants;
     `GetOrStartForPersonAsync` + `OperativeInductionService`; `/api/mobile/inductions/*` (`RequireOperative`, core);
     `InductionApiClient`; `Induction.razor` + `InductionPage.cs`. Migration `042_*` + EF `AddInductionTemplateLink`. See `TODO.md` SO-5a.
   - **[5b Done]** Accreditation / competency (Gate 3): SF-11 `TradeQualificationRequirement` gains
     `LegalMandatory`/`ClientRequired`/`CompanyId`, `QualificationType` gains `CompanyId` (org-custom, Q21),
     `QualificationCard` gains `CaptureClientId` (offline idempotency); **Gas Safe** seeded + mapped to "Gas Engineer"
     as legally-mandatory, with an idempotent seeder upsert. Pure `Gate3Evaluator` (only a missing/expired
     legally-mandatory accreditation blocks; advisory ones reported), reused by `EvaluateGate3Async`/`GetShortfallAsync`.
     Operative card upload `/api/mobile/cards/*` (`RequireOperative`, core) + `CaptureApiClient` card methods +
     `CardOutboxHandler` (offline outbox, lockstep both heads); emulator `AddAccreditation.razor` + native `AddCardPage.cs`,
     "Add accreditation" entry + `MyCards` "still needed" shortfall (`OperativeDetailDto.MissingQualifications`).
     Platform-admin CRUD (Q21) for the type library + trade→accreditation map (gated like `MasterDataService`;
     type-delete guarded when referenced): console `/api/qualifications/{types,requirements}` endpoints,
     `ApiQualificationService`, `AdminAccreditations.razor` + dialogs + nav. Migration `043_accreditation_map.sql` + EF
     `AddAccreditationMap`. **Site-entry turnstile enforcement of G3 deferred to Phase 7** (the capability + evaluator
     ship now; the `SiteEntryService.CheckCardsAsync` change lands with the RAMS Gate-5 rework). See `TODO.md` SO-5b.
6. **Registers + one notification engine** — competency/induction/RAMS as projections over the existing
   expiry engine (SF-9); alerts to operative (SMS) + site team (email).
7. **Site sign-in Gate 5** — replace the stubbed `SiteEntryService.CheckRamsAsync` with a real signed-approved-
   RAMS check (MC-8 fifth check where `hse` held); new `RamsAcknowledgement`. Mobile track in lockstep.

## Phase 1 — as built

New compliance master-list vertical (`MasterListItem`), mirroring the `TradeInvite`/`ReferenceData` patterns:

- Domain `src/Tedwren.Domain/Entities/MasterListItem.cs`; contracts
  `src/Tedwren.Abstractions/Contracts/MasterData/MasterDataDtos.cs` + service
  `src/Tedwren.Abstractions/Services/IMasterDataService.cs` (with `MasterListKeys`).
- Application `src/Tedwren.Application/MasterData/{MasterDataService,MasterListSeed}.cs`; port
  `Persistence/IMasterListItemRepository.cs` + double `Persistence/InMemory/InMemoryMasterListItemRepository.cs`
  (seeded from `MasterListSeed`).
- Dapper `src/Tedwren.DataAccess/Repositories/MasterListItemRepository.cs`; EF `MasterListItemRecord` +
  `TedwrenDbContext` mapping; dual migrations `Migrations/Scripts/{SqlServer,Postgres}/038_master_list_items.sql`
  + EF `AddMasterListItems`.
- API `src/Tedwren.Api/Endpoints/MasterDataEndpoints.cs` (`/api/master-data`; reads under the fallback policy,
  writes `RequireWrite`, global writes platform-admin-only enforced in the service, R15).
- Client `src/Tedwren.Client/Services/ApiMasterDataService.cs`; platform-admin management page
  `Pages/Admin/AdminMasterData.razor` (+ `EditMasterListItemDialog.razor`), nav entry in `ShellChrome`.
- Tests `tests/Tedwren.Application.Tests/MasterDataServiceTests.cs` (global vs org visibility, platform-admin-
  only shared writes, cross-tenant isolation, soft delete); `SchemaParityTests` green for both engines.

PRD IDs: SF-11, MC-17, Q21, R15.
