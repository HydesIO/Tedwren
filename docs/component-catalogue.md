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
Renders the brand mark, platform selector, nav list, environment panel and collapse
control.

| Parameter | Type | Notes |
|---|---|---|
| `IsCollapsed` | `bool` | Two-way bindable (`@bind-IsCollapsed`). |
| `BrandName` | `string` | Wordmark + logo `alt` / letter fallback. |
| `LogoUrl` | `string?` | Brand mark image (e.g. `images/logo-icon.svg`); falls back to the first letter of `BrandName`. Shown in both expanded and collapsed states. |
| `ActiveRoute` | `string?` | Current route; drives the active nav highlight. |
| `NavItems` | `IReadOnlyList<NavItem>` | Sidebar entries. |
| `Platforms` | `IReadOnlyList<string>` | Platform selector options. |
| `SelectedPlatform` | `string` | Currently selected platform label. |
| `OnPlatformSelected` | `EventCallback<string>` | Raised on platform change. |
| `Environment` | `AppSidebar.EnvironmentInfo?` | Environment panel data. |
| `OnNavigate` | `EventCallback<NavItem>` | Raised when a nav row is clicked. |

```razor
<AppSidebar NavItems="_navItems"
            ActiveRoute="@_activeRoute"
            @bind-IsCollapsed="_collapsed"
            Platforms="_platforms"
            SelectedPlatform="@_selected"
            OnPlatformSelected="OnPlatformSelected"
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

### `PlatformSelector`
Bordered dropdown for the active platform / tenant.

| Parameter | Type | Notes |
|---|---|---|
| `SelectedLabel` | `string` | Required. |
| `Options` | `IReadOnlyList<string>` | |
| `OnSelected` | `EventCallback<string>` | |

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
| `OnMenuClick` | `EventCallback` | |
| `OnSearch` | `EventCallback<string>` | |
| `OnSignOut` | `EventCallback` | |

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
| `EmptyIcon` / `EmptyTitle` / `EmptyDescription` | `string` | Empty-state copy. |

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

### `ConfirmDialog`
Wrapped `MudDialog` for destructive / irreversible actions. Shown via `IDialogService`
with `DialogParameters` (`ContentText`, `ConfirmText`, `CancelText`, `Destructive`);
returns `DialogResult.Ok(true)` on confirm. See the usage snippet in the component source.

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
