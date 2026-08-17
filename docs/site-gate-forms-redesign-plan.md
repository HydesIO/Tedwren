# Site Gate, Forms, Permits, Inductions & Loading-State Redesign — Plan

## Context

Two problems drive this work:

1. **Blank screens during API calls.** The app already has a sanctioned skeleton pattern
   (`LoadingSkeleton`, auto-applied by `DataTable`), and those skeletons are the desired behaviour.
   But several pages render nothing — or bare "Loading…" text — while their API calls are in
   flight: the **Dashboard** (no loading state at all), the **Site Gate** page (empty operatives
   list + null muster during a multi-step load), the **detail pages** (Site/User/Operative — header
   only, blank body), and the form/onboarding "Loading…" text pages. The rule to enforce: *there
   must always be a visible indicator that something is happening — never a blank screen.*

2. **Several console pages are clunky and need a UX redesign** — Site Gate, Forms editor, Permits,
   and Inductions — plus replacing comma-separated option entry with proper chips.

The backend already supports everything below (multiple induction templates; form sections/panels
end-to-end). These are almost entirely **client + UiComponents** changes — no domain/DTO/API
changes except a small chip-options helper. `docs/TedwrenPRDv6_4.docx` remains the source of truth;
this is UI/UX hardening over existing requirements (Site Gate = MC-8/R10; Inductions = MC-3/MC-4;
Permits; Forms Library). Follow the CSS-token rule (`tokens.css` only — no literals) and the
one-`.razor.css`-per-component convention.

---

## Workstream 1 — Never a blank screen (reusable loading wrapper + fix offenders)

**New component:** `src/Tedwren.UiComponents/Feedback/AsyncContent.razor` (+ `.razor.css`).
Generalises the switch already proven in `DataTable.razor` (lines 82-99):

```razor
@if (Loading)               { <LoadingSkeleton Variant="@Variant" Rows="@Rows" /> }
else if (Error is not null) { <BannerAlert Severity="Danger" ... Action="Retry" /> }
else if (IsEmpty)           { @EmptyContent (or a default EmptyState) }
else                        { @ChildContent }
```

Parameters: `Loading` (bool), `Variant` (`SkeletonVariant`, default `Card`), `Rows`, optional
`Error`/`OnRetry`, optional `IsEmpty`/`EmptyContent`, `ChildContent`. Reuses existing
`LoadingSkeleton`, `BannerAlert`, `EmptyState`. Token-based styling + reduced-motion guard, mirroring
`LoadingSkeleton.razor.css`.

**Apply across every offender** (lists already skeleton via `DataTable`, so they're fine):

- `Pages/Dashboard/Dashboard.razor` — add `_loading`; wrap the KPI row in `AsyncContent Variant="Kpi"`
  (the `Kpi` skeleton variant already exists and is unused) and the risk table / donut / feed in
  `Card`/`List` variants.
- `Pages/SiteGate/SiteGate.razor` — wrap operatives + muster in `AsyncContent` (ties into Workstream 2).
- `Pages/Sites/SiteDetail.razor`, `Pages/Users/UserDetail.razor`, `Pages/Workforce/OperativeDetail.razor`
  — replace the header-only/blank-body loading state with `AsyncContent Variant="Card"`.
- `Pages/Forms/FormFill.razor`, `Pages/Forms/FormBuilderPage.razor`, `Pages/Onboarding/SelfOnboard.razor`,
  `Pages/Organisation/QualificationCardsDialog.razor` — replace plain "Loading…" text with a skeleton.
- `Pages/Notifications/Notifications.razor` — guard the empty `ActivityFeed` render while `_loading`.

Add `AsyncContent` to `docs/component-catalogue.md`.

## Workstream 2 — Site Gate redesign

File: `Pages/SiteGate/SiteGate.razor` + **new** `SiteGate.razor.css` (the `gate__*` classes are
referenced but **never defined** today — no CSS file exists — which is why spacing looks wrong).

- **Operative entry blocks:** render as a responsive CSS-grid of cards/buttons with a real gap
  (`--spacing-*` tokens), so entry blocks are visually separated, with a clear active/selected state.
- **Decision result:** give the result block generous top spacing so it is clearly separate from the
  operative grid; keep the `BannerAlert` + checks table.
- **Manager override:** move into a right-aligned action row (`display:flex; justify-content:flex-end`)
  with proper spacing — not a stray button under the table.
- **On site now → full `DataTable<MusterPersonDto>`:** sortable Operative / Property / On-site-since
  columns, built-in skeleton + `EmptyState` ("Nobody on site"). Keep the competency-cover `StatusPill`
  row above it. Data unchanged (`MusterDto`).
- Add `_loading` and wrap via Workstream 1's `AsyncContent`.

## Workstream 3 — Forms editor: streamline + question grouping (panels)

Provide the **authoring ability to group questions into panels** (functional grouping), built on the
existing `FormSectionDef`/`FormSectionDto` model — no migration.

Files: `Pages/Forms/FormBuilderPage.razor`, `Pages/Forms/FormBuilder.razor` (+ `.razor.css`),
`Pages/Forms/FormEditModel.cs`.

- **Form details layout:** stack description **below** the name (not the current side-by-side 2-col
  grid) and make the description textarea taller (`Lines="2"` → `Lines="4"`). Make these two fields
  single-column.
- **Panels/grouping UX:** render each section as a distinct, clearly-titled **panel** (card with
  header, editable panel name, collapse/expand, remove) containing its own question list, an
  **"Add question to this panel"** action, and a top-level **"Add panel/group"** action. Make grouping
  obvious and easy — this is the core of the request. Support reorder/move of questions within a panel.
- **Tighter question spacing:** reduce inter-field gaps so questions in a panel read as a grouped set.
  Yes/No etc. unchanged functionally.
- **Options as chips** (Dropdown/MultiSelect): replace the "Options (comma separated)" text field with
  the new chip input (Workstream 6).

## Workstream 4 — Permits: list + separate Issue screen

Mirror the existing Forms split (`FormsLibrary.razor` list ↔ `FormBuilderPage.razor` editor).

- `Pages/Permits/Permits.razor` → **list only**: keep the `DashboardCard` + `DataTable<PermitDto>`;
  remove the inline issue form + `FormSection`s + `FormActions`; add a `PageHeader` `<Actions>` filled
  **"Issue permit"** button → `Href="/permits/new"`.
- **New** `Pages/Permits/IssuePermit.razor` with `@page "/permits/new"`: move the current issue-form
  markup + `@code`; on success `Nav.NavigateTo("/permits")` instead of in-place reset.
- No nav change needed (the `/permits` entry stays; the sub-route is reached via the button).

## Workstream 5 — Inductions: list + builder (multiple inductions)

Backend already supports multiple templates (`IInductionService.GetTemplatesAsync`,
`CreateDefaultTemplateAsync`, `GetTemplateForEditAsync(templateId)`), so this is **client-only**.
Currently `Inductions.razor` auto-loads the company's *first* template and edits it in place.

- **New list page** `Pages/Inductions/Inductions.razor` at `@page "/inductions"`:
  `DataTable<InductionTemplateDto>` (name, applies-to-site, mandatory, validity, updated) with an
  **"Add induction"** header action → `/inductions/new`, and per-row **Edit** → `/inductions/{id}/edit`.
  This supports *different inductions per site type*, which the current single-template UI cannot.
- **Rename current builder** to `Pages/Inductions/InductionBuilder.razor` with
  `@page "/inductions/new"` and `@page "/inductions/{TemplateId:guid}/edit"` (same dual-route pattern as
  `FormBuilderPage.razor`). "new" calls `CreateDefaultTemplateAsync` then edits; "edit" loads by id.
  On save → `Nav.NavigateTo("/inductions")`.
- Nav (`ShellChrome.cs`) `/inductions` entry unchanged.
- **Quiz options as chips** (Workstream 6), replacing "Options (comma separated)".

## Workstream 6 — Editable chip input (replaces comma-separated options)

**New component:** `src/Tedwren.UiComponents/Forms/ChipInput.razor` (+ `.razor.css`). Rounded-rectangle
chips (`--radius-control`) each showing an option's text, an assigned **ID** badge, and a remove (×);
a text box + Enter/Add appends a new chip. `@bind-Value` over `List<ChipOption>` (`Id`, `Text`).
There is no interactive chip today (`StatusPill`/`RiskChip` are display-only), so this is genuinely
new; follow their token + scoped-CSS conventions. Catalogue it in `docs/component-catalogue.md`.

- **Forms:** `FormEditModel.FieldEdit` — replace the `OptionsCsv` round-trip
  (`OptionsToCsv`/`CsvToOptionsJson`) with a `List<ChipOption>` ⇄ `OptionsJson` mapping that persists
  the assigned IDs into the existing `OptionsJson` (extend the JSON shape from a string array to
  `{id,text}` objects, with back-compatible read of plain strings). Wire `ChipInput` into
  `FormBuilder.razor`.
- **Inductions:** the builder `QuestionEdit` — replace `OptionsCsv` (`string.Join(", ")` / `Split(',')`)
  with the chip list mapped to `InductionQuizAuthoringDto.Options`. Verify `CorrectOptionIndex` still
  maps to the chosen chip.

---

## Sequencing

1. Workstream 6 (ChipInput) and Workstream 1 (AsyncContent) first — shared building blocks.
2. Then Workstreams 2–5, each independently testable, each not breaking existing behaviour.

Delivered on branch `claude/site-gate-forms-redesign-j8v0zk`; commits per workstream.

## Files (primary)

- New: `UiComponents/Feedback/AsyncContent.razor` (+css), `UiComponents/Forms/ChipInput.razor` (+css),
  `Client/Pages/Permits/IssuePermit.razor`, `Client/Pages/Inductions/Inductions.razor` (list) +
  `InductionBuilder.razor`.
- Edit: `Client/Pages/SiteGate/SiteGate.razor` (+ new css), `Dashboard.razor`, `SiteDetail.razor`,
  `UserDetail.razor`, `OperativeDetail.razor`, `FormFill.razor`, `FormBuilderPage.razor`,
  `SelfOnboard.razor`, `QualificationCardsDialog.razor`, `Notifications.razor`,
  `Forms/FormBuilder.razor` (+css), `Forms/FormEditModel.cs`, `Permits/Permits.razor` (+css),
  `docs/component-catalogue.md`.

## Verification

- `dotnet build Tedwren.sln` — zero errors, investigate warnings (per CLAUDE.md).
- `dotnet test Tedwren.sln` — full suite green; add/adjust unit tests for the `FormEditModel` chip⇄JSON
  round-trip (incl. back-compat read of plain-string options) and induction quiz option mapping.
- `dotnet run --project src/Tedwren.Client` and manually walk each page:
  - No blank screen on first paint of Dashboard, Site Gate, the three detail pages, form/onboarding
    pages — a skeleton shows, then content.
  - Site Gate: operative grid spaced, override right-aligned, on-site-now is a sortable DataTable.
  - Forms editor: description below name & taller; create multiple named panels and add questions to
    each; options entered as chips with IDs; tighter question spacing.
  - Permits: `/permits` is list + "Issue permit" button → `/permits/new` screen; issuing returns to list.
  - Inductions: `/inductions` lists inductions with Add; edit/new open the builder by id; quiz options
    are chips.
- Confirm the Blazor binding rule (CLAUDE.md): all new/changed inputs use `@bind-Value` or
  `Value`+`ValueChanged`, never one-way `Value=` on mutable/derived values.
