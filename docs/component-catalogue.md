# Tedwren Component Catalogue

A living inventory of the reusable components in `Tedwren.UiComponents`. Every component
is single-responsibility and parameterised: it takes typed parameters and raises
`EventCallback`s rather than reading global state, and all user-visible copy arrives
through parameters. Colour and spacing come only from `tokens.css` custom properties.

> **Status:** Phase 1 (Shell & theme) and Phase 2 (Dashboard — cards, data display and
> charts) components are documented below. Forms and feedback components are added to
> this catalogue as their phases land (Plan & Scope §5, §10).

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
| `NotificationCount` | `int` | Badge; hidden when 0. |
| `CurrentUserName` | `string` | |
| `CurrentUserRole` | `string` | |
| `CurrentUserAvatarUrl` | `string?` | Falls back to initials. |
| `ShowMenuButton` | `bool` | Shows the sidebar toggle. |
| `OnMenuClick` | `EventCallback` | |
| `OnSearch` | `EventCallback<string>` | |

### `GlobalSearchBox`
Search input with a keyboard-shortcut badge.

| Parameter | Type | Notes |
|---|---|---|
| `Placeholder` | `string` | |
| `ShortcutLabel` | `string?` | e.g. `⌘K`. |
| `OnSearch` | `EventCallback<string>` | Raised on Enter. |

### `ProfileMenu`
Avatar, name, role and dropdown.

| Parameter | Type | Notes |
|---|---|---|
| `UserName` | `string` | Required; drives initials fallback. |
| `UserRole` | `string` | |
| `AvatarUrl` | `string?` | |

### `PageHeader`
Title + subtitle + right-aligned action slot. Every page starts with one.

| Parameter | Type | Notes |
|---|---|---|
| `Title` | `string` | Required. |
| `Subtitle` | `string?` | |
| `Actions` | `RenderFragment?` | Right-aligned action buttons. |

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
