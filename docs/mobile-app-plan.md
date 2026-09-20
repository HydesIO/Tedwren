# Tedwren field mobile app (.NET MAUI, Android + iOS) — plan & scope

> This is the in-repo copy of the approved delivery plan for the mobile (**M-track**) work. It complements
> `docs/plan-and-scope.md` (the console/API plan). The PRD (`docs/TedwrenPRDv6_4.docx`, mirror
> `docs/TedwrenPRDv6_4.md`) remains the source of truth; where this disagrees with the PRD, the PRD wins.

## Context

Tedwren's primary daily user is the **operative** (a trade worker on site); its secondary daily user is the
**site manager**. Operatives are not application users today — they are `Person` + `Engagement` records keyed by
mobile number (SF-1), reached only through anonymous browser links. The PRD anticipates this app as an open
decision, not a prohibition: **R1** narrows "no app install" to mean the *gate and induction* must always work
in a browser, while an *optional app for self-service* is sanctioned (**Q8**) and an *optional app for verified
attendance* is sanctioned (**Q14 / §8.5**). This app is that optional layer — it never replaces the browser
paths.

The app is one binary with a **role-switched shell** (operative vs manager/admin), enterprise-grade, offline
capable, with an encrypted local store, biometric unlock, one-operative-per-device, mobile-number-only operative
login (anti buddy-punching deterrent), offline evidence-photo capture with sync, and a **comprehensive native
implementation of the forms/inspection library**.

### Decisions
1. **No in-app AI** — the app surfaces existing Tedwren functionality; no LLM/assistant.
2. **Local store = SQLite + SQLCipher** (AES-256) — relational fit for an append-only outbox + draft store.
3. **Anti-buddy-punching = device-bind + biometric unlock** (mobile no. + SMS OTP enrols one operative to one
   device; Face/Touch ID unlocks). A deterrent, not identity proof; face-match-at-sign-in stays PRD Phase 5
   (needs DPIA + non-biometric alternative, R17).
4. **Forms library integrated comprehensively**, as its own phase (M6).

## Guiding PRD constraints
- **R1** — app is optional; sign-in/out and induction always work in a phone browser.
- **R2 / R3** — the site-entry decision and attendance sign-in use current data, never cached: online-only,
  fail-closed, never queued offline. Offline = evidence/forms capture + read caches only.
- **R4 / R16** — captured records are append-only; corrections are new records; payroll figures stay traceable.
- **R11** — store UTC, display UK local; offline capture stamps UTC.
- **R13** — personal data stays in the UK (all sync targets the UK-hosted API).
- **R14** — sign-in decision < 3s (instrument the round-trip).
- **R15** — tenant isolation; operative token scoped so person A can't act as person B.
- **R17** — this release does on-device biometric *unlock* only (no server-side biometric processing, no DPIA).
- **R18** — subcontractor context never says "permitted/denied".

## Architecture

Native XAML/C# MAUI (not Blazor Hybrid) for a fluid, native feel; UI consistency comes from porting the design
tokens, not embedding web components.

| Project | Responsibility |
|---|---|
| `src/Tedwren.Mobile` | MAUI app head (net10.0-android; net10.0-ios). Shell, pages, platform services. |
| `src/Tedwren.Mobile.Core` | **Workload-free** net10.0 logic: API clients, offline store, sync engine, form model/validation, session/auth, view models. Unit-tested off-device. |
| `src/Tedwren.Mobile.Controls` | MAUI control kit + design tokens (ported from `tokens.css`) + `TwDynamicForm`. |
| `tests/Tedwren.Mobile.Core.Tests` | xUnit tests for the Core library. |

Reuse: `Tedwren.Abstractions` (all DTOs/interfaces), `Tedwren.Domain` value objects (`PhoneNumber`, `Geofence`),
the `AuthTokenHandler` bearer pattern (SecureStorage instead of localStorage), and `MusterDto.GeneratedUtc`
(offline data-age, MC-14). See `docs/mobile-app-build.md` for the two-solution layout and toolchain.

### Branding, navigation & role dashboards
- **Splash & icon**: reuse `src/Tedwren.Client/wwwroot/images/logo-icon.svg` (brand-orange tile + white "T").
  `MauiSplashScreen` = brand-orange `#E8590C` + white logo; `MauiIcon` = white/transparent "T".
- **Two role home experiences**, each an interactive card **menu** + its own **overview dashboard** (operative:
  today's site, hours, forms due; manager: KPIs, muster, competency cover, submissions to review — reusing the
  console's `IDashboardService`). Built from ported card controls so the app reads as one system with the console.

## Server-side additions (built in the existing projects, tested on the InMemory host)
1. **Operative identity, device binding & tokens** — `OperativeDevice` / `OtpChallenge` entities + enum; operative
   JWT (audience `tedwren-mobile`, `sub`=PersonId, bound `device_id`) + `RequireOperative` policy; refresh tokens;
   `/api/mobile/auth/*` (request-otp / verify-otp / refresh) reusing the existing `ISmsSender`; console device
   revoke/re-bind. Attendance/form submit take `PersonId` from the token, never the body.
2. **Operative-scoped surface** — `/api/mobile/me`, my-hours (SUB-27), cards/compliance, assigned sites,
   site-documents (MC-27), `/api/mobile/dashboard`; attendance sign-in/out (online-only, fail-closed).
3. **Binary upload** — `POST /api/mobile/uploads` (multipart, idempotent) via `IImageStore`; served by the
   authorised `GET /api/images/{id}` (R9).
4. **Forms (mobile)** — `/api/mobile/forms/*` (assignments-for-me, template delta sync, multipart submit) reusing
   the form services + `ModuleGate("forms")`; `GetForOperativeAsync` / `SubmitForOperativeAsync`.

## Forms & inspection engine — comprehensive integration
Reuse the whole server engine (versioned append-only templates, `IFormTemplateService` /
`IFormSubmissionService` / `IFormAssignmentService`, DTOs, PDF / failure-alert / scheduling). Add a native
`TwDynamicForm` renderer for all 14 `FormFieldKind`s (output `FormAnswerDto[]` + multipart file parts), an
operative "forms for me" query, immutable version caching, offline **draft/autosave/resume**, a schedule-aware
"forms due" inbox, idempotent multipart submit, client-side validation, and submission history + PDF. Managers
assign/review via the existing `/api/forms/*`.

## Phasing (M1–M8)
- **M1 — Foundation / walking skeleton** *(landed)*: projects, design-token port, brand splash + icon,
  role-switch shell → two card-menu homes + dashboard shells, SQLite+SQLCipher planned, Core + tests green.
- **M2 — Operative auth** (mobile + OTP + device bind + biometric) *(landed)*.
- **M3 — Operative surface + read caches + operative dashboard** (`/api/mobile/dashboard`) *(landed)*.
- **M4 — Attendance sign-in/out** (online-only, geofenced) *(landed)*: token-scoped PersonId + R15 site
  guard over the existing `IAttendanceService`; `/api/mobile/attendance/{sign-in,sign-out,current}`; live
  dashboard on-site state; MAUI `SignInOutPage` (cached site, location + geofence hint, online-only, R18 wording).
- **M5 — Offline capture & sync foundation** (encrypted outbox, multipart upload, sync engine, photo+GPS) *(landed)*:
  SQLCipher `EncryptedStore` (read cache + append-only outbox); connectivity-driven `SyncEngine`
  (ordered, idempotent, retry/backoff, upload-checkpointed); `POST /api/mobile/uploads` (multipart) reusing
  `IImageStore`; **two consumers** — ungated generic **evidence** (`EvidenceItem`, net-new) for every operative,
  and **`hse`-gated hazard/near-miss** reusing the existing HSE domain; MAUI `CaptureEvidencePage` +
  `ReportHazardPage` (camera + GPS, offline-first) + a pending-sync badge.
- **M6 — Forms & inspection engine (comprehensive)** *(landed)*: operatives complete assigned forms
  offline (all 14 field kinds, photos, signatures, RAG) with draft autosave/resume, syncing idempotently. Reuses
  the server engine (`SubmitForContextAsync` + `GetTemplateForFillAsync`; client-implemented interfaces untouched);
  idempotency via a `ClientId` on `CreateFormSubmissionRequest` (no new table); mobile-only `IMobileFormService`
  resolves "forms for me"; `/api/mobile/forms/*` (RequireOperative + `ModuleGate("forms")`); native `TwDynamicForm`
  renderer + `FormsInboxPage`/`FormFillPage`; forms outbox handler + draft store in `EncryptedStore`.
- **M7 — Manager/admin mode (comprehensive)** *(this increment)*: a role-switched native manager experience over the
  **console** plane — managers sign in with console email + password (`/api/auth/login`), which satisfies the API's
  secure-by-default fallback policy, so the dashboard, forms, workforce and decision endpoints are reused **as-is**.
  New server (no tables/migration): read-only evidence review (`IEvidenceCaptureQueryService` + `/api/evidence-captures`,
  R15, photo via `/api/images/{id}`) over the M5 `EvidenceItem`; authenticated muster + decide/override
  (`/api/manager/muster/{siteId}`, `/api/manager/entry/decide` — `CompanyId` from token, override attributed to the
  signed-in manager, MC-11). Console token has no refresh → re-login on expiry. Client: `ManagerSessionManager`
  (persisted biometric-gated resume) + `ManagerAuthMessageHandler` (no refresh); manager API clients +
  `ManagerDataService` (cache-then-network, MC-14 muster age); ported R18 `SiteGateResultPresenter`. MAUI head:
  `TwKpiCard`/`TwStatusPill`/`TwEmptyState`; live `ManagerHomePage` + muster / site-entry / operatives / forms
  (assign + review) / evidence / reports pages. Auditor role is read-only (RequireWrite gates review + override).
- **M8 — Hardening & store readiness** (perf, a11y, tablet, security review, store submission).

Out of scope: in-app AI; face-match-at-sign-in (PRD Phase 5, DPIA-gated).

## PRD notes to raise (raise, don't work around)
The app itself is Q8/Q14 (sanctioned, unspecified in detail); mobile-number+OTP login and one-device-per-operative
are SF-1-aligned design choices, not mandates; geotagged/timestamped in-app photos and push notifications for due
forms are enhancements beyond the current forms spec (`TedwrenPRDv6_4.md:156`).
