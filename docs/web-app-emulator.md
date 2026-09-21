# Tedwren mobile emulator (`Tedwren.Web.App`)

`src/Tedwren.Web.App` is a **browser emulator of the native mobile app** (`src/Tedwren.Mobile`). It exists so the
mobile app's functionality can be exercised **without an iOS/Android device** — for day-to-day testing, demos and
CI — while staying **like-for-like** with the app: the same flows, the **same API endpoints**, and phone/tablet
form factors only. It is completely separate from the web console (`src/Tedwren.Client`).

> **Keep it in lockstep.** Any change to the mobile app (native head or `Tedwren.Mobile.Core`) must be mirrored in
> the emulator, and vice-versa (see `CLAUDE.md` → Engineering standards). They deliberately share code so drift is
> minimised.

## How it stays faithful

The mobile app's *logic* already lives in **`src/Tedwren.Mobile.Core`** (a plain `net10.0` library): the typed API
clients, the operative + manager session managers, the `SyncEngine` outbox, the forms/validation engine, caching,
and the device-`Platform/` interfaces. The emulator **references that library unchanged**, so its API calls and
business logic are *identical* to the app's. Only two things are re-implemented for the browser:

1. **The UI** — the native MAUI screens (native `Tw*` controls, C#) are re-created as **Blazor pages + a faithful
   `Tw*` component kit** built on the shared `tokens.css` (copied at build, like `Tedwren.Web`). No MudBlazor / no
   console components.
2. **The four device seams** — `ISecureStore`, `IBiometricAuthenticator`, `IConnectivityService`, `ITelemetry`
   (plus the read cache / outbox / form-draft stores the device's SQLCipher store backs), swapped for browser
   implementations under `src/Tedwren.Web.App/Platform`.

TLS certificate pinning is dropped (the browser owns TLS). Everything else in `Tedwren.Mobile.Core` runs in the
WASM sandbox unchanged.

### Browser platform implementations (`src/Tedwren.Web.App/Platform`)

| Seam | Web implementation |
|---|---|
| `ISecureStore` | `WebSecureStore` — `localStorage` (a **test** surface, not a secure enclave). |
| `IBiometricAuthenticator` | `WebBiometricAuthenticator` — reports "unavailable", so resume proceeds without a prompt. |
| `IConnectivityService` | `WebConnectivityService` — a cached flag driven by the emulator's offline toggle. |
| `ITelemetry` | `WebTelemetry` — logs to the browser console; never throws. |
| `IReadCache` | `WebReadCache` — in-memory + `localStorage` write-through. |
| `IOutboxStore` | `WebOutboxStore` — in-memory + `localStorage`, so queued captures survive a reload. |
| `IFormDraftStore` | `WebFormDraftStore` — in-memory form-draft store. |
| geolocation | `WebGeolocation` — `navigator.geolocation`, falling back to null (SF-15). |

### Capability stubs (browser vs device)

| App capability | In the emulator |
|---|---|
| Camera (evidence / hazard / form photos) | `<InputFile accept="image/*" capture="environment">` |
| GPS (attendance / hazard) | `navigator.geolocation`, else null (location-optional path) |
| Biometrics | reported unavailable (resume proceeds) |
| Signature pad | a `<canvas>` (`TwSignaturePad`) that emits a PNG data URL |
| Offline capture + sync | in-memory + `localStorage` outbox, drained by the reused `SyncEngine` on reconnect |
| File download (reports) | browser blob download |

## Sign-in

- **Operative (field) home** → `operative@tedwren.com`. A browser can't receive an SMS one-time code, so the
  emulator uses a **Development-only** demo sign-in: `POST /api/mobile/auth/demo-sign-in` mints a real operative
  token (`tedwren-mobile` audience, `Operative` role, bound `device_id`) for a **seeded demo operative**. It is
  **fail-closed**: the endpoint is only mapped when the environment is not Production **and** `Demo:Enabled` is
  true; `OperativeAuthService.DemoSignInAsync` double-checks the flag; and `StartupSecurity` refuses to boot
  Production while `Demo:Enabled` is on. `contractor@`/`subcontractor@` are unaffected.
- **Manager / admin home** → `contractor@tedwren.com` or `subcontractor@tedwren.com` (password `Demo123!`), via the
  existing console `POST /api/auth/login` — exactly as the app's "Site manager / admin" flow.

`Demo:Enabled` defaults **off** in `src/Tedwren.Api/appsettings.json` and is **on** in
`appsettings.Development.json`. The demo operative (a `Person` + active `Engagement` on the main contractor, plus a
CSCS card) is seeded with the rest of the demo dataset (`DemoDataPlanBuilder` / `DemoOperatives`).

## Run it

```bash
# 1. API (Development ⇒ Demo:Enabled=true). Add the emulator origin to Cors:AllowedOrigins in
#    src/Tedwren.Api/appsettings.json (e.g. https://localhost:7180) — already added for the default port.
dotnet run --project src/Tedwren.Api

# 2. Seed the demo dataset once (console → admin → Demo data → Create), so contractor@, subcontractor@ and the
#    demo operative exist.

# 3. The emulator (serves on https://localhost:7180 by default).
dotnet run --project src/Tedwren.Web.App
```

Then, in the emulator: on `/` choose **operative** → "Sign in as demo operative" → exercise attendance, capture,
hazards, forms, hours, cards. Or sign in as `contractor@tedwren.com` → the manager surfaces (muster, site entry,
operatives, forms, evidence, reports). Toggle **Phone/Tablet**, **light/dark** and **online/offline** in the
emulator chrome. `/kit` renders the component kit.

## Build & test

The emulator is in `Tedwren.sln`, so CI builds and tests it (no MAUI workload):

```bash
dotnet build Tedwren.sln
dotnet test  Tedwren.sln   # incl. tests/Tedwren.Web.App.Tests (bUnit) and the demo-auth API tests
```

## Delivery (W-App track)

Built in phases, each independently buildable and not breaking existing functionality:

- **WA1** — scaffold: project, DI over `Tedwren.Mobile.Core`, browser platform seams, device frame, faithful `Tw*`
  kit, role chooser; wired into `Tedwren.sln` + CI.
- **WA2** — operative demo login end-to-end (`operative@tedwren.com` → field home; fail-closed server path).
- **WA3** — operative field screens (attendance, capture, hazard, forms inbox/fill, hours, cards, profile).
- **WA4** — manager screens (home, muster, site entry, operatives + detail, forms manage/assign/review, evidence,
  reports).
- **WA5** — hardening (`ErrorBoundary`, global exception hooks, per-screen loading/empty/error) + tests.
- **WA6** — offline emulation (connectivity toggle + `localStorage`-persisted outbox/cache).
