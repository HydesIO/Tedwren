# Building the Tedwren mobile app

The MAUI field app (`docs/mobile-app-plan.md`) is split across two solutions on purpose.

## Two solutions

| Solution | Contains | Builds where |
|---|---|---|
| `Tedwren.sln` | Everything except the MAUI heads — **including `src/Tedwren.Mobile.Core` and `tests/Tedwren.Mobile.Core.Tests`**. | Any .NET 10 SDK, including Linux CI. No MAUI workload needed. |
| `Tedwren.Mobile.slnx` | The MAUI-track: `Tedwren.Mobile` (app head), `Tedwren.Mobile.Controls`, `Tedwren.Mobile.Core` (+ its refs and tests). | A machine with the MAUI workloads (and, for iOS, macOS + Xcode). |

**Why split:** the MAUI heads target `net10.0-android` / `net10.0-ios`, which need the `maui-android` /
`maui-ios` workloads (and the Android SDK / Xcode). Keeping them out of `Tedwren.sln` means the existing solution
and its CI keep building on Linux with a plain SDK, exactly as before. **All testable mobile logic lives in
`Tedwren.Mobile.Core`** (a plain `net10.0` library), so it is covered by `dotnet test Tedwren.sln`; the MAUI heads
stay thin (UI + platform glue).

## Prerequisites for the MAUI heads

```bash
dotnet workload install maui-android      # Android (Linux/macOS/Windows)
dotnet workload install maui-ios          # iOS (macOS only)
```

- **Android** also needs a JDK (17+) and the Android SDK (platform + build-tools). On a dev box the workload can
  provision the Android SDK; on CI, install it and set `ANDROID_HOME`.
- **iOS** builds require **macOS + Xcode** — they cannot be built on Linux.

## Build & test

```bash
# Core + server + Core tests (Linux/CI friendly)
dotnet build Tedwren.sln
dotnet test  Tedwren.sln

# MAUI heads (workload machine)
dotnet build Tedwren.Mobile.slnx -f net10.0-android
dotnet build Tedwren.Mobile.slnx -f net10.0-ios        # macOS only

# Run on a device/emulator
dotnet build src/Tedwren.Mobile/Tedwren.Mobile.csproj -t:Run -f net10.0-android
```

## Configuration & assets to add before device testing
- **API base URL** — `MauiProgram.ApiBaseUrl` defaults to `https://localhost:7296/`; point it at a reachable
  host (a device cannot reach `localhost`). The API's `Cors:AllowedOrigins` does not apply to native HTTP.
- **Inter fonts** — add the OFL `.ttf` files and uncomment the `AddFont` lines (see
  `src/Tedwren.Mobile/Resources/Fonts/README.md`).
- **Branding** — `Resources/AppIcon/appicon(fg).svg` and `Resources/Splash/splash.svg` carry the brand-orange
  tile + white "T" (derived from `src/Tedwren.Client/wwwroot/images/logo-icon.svg`).
