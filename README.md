# Tedwren

A .NET 10 **UK construction workforce-compliance platform**, delivered as two independently saleable
products — a subcontractor *time-and-attendance* product and a main contractor *workforce-management*
product — sharing one data foundation, plus a platform-operator admin area and a public marketing site.

The definitive brief is **`docs/TedwrenPRDv6_4.docx` (PRD v6.4)** — the source of truth. See **`CLAUDE.md`**
for architecture and engineering standards, **`docs/plan-and-scope.md`** for the phased delivery plan,
**`docs/next-phases-plan.md`** for the next stage of work, and **`TODO.md`** for the live checklist.

## Status

The platform is built out well beyond the original UI phases: the shared foundation (people/companies,
qualification cards + competency, expiry engine, sites + geofences, sign-in/attendance, roles/audit/module
entitlements), the **subcontractor MVP** (timesheets, compliance packs) and the **main contractor MVP**
(digital induction, the five-check site-entry decision, muster) are all delivered over a real Web API and
Dapper data access, with JWT auth, Resend email, a Forms Library (the configurable checklist/inspection
engine), a GoCardless-backed admin/billing plane, and a marketing site (`Tedwren.Web`). It runs against a
database — there is no runtime mock mode; the in-memory repositories survive only as a test double.

The current stage of work is **Launch Readiness** (production hardening) followed by the next commercial
modules — see `docs/next-phases-plan.md`.

## Architecture

Two deployables that talk over HTTP/CORS, plus supporting libraries (full table in `CLAUDE.md`):

| Project | Responsibility |
|---|---|
| `src/Tedwren.Client` | Blazor WebAssembly app — UI only; consumes service **interfaces**, never data access directly. |
| `src/Tedwren.UiComponents` | Reusable MudBlazor component kit + theme (no HTTP/auth/data concerns). |
| `src/Tedwren.Abstractions` | Shared service interfaces, DTOs and config contracts (referenced by client + API). |
| `src/Tedwren.Domain` | Entities, value objects, enums (no external dependencies). |
| `src/Tedwren.Application` | Business services (each behind an interface, SRP). |
| `src/Tedwren.DataAccess` | Dapper repositories (product/compliance DB) + product EF `DbContext`/migrations. |
| `src/Tedwren.DataAccess.Commercial` | Commercial/admin DB plane (billing + go-to-market). |
| `src/Tedwren.Api` | ASP.NET Core Web API (separate deployable, CORS, secure-by-default). |
| `src/Tedwren.Web` | Public marketing/SEO website (MVC). |
| `tests/*` | xUnit unit + integration tests. |

## Build & run

```bash
dotnet build Tedwren.sln          # whole solution
dotnet test  Tedwren.sln          # all test projects
dotnet run --project src/Tedwren.Api      # Web API (health at /health)
dotnet run --project src/Tedwren.Client   # Blazor WASM client
```

The API runs against the database by default (`DataSource:Mode=Database`, `Provider=SqlServer`). No database
credential is committed — supply `ConnectionStrings:SqlServer` (and the required secrets) from the environment;
see **`docs/operations.md`** for the full list and the production startup checks. For a database-free local run,
set `DataSource__Mode=InMemory` (the test-only in-memory repositories). `dotnet test` uses that path
automatically. The .NET 10 SDK installs from `packages.microsoft.com` in the dev container.

## Engineering standards

Apply the standards in **`CLAUDE.md`** to every change: .NET 10 + async, a summary comment on every class and
method, Single Responsibility, Dapper across both engines behind shared abstractions, JSON for extensible
config, **reuse the existing MudBlazor component kit** (`tokens.css` is the only source of colour/spacing), the
MudDialog design standard, secure-by-default endpoints (an authorization `FallbackPolicy` requires
authentication unless a route opts out with `.AllowAnonymous()`), and building **and** testing the whole
solution before opening a PR. Reference PRD requirement/rule IDs (SF-/SUB-/MC-/R-) in code, commits and
`TODO.md` rather than reproducing PRD prose.
