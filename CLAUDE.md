# CLAUDE.md — Tedwren development guide

## Authoritative source of truth

**`docs/TedwrenPRDv6_4.docx` (Product Requirements Document v6.4) is the definitive source of
truth for all development in this repository.** It supersedes PRD v6.3, PRD v5.0 and Product
Specification v4.0. Where anything in code, comments, this file, or `TODO.md` disagrees with the
PRD, **the PRD wins** — and the discrepancy should be raised, not silently worked around.
`docs/TedwrenPRDv6_4.md` is a plain-text mirror of the same document, kept for in-repo diffing and
search; the `.docx` is still the file of record; if it is ever revised, re-sync the `.md` mirror
in the same change.

- Section 5 is requirements (SF / SUB / MC identifiers). Section 7 is rules (R1–R18) that must
  hold — each is a commercial, legal or safety constraint. Section 8 is the later commercial
  modules (referred to in planning as "PRD-Phase 1–7"). Section 10.1 lists decisions that are
  genuinely open.
- Reference PRD requirement/rule IDs (e.g. SF-1, SUB-8, MC-8, R10) in code comments, commits and
  `TODO.md` rather than reproducing PRD prose.
- Do **not** invent requirements or assume behaviour the PRD or existing solution does not
  support. If something is unspecified, treat it as an open question, not licence to guess.

## Planning & tracking

- **`docs/plan-and-scope.md`** — the phased development plan & scope of works. The delivery
  sequence follows PRD §11 (shared foundation → subcontractor MVP → main contractor MVP →
  hardening → PRD-Phases 1–7). Phase numbering continues the existing UI sequence: Phases 1–6
  (UI/UX over mock data) are complete; backend work starts at **Phase 7**.
- **`TODO.md`** — the living development checklist (planned / in-progress / completed / deferred /
  outstanding). Update it whenever work is started, completed, deferred or newly identified.
  Completed items carry a concise description of what changed and the relevant phase/area.
  `TODO.md` is the working checklist; the PRD remains the source of truth.

## Architecture

Two deployables that talk over HTTP/CORS, plus supporting libraries:

| Project | Responsibility |
|---|---|
| `src/Tedwren.Client` | Blazor WebAssembly app — UI only. Consumes service **interfaces**, never data-access directly. |
| `src/Tedwren.UiComponents` | Reusable MudBlazor component kit + theme. No HTTP/auth/data concerns. |
| `src/Tedwren.Abstractions` | Shared service interfaces + DTOs + config contracts, referenced by both client and API. |
| `src/Tedwren.Domain` | Entities, value objects, enums. No external dependencies. |
| `src/Tedwren.Application` | Business services (each behind an interface, SRP). |
| `src/Tedwren.DataAccess` | Dapper repositories: shared base + SQL Server / PostgreSQL dialects. |
| `src/Tedwren.Api` | ASP.NET Core Web API (separate deployable, CORS, mobile-ready). |
| `tests/*` | xUnit unit + integration tests. |

### The data source (database only; in-memory is a test double)

**The product runs against the database — there is no runtime mock mode.** The client always calls the Web
API; the API always uses the Dapper repositories against SQL Server / PostgreSQL. The in-memory
implementations under `src/Tedwren.Application/Persistence/InMemory` survive **only as test doubles** (fast,
isolated unit/API tests) and are never a supported runtime configuration.

- **API** (`src/Tedwren.Api/appsettings.json`): `DataSource:Mode` defaults to `Database`;
  `DataSource:Provider` = `SqlServer` | `PostgreSql` (bound to `BackendOptions`). Set
  `ConnectionStrings:SqlServer`. Get the schema up to date with EF migrations — see `docs/ef-migrations.md`.
  The only other `Mode` value is `InMemory`, which is **test-only** — selected by the API test host, not for
  deployment.
- **Client** (`src/Tedwren.Client/wwwroot/appsettings.json`): calls the Web API at `Api:BaseUrl` (no data-source
  switch). The API's `Cors:AllowedOrigins` must include the client's served origin.
- **Tests** force `DataSource:Mode=InMemory` (via a module initializer in `Tedwren.Api.Tests`) so the suite
  runs without a database. This is the only sanctioned use of the in-memory path.

Note: every console page now renders live API data — the former `Tedwren.UiComponents.SampleData` project has
been removed. Static shell chrome (nav/route inventory, platform switcher, environment badge) lives in
`src/Tedwren.Client/Services/ShellChrome.cs` as fixed app configuration, not a data source.

## Engineering standards (apply to every change)

- **.NET 10.** Use `async` wherever practical.
- **Summary comment on every class and every method.** Follow the **Single Responsibility
  Principle** — no god classes, no unrelated responsibilities bundled together.
- **Dapper** for data access across both engines; share base classes/abstractions between SQL
  Server and PostgreSQL to avoid duplicated logic. **JSON** (System.Text.Json) for settings,
  integrations, schemas and other extensible config (`nvarchar(max)` / `jsonb`).
- **Reuse first.** Extend the existing parameterised MudBlazor components (`DataTable<TItem>`,
  the Forms and Feedback suites, cards, charts, detail components) rather than adding new
  patterns. New components follow the established naming, scoped-CSS and `tokens.css`
  conventions and are catalogued in `docs/component-catalogue.md`. `tokens.css` is the only
  source of colour/spacing — no literals elsewhere.
- **Tests** must not modify existing/production records — use isolated, transactional,
  purpose-created or mocked data. Integration tests run against SQL Server LocalDB with a
  transaction rolled back per test; a dedicated PostgreSQL suite is the pre-launch parity gate.
- **Build and test the whole solution before opening a PR.** Resolve all compile errors.
  Investigate warnings rather than suppressing them; suppression must be justified and documented
  and must not hide a real issue.
- Each phase is **independently testable** and must **not break existing functionality** or a
  previously completed phase. Deliver a usable increment where possible.

## Build & run

```bash
dotnet build Tedwren.sln          # whole solution
dotnet test  Tedwren.sln          # all test projects
dotnet run --project src/Tedwren.Api      # Web API (health at /health)
dotnet run --project src/Tedwren.Client   # Blazor WASM client
```

> Toolchain note: the .NET 10 SDK installs from `packages.microsoft.com`
> (`apt-get install -y dotnet-sdk-10.0`) in this environment; `dot.net` is egress-blocked.
