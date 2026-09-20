# Tedwren mobile — store-readiness checklist (M8)

> The release runway for the .NET MAUI field app (`src/Tedwren.Mobile`). It enumerates every gate that stands
> between the current code and a store submission, with an honest status for each. The PRD
> (`docs/TedwrenPRDv6_4.docx`, mirror `docs/TedwrenPRDv6_4.md`) remains the source of truth; where this disagrees
> with the PRD, the PRD wins, and the discrepancy is raised (§ "Decisions to raise with Leigh").

**Status legend**

| Status | Meaning |
|---|---|
| ✅ **done** | Implemented and verified in the buildable solution (`Tedwren.sln`, server + `Mobile.Core`). |
| 🟦 **CI** | Code is written but only compiles/builds on a MAUI-capable runner (Android on Linux/CI, iOS on macOS). Hand-reviewed here. |
| 📱 **device** | Requires a physical device / emulator / simulator to verify. |
| 👤 **human** | A person must do it (store consoles, signing, legal, procurement). |
| 🟨 **raise** | PRD-silent — an engineering/commercial decision for Leigh before it can be closed. |

---

## 1. Performance

| Item | Status | Notes |
|---|---|---|
| R14 — site-entry decision < 3s | ✅ **done** (server) / 📱 **device** (end-to-end) | Server round-trip is instrumented (`EntryDecisionResultDto.ElapsedMs`). M8 adds the **client-side** round-trip via `ITelemetry.TrackTiming` (`site-entry.decide.roundtrip`, `attendance.signin.roundtrip`). Verify the wall-clock budget on real devices over a real network. |
| Cold-start time | 📱 **device** | Measure app launch → first interactive frame on a low-end Android and a baseline iPhone. Session resume is biometric-gated + does a token refresh; keep the loading page responsive. |
| Offline read latency | 📱 **device** | `EncryptedStore` (SQLCipher) reads fail soft; confirm cached dashboards/muster render instantly offline. |
| List/scroll smoothness | 📱 **device** | Operatives / submissions / evidence lists — verify no jank with realistic row counts. |

## 2. Accessibility

| Item | Status | Notes |
|---|---|---|
| Screen-reader semantics on shared controls | 🟦 **CI** | `SemanticProperties.Description` on `TwMenuTile`, `TwKpiCard`, `TwStatusPill`; decorative glyphs hidden (`TwEmptyState`, `TwSkeleton`, `TwDonutStat` exposes a spoken split). |
| VoiceOver (iOS) / TalkBack (Android) pass | 📱 **device** | Navigate every screen with the screen reader on; confirm reading order, labels on icon-only affordances, and that the signature pad + dynamic form fields are announced. |
| WCAG 2.2 AA conformance target | 🟨 **raise** | The PRD does not state a conformance target. M8 **assumes** WCAG 2.2 AA — confirm with Leigh. Then run a full audit (contrast in both themes, tap-target size ≥ 44×44, dynamic-type scaling, focus order). |
| Dynamic type / large fonts | 📱 **device** | Verify layouts hold at the OS's largest accessibility text sizes. |

## 3. Tablet & responsive layout

| Item | Status | Notes |
|---|---|---|
| Tablet card grids (3-up) | 🟦 **CI** | `TileGrid` widens to 3 columns on `DeviceIdiom.Tablet`. |
| iPad enabled | 🟦 **CI** | `Info.plist` targets iPad; `SupportedOSPlatformVersion` ios 15.0. |
| Master-detail on wide screens | 📱 **device** | Confirm the manager list/detail pages use the extra width sensibly; no stretched two-column phone layout. |

## 4. Security review

| Item | Status | Notes |
|---|---|---|
| One-operative-per-device binding | ✅ **done** | `OperativeDevice` enforces one active device per person **and** one person per device; enrolment is SMS-OTP gated; re-bind is a console admin action. |
| Token storage | ✅ **done** / 🟦 **CI** | Access/refresh tokens + the DB key live in the OS secure enclave (`ISecureStore` → Keychain / Keystore). Operative + manager tokens are `aud`-scoped and role-gated server-side (R15). |
| Encryption at rest | 🟦 **CI** / 📱 **device** | SQLCipher (AES-256) via `Microsoft.Data.Sqlite.Core` + `SQLitePCLRaw.bundle_e_sqlcipher`, key applied as a `PRAGMA key`. Verify the DB file is unreadable without the key on a rooted device. |
| Biometric-gated DB key | 🟦 **CI** / 📱 **device** | The SQLCipher key read is gated behind a local biometric unlock once per app run (in addition to the enclave); skipped when no biometric is enrolled (enclave still protects). |
| Real biometric unlock | 🟦 **CI** / 📱 **device** | `BiometricPrompt` (Android, `BiometricWeak | DeviceCredential`) / `LAContext` `DeviceOwnerAuthentication` (iOS). **Local unlock only — R17**; no biometric leaves the device, so no DPIA for this release. Verify prompt, cancel, lockout, and the no-biometric fallback. |
| TLS certificate pinning | 🟦 **CI** / 👤 **human** | `TlsPinning` validates the server SPKI SHA-256 against a pin set on every client. **The pin set is empty** — populate it with the production leaf **and** a backup/intermediate pin before submission, or a routine cert renewal bricks the app. DEBUG bypasses pinning for the dev cert. |
| Root / jailbreak awareness | 🟨 **raise** / 📱 **device** | Not implemented. Decide whether to add a root/jailbreak signal (deterrent only). Low priority given enclave + SQLCipher. |
| No secrets in the binary | ✅ **done** | No API keys/secrets are compiled in; auth is user-credential/OTP based. Confirm again after wiring the production base URL + pins. |

## 5. Telemetry, crash reporting & analytics

| Item | Status | Notes |
|---|---|---|
| Telemetry seam | ✅ **done** | `ITelemetry` (Core) + `NoOpTelemetry` default; `LoggingTelemetry` (MAUI) logs via `ILogger` and never throws. |
| Global crash capture | 🟦 **CI** / 📱 **device** | `App` hooks `AppDomain.UnhandledException` + `TaskScheduler.UnobservedTaskException` → `ITelemetry.TrackError`. Add the platform hooks (`AndroidEnvironment.UnhandledExceptionRaiser`, `NSSetUncaughtExceptionHandler`) during device hardening. |
| Real vendor (UK-hosted) | 🟨 **raise** | **PRD is silent** on telemetry/crash/analytics. **R13 requires personal data to stay in the UK**, so any vendor must be UK-hosted (or self-hosted). Leigh's call — App Center is retiring; candidates: self-hosted Sentry (EU/UK region), a UK-region OpenTelemetry sink. Until chosen, `LoggingTelemetry` keeps the seam useful without shipping data off-device. |
| Consent / data-minimisation | 🟨 **raise** | If a vendor is adopted, decide the consent model and scrub PII from events (the seam only sends event names + timings today). |

## 6. Push notifications

| Item | Status | Notes |
|---|---|---|
| Push channel | 🟨 **raise** | **PRD-silent.** "Forms due/assigned" and "hazard reported → manager notified" are currently email (server `RecurringFormReminderJob` / HSE alerts). Push is an *enhancement* — decide whether to add FCM/APNs (needs a UK-hosted push-orchestration decision under R13, per-device token registration, and a notifications-permission flow). Not built. |

## 7. Privacy manifests & store data declarations

| Item | Status | Notes |
|---|---|---|
| iOS privacy manifest | 🟦 **CI** / 👤 **human** | `Platforms/iOS/PrivacyInfo.xcprivacy` declares **no tracking**, precise-location + photos as *app functionality* (linked to the user), and required-reason APIs (UserDefaults CA92.1, FileTimestamp C617.1). Re-validate against Apple's current list at submission. |
| iOS usage strings | 🟦 **CI** / 📱 **device** | Confirm `NSCameraUsageDescription`, `NSLocationWhenInUseUsageDescription`, `NSFaceIDUsageDescription` are present and human-worded in `Info.plist`. |
| Android data-safety form | 👤 **human** | Complete the Play Console Data safety form to match the manifest: location + photos collected for app functionality, linked to the user, not shared, not used for tracking; encrypted in transit + at rest. |
| Android permissions audit | 🟦 **CI** / 📱 **device** | `CAMERA`, `ACCESS_FINE_LOCATION`, `USE_BIOMETRIC`, `INTERNET` — confirm nothing broader is declared. |

## 8. App Store / Play Store submission

| Item | Status | Notes |
|---|---|---|
| App identity | 🟦 **CI** | `ApplicationId = io.hydes.tedwren`, title "Tedwren", version 1.0 (1). Confirm the bundle id / package name is reserved in both consoles. |
| App name reservation | 👤 **human** | PRD task-24 (name reservation) — reserve "Tedwren" in App Store Connect + Play Console. |
| Signing & provisioning | 👤 **human** | Android upload/app-signing keys; iOS distribution certificate + provisioning profile + App Store Connect app record. |
| iOS build | 👤 **human** / 📱 **device** | **Requires macOS + Xcode** — cannot build on this Linux container. Set up a macOS CI runner. |
| Screenshots & metadata | 👤 **human** | Phone + tablet screenshots (both themes), description, keywords, support URL, privacy-policy URL. |
| Age rating / content | 👤 **human** | Complete both consoles' questionnaires (no objectionable content; a workforce/utility app). |

## 9. Offline / background sync

| Item | Status | Notes |
|---|---|---|
| Foreground + on-resume sync | ✅ **done** (Core) / 🟦 **CI** | `SyncEngine` drains the outbox on connectivity-up and on app start/resume (`App.OnStart`/`OnResume`); ordered, idempotent, retry/backoff, upload-checkpointed. Unit-tested in `Mobile.Core.Tests`. |
| True OS background sync (app closed) | 🟨 **raise** / 📱 **device** | Not built — WorkManager (Android) / BGTaskScheduler (iOS) are easy to get wrong un-tested. Decide whether the field workflow needs sync while the app is fully closed (foreground/resume sync may suffice). Device-gated follow-up. |
| Orphan-image cleanup | 🟦 **CI** | An uploaded image whose submission never lands is currently retained. Add a server-side sweep (post-M8 backlog). |

## 10. Fonts & branding

| Item | Status | Notes |
|---|---|---|
| Inter typeface | 👤 **human** / 🟦 **CI** | The brand typeface is Inter. Add the **OFL** `.ttf` files to `Resources/Fonts/` and uncomment the `AddFont` calls in `MauiProgram` (see `Resources/Fonts/README.md`). Until then the OS default renders. |
| Splash & icon | 🟦 **CI** / 📱 **device** | Brand-orange `#E8590C` splash + white Tedwren logo; white/transparent "T" adaptive icon. Verify on device (adaptive icon masks, dark/light). |

## 11. Configuration

| Item | Status | Notes |
|---|---|---|
| Production API base URL | 🟦 **CI** / 👤 **human** | `MauiProgram.ApiBaseUrl` → `TedwrenApiOptions` is `https://localhost:7296/` for dev. A physical device cannot reach `localhost` — set the real UK-hosted API origin per build configuration before device testing / submission. |
| CORS | ✅ **done** | Native clients don't need CORS, but confirm the API's `Cors:AllowedOrigins` still serves the web client. |
| AndroidX.Biometric version | 🟦 **CI** | `Xamarin.AndroidX.Biometric 1.1.0.29` is pinned; **confirm/adjust to the installed MAUI workload's AndroidX set on the first CI build** to avoid a version clash. |

---

## Decisions to raise with Leigh (PRD-silent — raise, don't work around)

1. **Telemetry / crash-reporting vendor** — none specified; must be UK-hosted (R13). Approve a vendor or accept
   the logging-only seam for launch.
2. **Push notifications** — not in the PRD; today's reminders/alerts are email. Approve adding push (with a
   UK-hosted orchestrator) or defer.
3. **WCAG conformance target** — not stated; M8 assumes **WCAG 2.2 AA**. Confirm the target for the audit.
4. **True OS background sync** — confirm whether sync-while-closed is required, or foreground/resume sync suffices.
5. **Root/jailbreak detection** — confirm whether a deterrent signal is wanted.

## Boundaries documented (no action, for the record)

- **R17** — this release does **on-device biometric *unlock* only**; no biometric data is processed server-side,
  so **no DPIA is triggered** by the mobile app. Face-match-at-sign-in (with liveness) remains **PRD Phase 5** and
  needs a DPIA + a non-biometric alternative before it can be built.
- **R1** — the app is the optional Q8/Q14 layer; the site gate and induction always work in a phone browser and
  are never replaced.
- **R2 / R3** — attendance sign-in and the site-entry decision are online-only, fail-closed, and never queued
  offline; only evidence/forms capture and read caches work offline.

## Post-M8 feature backlog (deferred, not hardening)

Multipart forms upload (base64 today); operative submission-history read-back (needs an operative image/file GET —
`/api/images/{id}` + form files routes are console-only, R9); configurable competency-cover (MC-13, multi-competency
+ config — a PRD gap to raise); evidence-pack ZIP on device (summary only on mobile today); the Blazor **console**
evidence-review page (the API serves it; the native page is the M7 deliverable); site-documents (MC-27, needs a
Site↔Document model that does not exist yet).
