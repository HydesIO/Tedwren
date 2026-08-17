# Tedwren Component Catalogue

A living inventory of the reusable components in `Tedwren.UiComponents`. Every component
is single-responsibility and parameterised: it takes typed parameters and raises
`EventCallback`s rather than reading global state, and all user-visible copy arrives
through parameters. Colour and spacing come only from `tokens.css` custom properties.

> **Status:** Complete — Phases 1–5. Every component in Plan & Scope §5 is documented
> below (shell & theme; dashboard cards, data display and charts; `DataTable` + list
> pages; the form components; and the feedback / loading / empty / error components).
> Phase 5 also covered the responsive, empty/loading/error and accessibility pass (§8).

---

## Theme

### `TedwrenTheme` (static)
The single `MudTheme` instance, generated from `tokens.css`. Applied once at
`MudThemeProvider` level.

```razor
<MudThemeProvider Theme="@TedwrenTheme.Instance" IsDarkMode="false" />
```

---

## Navigation & shell

### `AppSidebar`
Renders the brand mark, nav list, environment panel and collapse control.

| Parameter | Type | Notes |
|---|---|---|
| `IsCollapsed` | `bool` | Two-way bindable (`@bind-IsCollapsed`). |
| `BrandName` | `string` | Wordmark + logo `alt` / letter fallback. |
| `LogoUrl` | `string?` | Brand mark image (e.g. `images/logo-icon.svg`); falls back to the first letter of `BrandName`. Shown in both expanded and collapsed states. |
| `ActiveRoute` | `string?` | Current route; drives the active nav highlight. |
| `NavItems` | `IReadOnlyList<NavItem>` | Sidebar entries. |
| `Environment` | `AppSidebar.EnvironmentInfo?` | Environment panel data. |
| `OnNavigate` | `EventCallback<NavItem>` | Raised when a nav row is clicked. |

```razor
<AppSidebar NavItems="_navItems"
            ActiveRoute="@_activeRoute"
            @bind-IsCollapsed="_collapsed"
            Environment="_environment"
            OnNavigate="OnNavigate" />
```

### `SidebarNavItem`
One nav row with active / hover / expandable states.

| Parameter | Type | Notes |
|---|---|---|
| `Icon` | `string` | MudBlazor outline icon. Required. |
| `Label` | `string` | Row label. Required. |
| `IsActive` | `bool` | Active styling + `aria-current`. |
| `IsCollapsed` | `bool` | Icon-only when collapsed. |
| `HasChildren` | `bool` | Shows an expand chevron. |
| `OnClick` | `EventCallback` | Click / Enter / Space. |

### `EnvironmentPanel`
Status dot + version / build info block.

| Parameter | Type | Notes |
|---|---|---|
| `EnvironmentName` | `string` | Required. |
| `Version` | `string` | |
| `Build` | `string` | |
| `IsHealthy` | `bool` | Green (healthy) or amber (degraded) dot. |

### `AppTopBar`
Page title, global search, notifications, help, profile menu.

| Parameter | Type | Notes |
|---|---|---|
| `PageTitle` | `string` | |
| `Notifications` | `IReadOnlyList<NotificationEntry>` | Feeds the bell dropdown; unread count drives the badge. |
| `NotificationsHref` | `string` | "View all" target. |
| `CurrentUserName` | `string` | |
| `CurrentUserRole` | `string` | |
| `CurrentUserAvatarUrl` | `string?` | Falls back to initials. |
| `ShowMenuButton` | `bool` | Shows the sidebar toggle. |
| `IsDark` | `bool` | Drives the light/dark toggle icon. |
| `OnMenuClick` | `EventCallback` | |
| `OnOpenSearch` | `EventCallback` | Opens the command palette. |
| `OnToggleTheme` | `EventCallback` | Toggles light/dark. |
| `OnSignOut` | `EventCallback` | |

The search box is now a trigger that opens the `CommandPalette` (it no longer accepts
inline text); `GlobalSearchBox` remains in the library for standalone use.

### `Flyout`
Lightweight custom dropdown — a trigger, an absolutely-positioned panel and a click-away
backdrop — used instead of `MudMenu` where a slim bespoke popover is wanted.

| Parameter | Type | Notes |
|---|---|---|
| `Trigger` | `RenderFragment?` | The clickable trigger. |
| `ChildContent` | `RenderFragment?` | Panel contents. |
| `Align` | `Flyout.FlyoutAlign` | `Left` / `Right`. |
| `Width` | `string` | Panel width (default `280px`). |
| `CloseOnContentClick` | `bool` | Close when the panel is clicked (default true). |

### `NotificationsMenu`
Bell trigger + custom dropdown showing the most recent notifications with a "View all"
link. Built on `Flyout`.

| Parameter | Type | Notes |
|---|---|---|
| `Notifications` | `IReadOnlyList<NotificationEntry>` | |
| `MaxItems` | `int` | Most recent shown (default 10). |
| `ViewAllHref` | `string` | Notifications page link. |

### `CommandPalette`
Global search overlay opened with `Ctrl`/`⌘`+`K` (or the top-bar search). Fuzzy-ish
search across supplied `CommandItem`s, grouped results, full keyboard navigation
(↑/↓/Enter/Esc). The host builds items from pages + entities and handles selection.

| Parameter | Type | Notes |
|---|---|---|
| `Items` | `IReadOnlyList<CommandItem>` | `CommandItem(Label, Group, Icon, Href, Detail?)`. |
| `IsOpen` | `bool` | Two-way bindable. |
| `OnSelect` | `EventCallback<CommandItem>` | Raised on selection. |
| `MaxResults` | `int` | Cap on shown results (default 12). |

> Keyboard shortcut + theme persistence use `wwwroot/js/tedwren.js` (registered by
> `MainLayout` via JS interop).

### `GlobalSearchBox`
Search input with a keyboard-shortcut badge.

| Parameter | Type | Notes |
|---|---|---|
| `Placeholder` | `string` | |
| `ShortcutLabel` | `string?` | e.g. `⌘K`. |
| `OnSearch` | `EventCallback<string>` | Raised on Enter. |

### `ProfileMenu`
Avatar, name, role and a slim custom dropdown (built on `Flyout`, not `MudMenu`).

| Parameter | Type | Notes |
|---|---|---|
| `UserName` | `string` | Required; drives initials fallback. |
| `UserRole` | `string` | |
| `AvatarUrl` | `string?` | |
| `ProfileHref` / `SettingsHref` | `string` | Menu links. |
| `OnSignOut` | `EventCallback` | |

### `PageHeader`
Title + subtitle + right-aligned action slot. Every list/form page starts with one.

| Parameter | Type | Notes |
|---|---|---|
| `Title` | `string` | Required. |
| `Subtitle` | `string?` | |
| `Actions` | `RenderFragment?` | Right-aligned action buttons. |

### `DetailHeader`
Detail-page header: breadcrumb + title + optional status pill + avatar + actions.

| Parameter | Type | Notes |
|---|---|---|
| `Title` | `string` | Required. |
| `Subtitle` | `string?` | |
| `StatusLabel` | `string?` | Renders a `StatusPill` when set. |
| `Status` | `StatusKind` | |
| `Breadcrumbs` | `IReadOnlyList<DetailHeader.Crumb>?` | `Crumb(Label, Href?)`. |
| `AvatarText` / `AvatarUrl` | `string?` | Optional leading avatar. |
| `Actions` | `RenderFragment?` | Right-aligned actions. |

```razor
<PageHeader Title="Workforce" Subtitle="Operative register">
    <Actions>
        <MudButton Variant="Variant.Filled" Color="Color.Primary">Add operative</MudButton>
    </Actions>
</PageHeader>
```

---

## Cards & data display (Phase 2)

### `KpiCard`
Single metric tile: icon, value, trend badge and optional sparkline.

| Parameter | Type | Notes |
|---|---|---|
| `Title` | `string` | Required. |
| `Value` | `string` | Required; pre-formatted. |
| `Icon` | `string` | MudBlazor outline icon. |
| `TrendValue` | `string?` | e.g. `+4.2%`. |
| `TrendDirection` | `TrendDirection` | `Up` / `Down` drives colour + arrow; `None` hides the badge. |
| `Sparkline` | `IReadOnlyList<double>?` | Rendered via `TrendSparkline` when ≥ 2 points. |
| `AccentColour` | `string` | Icon tint + sparkline colour (token var). |

### `DashboardCard`
Generic bordered/shadowed card shell with a title row, body and optional footer link.

| Parameter | Type | Notes |
|---|---|---|
| `Title` / `Subtitle` | `string?` | Header text. |
| `Body` | `RenderFragment?` | Card content. |
| `HeaderActions` | `RenderFragment?` | Right-aligned header slot. |
| `FooterLinkText` / `FooterLinkHref` | `string?` | Optional footer link. |
| `Class` | `string?` | Extra CSS class(es) appended to the card's root element. |

### `DonutStat`
SVG donut + centred value/label. Used by the compliance overview.

| Parameter | Type | Notes |
|---|---|---|
| `Segments` | `IReadOnlyList<DonutSegment>` | Normalised across segments. |
| `CentreValue` | `string` | Required (e.g. `92%`). |
| `CentreLabel` | `string?` | |
| `Size` / `Thickness` | `int` | Geometry. |

### `LegendList`
Coloured-dot legend rows with value and optional percentage columns.

| Parameter | Type | Notes |
|---|---|---|
| `Items` | `IEnumerable<LegendItem>` | |

### `ExpiryList`
Repeated "item / site / expiry date / days remaining" rows, each with a `StatusPill`.

| Parameter | Type | Notes |
|---|---|---|
| `Items` | `IEnumerable<ExpiryItem>` | Days-remaining label derived automatically. |

### `ActivityFeed`
Icon + primary/secondary text + relative-time rows, with an accent per row.

| Parameter | Type | Notes |
|---|---|---|
| `Items` | `IEnumerable<ActivityItem>` | |

### `TrendSparkline`
Small inline SVG line + area chart. No dependencies.

| Parameter | Type | Notes |
|---|---|---|
| `Values` | `IReadOnlyList<double>` | ≥ 2 points to render. |
| `Colour` | `string` | Stroke + gradient fill. |
| `Width` / `Height` | `int` | viewBox geometry (scales to container). |

### `StatusPill`
Rounded coloured pill (Healthy / Warning / Compliant / Expired / …).

| Parameter | Type | Notes |
|---|---|---|
| `Label` | `string` | Required. |
| `Status` | `StatusKind` | `Neutral` / `Success` / `Warning` / `Danger` / `Info` / `Permit`. |

### `RiskChip`
Small numeric severity chip for the heatmap "At risk" column.

| Parameter | Type | Notes |
|---|---|---|
| `Value` | `int` | |
| `Severity` | `RiskSeverity` | `Low` / `Medium` / `High` drives colour. |

---

## Tables & feedback (Phase 3)

### `DataTable<TItem>`
Generic sortable / filterable table wrapping `MudTable`, with client-side free-text
search, per-column distinct-value dropdown filters, sorting, paging, row-click and a
built-in empty state. Columns are declared with `DataColumn<TItem>`.

| Parameter | Type | Notes |
|---|---|---|
| `Items` | `IReadOnlyList<TItem>` | Required. |
| `Columns` | `IReadOnlyList<DataColumn<TItem>>` | Required. |
| `Searchable` | `bool` | Free-text search box (default true). |
| `SearchPlaceholder` | `string` | |
| `ShowPager` | `bool` | Default true. |
| `PageSizeOptions` | `int[]` | Default `10, 25, 50`. |
| `OnRowClick` | `EventCallback<TItem>` | Rows are styled clickable only when set. |
| `Actions` | `RenderFragment?` | Right-aligned toolbar slot. |
| `Loading` | `bool` | Renders the table `LoadingSkeleton`. |
| `ErrorMessage` | `string?` | Renders a `BannerAlert` (+ Retry via `OnRetry`). |
| `EmptyIcon` / `EmptyTitle` / `EmptyDescription` | `string` | Empty-state copy. |

A summary row shows the filtered result count (`N of M`) and removable chips for the
active search term and each column filter.

```razor
<DataTable TItem="Company" Items="_companies" Columns="_columns"
           SearchPlaceholder="Search companies…"
           EmptyTitle="No companies yet" />
```

### `DataColumn<TItem>`
Declarative column definition.

| Member | Type | Notes |
|---|---|---|
| `Title` | `string` | Header text (required). |
| `Value` | `Func<TItem, object?>?` | Sort key + default cell + search/filter text. |
| `Text` | `Func<TItem, string>?` | Explicit string projection for search / filter / cell. |
| `CellTemplate` | `RenderFragment<TItem>?` | Custom cell (takes precedence). |
| `Sortable` | `bool` | Default true. |
| `Searchable` | `bool` | Default true. |
| `Filterable` | `bool` | Adds a distinct-value dropdown filter. |
| `AlignRight` | `bool` | Right-align (numeric columns). |

### `EmptyState`
Icon + message + optional action, for lists / tables with no data.

| Parameter | Type | Notes |
|---|---|---|
| `Icon` | `string` | |
| `Title` | `string` | Required. |
| `Description` | `string?` | |
| `Action` | `RenderFragment?` | |

### `KeyValueList`
Label / value pairs for detail-page overview sections (responsive definition grid).

| Parameter | Type | Notes |
|---|---|---|
| `Items` | `IEnumerable<KeyValueList.Pair>` | `Pair(Label, Value?, ValueContent?)`. |
| `Columns` | `int` | Grid columns (default 2). |

---

## Forms (Phase 4)

All inputs are thin, styled wrappers around MudBlazor's own form components, sharing the
`FormField` chrome (label above the control, optional helper text, and a reserved-space
validation message that never shifts layout). Binary inputs default to `TedwrenToggle`
(a switch), **never a checkbox** — see the switches-over-checkboxes rule in `README.md`.

### `FormField`
Shared field chrome used by every wrapper. `Label`, `For`, `Required`, `HelperText`,
`Error`, `ErrorText`, `ChildContent`.

### `TedwrenTextField`
Wrapped `MudTextField`. `Label`, `@bind-Value`, `Placeholder`, `HelperText`, `Required`,
`Error`, `ErrorText`, `Lines` (multiline), `InputType`, `Disabled`, `ReadOnly`,
`AdornmentIcon`.

### `TedwrenSelect<T>`
Wrapped `MudSelect`. `Label`, `@bind-Value`, `Options`, `OptionText`, `Placeholder`,
`Required`, `Error`, `ErrorText`, `Clearable`.

### `TedwrenAutocomplete<T>`
Searchable single-select over `MudAutocomplete`. `Label`, `@bind-Value`, `Options`,
`OptionText`, `Placeholder`, `Required`, `Error`, `ErrorText`.

### `TedwrenToggle`
The default binary/boolean control — a labelled switch wrapping `MudSwitch`.
`Label`, `Description`, `@bind-Value`, `Disabled`.

### `TedwrenDateRangePicker`
The "28 Jul – 3 Aug" style range control. `Label`, `@bind-DateRange`, `Placeholder`,
`HelperText`, `Required`, `Error`, `ErrorText`.

### `TedwrenFileUpload`
Card/document upload with drag-and-drop and a selected-file preview (UI-only — files are
listed, not persisted). `Label`, `PromptText`, `HintText`, `Accept`, `MaxFiles`,
`FilesChanged`.

### `TedwrenStepper`
Wrapped `MudStepper` for multi-step flows (onboarding, induction builder). `@bind-ActiveIndex`,
`Linear`, `ChildContent` (consumer supplies `MudStep` children).

### `FormSection`
Titled, bordered grouping wrapper — forms are built from these, not one long list.
`Title`, `Description`, `ChildContent` (two-column grid; children spanning both columns
use a wrapper with `grid-column: 1 / -1`).

### `FormActions`
Consistent action placement: destructive bottom-left, secondary + primary bottom-right.
`Primary`, `Secondary`, `Destructive` render fragments.

### `InlineValidationMessage`
Consistent field-level error with reserved space. `Message`.

### `BannerAlert`
Page-level informational / warning banner. `Message`, `Title`, `Severity` (`StatusKind`),
`Action`.

---

## Feedback & state (Phase 5)

### `EmptyState`
See Phase 3 above — icon + message + optional action for the no-data / no-results case.

### `LoadingSkeleton`
Skeleton placeholders (the default loading pattern — not spinners). Respects
`prefers-reduced-motion`.

| Parameter | Type | Notes |
|---|---|---|
| `Variant` | `LoadingSkeleton.SkeletonVariant` | `Card` / `Table` / `List` / `Kpi`. |
| `Rows` | `int` | Placeholder row count. |

`DataTable<TItem>` renders the `Table` variant automatically when its `Loading`
parameter is set, and a `BannerAlert` with a Retry button when `ErrorMessage` is set.

### `AsyncContent`
Loading / error / empty / content switch for any async region — so a page never paints a
blank screen while an API call is in flight. Generalises the pattern proven in
`DataTable`, reusing `LoadingSkeleton`, `BannerAlert` and `EmptyState`. Wrap the region
that depends on loaded data; drive `Loading` from the page's load flag.

| Parameter | Type | Notes |
|---|---|---|
| `Loading` | `bool` | Shows the skeleton while true. |
| `Variant` | `LoadingSkeleton.SkeletonVariant` | Skeleton shape (`Card` default / `Table` / `List` / `Kpi`). |
| `Rows` | `int` | Skeleton placeholder count. |
| `Error` | `string?` | When set (and not loading), shows an error banner instead of the content. |
| `ErrorTitle` | `string` | Error banner title. |
| `OnRetry` | `EventCallback` | Adds a Retry button to the error banner when provided. |
| `IsEmpty` | `bool` | When true (and not loading/errored), shows the empty state. |
| `EmptyContent` | `RenderFragment?` | Custom empty markup; falls back to a default `EmptyState`. |
| `EmptyIcon` / `EmptyTitle` / `EmptyDescription` | — | Default empty-state copy. |
| `ChildContent` | `RenderFragment?` | The loaded content. |

### `ConfirmDialog`
Wrapped `MudDialog` for destructive / irreversible actions. Shown via `IDialogService`
with `DialogParameters` (`ContentText`, `ConfirmText`, `CancelText`, `Destructive`) and
`TedwrenDialog.Small()` options; returns `DialogResult.Ok(true)` on confirm. See the usage
snippet in the component source.

---

## Dialogs

The shared dialog standard (see CLAUDE.md → *MudDialog design standard*). Every MudBlazor dialog
in the Client uses a `TedwrenDialog` size preset; interactive dialogs add a `DialogGuidance` panel,
group fields with the `.tw-dialog-body*` / `.tw-dialog-section*` helpers in `wwwroot/css/dialogs.css`,
and take theme-adaptive colour from `--mud-palette-*` (the dialog overlay renders outside the
`.theme-dark` shell, so the `--color-*` tokens would resolve to their light values there).

### `TedwrenDialog` (static)
`Tedwren.UiComponents.Dialogs.TedwrenDialog` — standard `DialogOptions` presets so a dialog's width
is chosen deliberately, not dictated by its content. `Small()` (confirmations / warnings / short
messages), `Medium()` (normal forms / editing), `Large()` (complex or multi-section forms, detailed
viewers, workflows). All are `FullWidth` with a header close button and a non-dismissing backdrop.
Pass at the call site: `DialogService.ShowAsync<T>(title, parameters, TedwrenDialog.Medium())`.

### `DialogGuidance`
Contextual guidance panel for the top of an interactive dialog — a short, persistent explanation of
what the user should do (not a tooltip). Subtle rounded amber (`Severity=StatusKind.Warning`, default)
or blue (`StatusKind.Info`) panel, readable in light and dark. `Text` or `ChildContent`, optional
`Title`. Omit where the purpose is already obvious (e.g. a plain `ConfirmDialog`).

### `ProgressDialog`
Standard progress / loading dialog (shown at `TedwrenDialog.Medium()`): a titled, spacious panel —
never a bare spinner. `Description`, `StatusText` (e.g. "Processing 24 of 87…"), `Value`/`Max`
(a determinate linear bar when `Max > 0`, otherwise indeterminate), and `OnCancel` (a Cancel button
appears only when set — offer cancellation only where the operation supports it).

---

## Forms Library field wrappers (Phase 21)

Added for the customer-built Forms Library (PRD-Phase 2). Each builds on `FormField`
chrome, reads colour/spacing from `tokens.css`, and follows the live-binding rule
(`@bind-Value` / `Value`+`ValueChanged`). They fill the input kinds the Phase 4 suite
lacked, so pages stop reaching for raw MudBlazor.

### `TedwrenNumericField`
Wrapped `MudNumericField<decimal?>` — the default numeric input. `Label`, `@bind-Value`,
`Min`, `Max`, `HelperText`, `Required`, `Error`/`ErrorText`, `HideSpinButtons`.

### `TedwrenDatePicker`
Wrapped `MudDatePicker` — a single editable date (`dd MMM yyyy`). `Label`, `@bind-Date`,
`Placeholder`, `Required`, `Error`/`ErrorText`.

### `TedwrenRadioGroup`
Wrapped `MudRadioGroup<string>` — one choice from an always-visible option list.
`Label`, `@bind-Value`, `Options`, `Required`.

### `TedwrenRagInput`
A red/amber/green status selector for inspection items — three mutually-exclusive buttons
coloured from the status tokens; value is `"Red"` / `"Amber"` / `"Green"`. `Label`,
`@bind-Value`, `Required`. `role="radiogroup"` with `aria-checked` per option.

### `TedwrenSignaturePad`
A canvas signature pad; strokes are captured as a PNG data URL surfaced through `Value`
(pushed from JS on stroke-end via a `[JSInvokable]` callback), with a "Clear" action.
Interop lives in `wwwroot/js/tedwren.js` (`tedwren.signature.*`). `Label`, `@bind-Value`,
`Required`. `IAsyncDisposable` — disposes the canvas handlers and the DotNet reference.

### `ChipInput`
Editable list of option chips — replaces the old "comma separated" option entry for choice
fields (Forms) and quiz answers (Inductions). Each chip shows the option text, its assigned
id badge and a remove control; the text box appends a new chip on Enter or Add (duplicates
are ignored). Binds two-way over a mutable `List<ChipOption>` (`Id`, `Text`) edited in place.

| Parameter | Type | Notes |
|---|---|---|
| `Value` | `List<ChipOption>` | The option list, mutated in place; pair with `ValueChanged`. |
| `ValueChanged` | `EventCallback<List<ChipOption>>` | Raised on add / remove. |
| `Label` / `Placeholder` / `HelperText` | `string?` | Field chrome. |
| `Error` / `ErrorText` | — | Validation state. |

`ChipOption` (in `Tedwren.UiComponents.Forms`) carries a stable `Id` assigned on creation
(`ChipOption.Create(text)`) and the display `Text`.

## Forms Library components (client, Phase 21–22)

Client-side components (in `Tedwren.Client/Pages/Forms`) that compose the field wrappers
into the builder and fill experiences.

### `FormBuilder`
The authoring surface: questions are grouped into named **panels**, each a collapsible card
with an editable name, its own question list, an "Add question to this panel" action and
per-question reorder controls; a top-level action adds a new panel. Choose each question's
answer type, toggle `Required`, and enter choice-field options as `ChipInput` chips. Edits a
`List<FormEditModel.SectionEdit>` in place (`Sections` parameter). `FormEditModel` maps
to/from the `FormSectionDto` contracts and persists option chips into `OptionsJson` as
`{id,text}` objects (reading legacy plain-string arrays back for compatibility).

### `DynamicFormRenderer`
Renders a published form's sections/fields as the matching `Tedwren*` input per
`FormFieldKind`, and exposes `GetAnswers()` / `GetFiles()` (base64) for submission. Files
are read from `IBrowserFile`; multi-select renders one `TedwrenToggle` per option.

### `FormSubmissionDialog`
Views a completed submission — answers labelled from the template version it was completed
against, captured-file downloads, and the review actions (approve / reject with a reason).

---

## Models

### `NavItem`
`record NavItem(string Label, string Icon, string Href, IReadOnlyList<NavItem>? Children = null)`
— a single sidebar entry, supplied by the host app so the library never hard-codes the
route list.

### Display models & enums (`Tedwren.UiComponents.Models`)
- `TrendDirection` — `None` / `Up` / `Down`.
- `StatusKind` — `Neutral` / `Success` / `Warning` / `Danger` / `Info` / `Permit`.
- `RiskSeverity` — `Low` / `Medium` / `High`.
- `LegendItem(string Label, double Value, string Colour, double? Percentage = null)`.
- `DonutSegment(string Label, double Value, string Colour)`.
- `ExpiryItem(string Title, string Site, DateOnly ExpiresOn, int DaysRemaining, StatusKind Status)`.
- `ActivityItem(string Icon, string Primary, string Secondary, string RelativeTime, StatusKind Accent)`.
