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
| Teardown deletes | `DeleteAsync(id)` on the touched repositories (Dapper dual-engine + in-memory doubles) |
| API | `src/Tedwren.Api/Endpoints/DemoDataEndpoints.cs` (`/api/admin/demo-data`, `PlatformAdmin`) |
| Client | `ApiDemoDataService`, `Pages/Admin/AdminDemoData.razor`, `Pages/Admin/DemoDataProgressDialog.razor` |
| Tests | `DemoDataServiceTests`, `DemoDataApiTests` |

The service is **not** run at startup — it is only ever triggered by an operator from the admin portal.
