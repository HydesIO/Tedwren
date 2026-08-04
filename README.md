# Tedwren — UI/UX Base Project

A Blazor WebAssembly solution that is the **visual and structural foundation** for
both Tedwren products (subcontractor and main contractor). It is a deliberate
**UI/UX exercise**: screens are built with representative sample data, forms validate
and toggle visually, and nothing persists. Business logic, the data model, API
contracts and backend services are explicitly out of scope and follow later.

See `docs/` and the Plan & Scope of Works v1.0 for the full brief.

## Status

**Phases 1–4 are complete.**

Phase 1 delivered the solution scaffold, design tokens, MudBlazor theme, and the
application shell (`MainLayout`, `AppSidebar`, `AppTopBar`). Phase 2 added the §5.2
card, data-display and chart components and rebuilt the Dashboard route from them.
Phase 3 added the generic `DataTable<TItem>` and `EmptyState` and the list pages
(Organisation, Workforce, Sites, Compliance, Audit Log). Phase 4 added the form
components — `FormField`, `TedwrenTextField`, `TedwrenSelect`, `TedwrenAutocomplete`,
`TedwrenToggle`, `TedwrenDateRangePicker`, `TedwrenFileUpload`, `TedwrenStepper`,
`FormSection`, `FormActions`, `InlineValidationMessage`, `BannerAlert` — and the forms:
add-company, invite-user (permission toggles), add-operative stepper (direct entry vs
send-a-link), permit issuance, the induction builder stepper, and system configuration
(module/entitlement and enforcement toggles at scale). Every form validates required
fields, uses switches per §6.2, and follows the action-placement convention. The
responsive/accessibility polish pass is Phase 5.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MudBlazor is restored automatically from NuGet on first build.

## Running the solution

```bash
dotnet restore
dotnet run --project src/Tedwren.Client
```

Then open the URL printed in the console (typically `https://localhost:5001`).

## Solution structure

```
Tedwren.sln
├── src/
│   ├── Tedwren.Client                  Blazor WASM app — startup, routing, layout, pages
│   ├── Tedwren.UiComponents            Reusable component kit + MudBlazor theme
│   └── Tedwren.UiComponents.SampleData In-memory sample data + interfaces (the only
│                                       project replaced when real APIs arrive)
└── docs/
    └── component-catalogue.md          Living component inventory
```

The two-project split (`Client` vs `UiComponents`) is deliberate: it forces every
visual element to be a genuinely reusable component rather than page-local markup, and
gives the API-integration phase a clean seam. **`Tedwren.UiComponents` must never
reference anything HTTP- or auth-related.**

## Design tokens & theme

`src/Tedwren.Client/wwwroot/css/tokens.css` is the **literal source of truth** for
colour, radius, shadow, spacing and typography (from the approved dashboard).
`src/Tedwren.UiComponents/Theme/TedwrenTheme.cs` is a single `MudTheme` generated from
those same values and applied once at `MudThemeProvider` level — never overridden per
page. Do not introduce colour or spacing literals outside `tokens.css`.

## Adding a new page using the shell

1. Create a folder under `src/Tedwren.Client/Pages/` matching the functional area.
2. Add a `.razor` component with an `@page "/route"` directive. It automatically renders
   inside `MainLayout` (sidebar + top bar + theme).
3. Start the page body with a `<PageHeader Title="…" Subtitle="…" />`.
4. Add the route to `ShellSampleDataService.Nav` (in `Tedwren.UiComponents.SampleData`)
   so it appears in the sidebar and the top-bar title resolves correctly.

## Adding a new reusable component

1. Add the component to the appropriate folder in `Tedwren.UiComponents`
   (`Navigation/`, `Cards/`, `DataDisplay/`, `Forms/`, `Feedback/`, `Charts/`).
2. Make it single-responsibility and **parameterised** — take typed parameters and
   raise `EventCallback`s; never read global state or hard-code copy. All text comes
   through parameters.
3. Use component-scoped CSS isolation (`Component.razor.css`) and reference **only**
   the `--token` custom properties from `tokens.css` for colour/spacing — no literals.
4. Add an entry to `docs/component-catalogue.md` (props table + usage snippet).

## Forms convention — switches over checkboxes

Binary / boolean inputs default to a **toggle switch** (`TedwrenToggle`, wrapping
`MudSwitch`), not a checkbox — for yes/no settings, feature on/off states, consent-style
single questions, and list-row included/excluded states.

`MudCheckBox` is reserved **only** for genuine multi-select-from-a-list situations
(e.g. selecting several operatives or document types), where a list of independent
selections is being made — that is a selection list, not a toggle, and should look like
one (checkbox list or chip multi-select), not a stack of switches.

## Out of scope for this project

Authentication, authorisation, any API / database / persistence, real-time data,
PRD business rules, and native mobile apps. Interfaces are written so these can be added
later without reshaping the UI (data arrives through injected services, not hard-coded
in components).
