# Demo Data Service

A one-click way for a Tedwren platform admin to populate (or wipe) a complete, realistic demonstration
dataset — for sales demos, screenshots and exercising the reporting surfaces. It lives in the **Product Admin
portal** at **`/admin/demo-data`** and is restricted to platform admins (the `PlatformAdmin` policy).

## What it creates

Two fixed demo tenants, both with the administrator password `Demo123!`:

| Company | Role | Admin sign-in | Sites | Workforce |
|---|---|---|---|---|
| **Demo Contractors Ltd** | Main contractor | `contractor@tedwren.com` | 10 gated sites (geofenced, with a compound) | 25 uniquely-named operatives |
| **Demo Sub Contractors Ltd** | Subcontractor | `subcontractor@tedwren.com` | 6 of its **own distinct** sites, including **dispersed retrofit sites with no gate** | 5 contractors |

Each company also gets a compliance manager and (for the main contractor) a site manager console user.

## Published sign-in accounts (auto-seeded at startup)

So the console and the browser emulator can always be signed into without first running "Create demo data", the
published demo logins are seeded (and repaired) on **every startup** by `DemoLoginSeeder`. This is **gated on
`Demo:Enabled`** — off by default and refused in Production by `StartupSecurity` — so these fixed credentials
never reach a real deployment. It is idempotent and self-healing: each account is created when missing, and an
existing one has its password and active status repaired so the documented credential always works.

| Login | Account | Password |
|---|---|---|
| Main contractor | `contractor@tedwren.com` (Administrator of Demo Contractors Ltd) | `Demo123!` |
| Subcontractor | `subcontractor@tedwren.com` (Administrator of Demo Sub Contractors Ltd) | `Demo123!` |
| Operative | `operative@tedwren.com` — via the emulator's Development-only operative demo sign-in | `Demo123!` |
| Platform admins | `leigh.hydes@tedwren.com`, `james.darby@tedwren.com`, `james.wheeler@tedwren.com` | `Admin123!` |

The seeder creates the two demo tenants and the demo operative's person + engagement (so `operative@` resolves),
but not the wider history — run **Create demo data** for the sites, cards, attendance and commercial history. The
platform administrators are created by `AdminUserSeeder`; `DemoLoginSeeder` heals their password to the demo admin
value when demo mode is on. The passwords are held once in `DemoCredentials` so the plan and the seeder can't drift.

Alongside the organisation data, the seeder writes **comprehensive history** so every console and reporting
page shows live data:

- **Module entitlements** switched on for both companies.
- **Qualification cards** per operative — a deliberate mix of valid, expiring-soon and expired cards (plus
  supervision, working-at-height and first-aid cards by trade) so the compliance roll-up is non-trivial.
- **Attendance** — ten working days of sign-in/out per operative. Gated sites record accepted QR scans within
  the boundary; the subcontractor's dispersed/retrofit sites have no boundary, so attendance is recorded and
  **flagged** (the "workforce management beyond the site gate" path).
- **Commercial database** (reporting): one direct-debit **mandate** and a metered **subscription** per company,
  **twelve months of monthly payments** (including a failed-then-re-taken month and an in-flight latest
  collection), and Tedwren's own **BACS payouts**. These populate `/admin/payments`, `/admin/billing`,
  `/admin/subscriptions` and `/admin/payouts`.

## Seed / recreate / delete

The page shows the current status (present or not, with per-area counts) and three actions:

- **Create / Recreate** — builds the dataset. If it already exists, it is cleared first and rebuilt, so
  recreate is always a clean rebuild.
- **Delete** — removes the entire dataset from both databases.

Each action runs inside a **MudDialog** with a **progress bar** that polls the server's staged progress.
Delete and recreate are confirm-gated.

## How it stays precise

Every record's identifier is derived deterministically from a fixed namespace plus a stable key
(`DemoDataIds.Derive`). A single deterministic plan (`DemoDataPlanBuilder.Build`) is the source of truth for
both seeding (insert every record) and teardown (delete every record by id, in reverse dependency order). This
guarantees:

- **Nothing outside the two demo companies is ever touched.**
- Delete removes exactly what seed created — no orphans, no guesswork.
- Recreate is idempotent (the same ids every time).

## Where it lives

| Concern | Location |
|---|---|
| Contract + DTOs | `Tedwren.Abstractions.Services.IDemoDataService`, `Tedwren.Abstractions.Contracts.DemoData` |
| Service + plan | `src/Tedwren.Application/DemoData/` (`DemoDataService`, `DemoDataPlanBuilder`, `DemoDataIds`, `DemoDataProgressState`) |
| Startup login seed | `src/Tedwren.Application/DemoData/DemoLoginSeeder.cs` (+ `DemoCredentials.cs`), wired in `Program.cs` after `AdminUserSeeder` |
| Teardown deletes | `DeleteAsync(id)` on the touched repositories (Dapper dual-engine + in-memory doubles) |
| API | `src/Tedwren.Api/Endpoints/DemoDataEndpoints.cs` (`/api/admin/demo-data`, `PlatformAdmin`) |
| Client | `ApiDemoDataService`, `Pages/Admin/AdminDemoData.razor`, `Pages/Admin/DemoDataProgressDialog.razor` |
| Tests | `DemoDataServiceTests`, `DemoLoginSeederTests`, `DemoDataApiTests` |

The full-dataset service is **not** run at startup — it is only ever triggered by an operator from the admin
portal. Only the lightweight published sign-in accounts above are seeded at startup, and only when `Demo:Enabled`.
The on-demand seed skips any of those accounts already present, so the two coexist.
