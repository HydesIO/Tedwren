# Worker Passport — plan of action

**Status: planning only.** This document converts `docs/TedwrenWorkerPassportPRDv0_1.docx` (mirror:
[`TedwrenWorkerPassportPRDv0_1.md`](TedwrenWorkerPassportPRDv0_1.md)) into a phased plan of works. **No
feature code has been written for it yet.** The passport's new persisted schema is deliberately held until
the open PRD questions Q1/Q2 are signed off (see *Gating pre-work*). Requirement/rule IDs (WP-/W-/SF-) are
referenced, not reproduced — read them in the PRD mirror.

## Context

The PRD introduces a **third Tedwren product**: the **Worker Passport** — an individual construction
worker's own credential record (personal details, emergency contacts, and credentials with real expiry
logic), built on the existing platform's shared foundation. It is worker-owned, worker-paid (£10/yr, Stripe
consumer checkout), mobile-web-first, and deliberately **not** a rival to the CSCS Digital Skills Passport.
The PRD defines requirements **WP-1…WP-18** (P0/P1), rules **W1–W7**, the **CSCS licensing constraint (§7)**,
and **four open questions (Q1–Q4)** that gate the build.

Two things are planned here: the phased product build, and — called out specifically — a **Worker Passports
section in the platform admin area** to track passports comprehensively. Per the decisions below the admin
section is scoped to **operational metrics only** so it does not breach Rule W5.

### Decisions taken

| Decision | Choice | Consequence |
|---|---|---|
| What the admin section exposes | **Operational metrics only** | Counts / states / activity — **no** credential images, personal details or emergency-contact content. Honours Rule W5 ("no administrative override") and the DPIA posture (§9.3). |
| Q1 (data controller) / Q2 (identity collision) | **Hold schema until sign-off** | Plan the passport data model; persist nothing new until Leigh confirms the controller + collision positions. |

The v0.1 PRD becomes **PRD v6.4 §5.4 only after Leigh & James have commented** — so `TedwrenPRDv6_4.*` is
**not** edited by this plan; the §5.4 merge is a downstream step.

## Reuse map — existing foundation vs. genuinely new

The shared foundation is substantially built. Reuse it; do not fork it.

**Reuse unchanged**

- `Person` (`src/Tedwren.Domain/Entities/Person.cs`) + `PhoneNumber` value object — SF-1 identity key. A
  passport is a *view of* this record, not a second record (WP-2).
- `Engagement` + `Company` (`src/Tedwren.Domain/Entities/`) — SF-2 per-company view; the passport must not
  become a hole in this (Q2).
- `QualificationCard` + `QualificationType` + `DefaultQualificationLibrary` — credentials with issuer (on the
  type), reference (`CardNumber`), `IssuedOn`/`ExpiresOn`, `CaptureSource`, `ImageReference`, renewal history
  (`Supersedes…`), and **computed** currency `GetStatus()` (SF-8). Covers WP-3.
- `QualificationService` (`src/Tedwren.Application/Qualifications/`) — capture / confirm / renew / shortfall.
- `StoredImage` + `IImageStore` — SF-5 photo capture (WP-3).
- Induction plumbing (`InductionSession` / `InductionTemplate` / `InductionService`) — the hook for WP-9.

**Reuse with a decision to make**

- `CardVerificationState` = `ReadUnchecked` / `CustomerChecked` / `CscsVerified`
  (`src/Tedwren.Domain/Enums/CardVerificationState.cs`) is the WP-4 three-state status
  (self-declared / employer-confirmed / verified). Decide: reuse verbatim, or add display aliases so
  worker-facing copy reads "self-declared / employer-confirmed / verified". W3 already constrains who may
  write the `CscsVerified` state.

**Genuinely new (no entity/table today) — all gated on Q1/Q2**

- Personal details (DOB, NI, address) and emergency contacts — today these exist only as *un-persisted*
  induction step labels (WP-5).
- Passport ownership/claim + subscription/lapse lifecycle (WP-1/2/12, §8, W2).
- Sharing: selected-credential, recipient-no-account, time-limited, revocable grants + open tracking
  (WP-7/8, W5/W7); modelled as specific revocable grants from day one (§5.2 → future PRD §8.6).
- Audit trail (WP-14), export (WP-10), delete (WP-11), account recovery (WP-13).
- Reminder engine (WP-6) — needs an **SMS provider** (only email/Resend exists today; SMS is outstanding).
- Stripe consumer checkout + webhook + subscription (§8) — none exists; **GoCardless is the exact template**
  (`docs/plan-and-scope.md` → *Admin area & GoCardless billing*).
- QR / wallet pass + work history (WP-15/16, P1).

## Gating pre-work (blocks the build, not this plan)

From PRD §9 / §10 / §7. Must land before schema/delivery work starts.

| Gate | Blocks | Source |
|---|---|---|
| **Q1** data controller determination | the data model / schema | §9.1 |
| **Q2** collision rule (passport holder later added as operative → employer sees nothing without a WP-7 share) | the data model / schema | §9.1 |
| **Q3** app vs mobile-web (default: mobile-web first) | delivery approach | §9.1 |
| **Q4** induction pre-fill vs write-through (default: pre-fill) | WP-9 design | §9.1 |
| **DPIA** (identity + qualifications + emergency contacts, shared to third parties) | any storage go-live | §9.3 |
| **Consumer contract terms** (cancellation rights, unfair-terms) | checkout go-live | §9.3 |
| **CSCS §7.3 letter** (may a lawful check persist in a worker-held record; Licensee Partner Policy) | **WP-17 only** | §7.3 |

## Phased roadmap

Slots alongside existing tracks: backend numeric phases continue after the Forms Library (**Phase 26+**);
marketing-site work continues on the **W-track (W9+)**. Structure mirrors the Admin/GoCardless billing
sub-phases (a self-contained product track), not the SF/MC feature phases. Each phase is independently
testable and must not break existing functionality.

| Phase | Scope | Key WP-IDs / rules | Depends on |
|---|---|---|---|
| **WP-A — Foundation data** | New Person-scoped entities: personal details, emergency contacts; passport ownership/claim; account recovery. Repos (both backends) + product-DB migration. Confirm WP-4 status mapping. | WP-1,2,5,13; SF-1/2 | Q1, Q2, DPIA |
| **WP-B — Worker passport (mobile-web)** | Self-registration without employer, keyed to Person by phone; credential management (reuse `QualificationService`/SF-5); personal details + emergency contacts UI; export (WP-10) + delete (WP-11) with plain-words consequences. | WP-1,2,3,5,10,11; W1,W6,W7 | WP-A, Q3 |
| **WP-C — Sharing & consent** | Share aggregate (selected creds, named recipient, no account, time-limited, revocable); recipient viewer showing status unaltered; share list + open-tracking + revoke (plain words); passport audit trail. | WP-7,8,14; W4,W5,W7 | WP-A |
| **WP-D — Expiry & reminders** | Reminder engine over computed expiry; worker-configurable timing + default; nominated-notify (opt-in). | WP-6,18; G3 | WP-A, **SMS provider** |
| **WP-E — Commercial (Stripe)** | Stripe consumer checkout + signed webhook + subscription (commercial plane, GoCardless template); replace the looping marketing CTA with a real checkout (W-track); **lapse → read-only, never hidden/deleted** (W2); consumer terms at point of sale (W7); price from config, never hard-coded (§8). | WP-12; W2,W7; §8 | Consumer terms; W-track |
| **WP-F — Induction pre-fill** | Passport holder starting an induction on a MC customer site pre-fills details / contacts / quals; worker confirms before accepted (pre-fill, not write-through). | WP-9; Q4 | Main-contractor product live; WP-A |
| **WP-G — Verified write-back** | Interface only; passport never initiates verification; verified state written only from a MC check under the customer's licence. **Ships nothing** until CSCS §7.3 answered. | WP-17; W3,W4; §7 | CSCS §7.3 |
| **WP-H — Digital presentation (P1)** | QR / wallet pass constrained by W4; work history from own attendance. | WP-15,16; W4 | WP-A |
| **WP-Admin — Operational oversight** | The admin Worker Passports section (below). Metrics / states only. | G3,G4; §8.1; W5 | reads WP-A/C/D/E state as it lands |

## The admin "Worker Passports" section (operational metrics only)

**Purpose.** Give the platform operator a comprehensive view of the passport product's *commercial and
operational health* — the numbers §8.1 says decide the product (renewal, reminder/share engagement as the
early indicators, support load) — **without** exposing any worker's passport content. No names, DOB, NI,
addresses, emergency-contact details or credential images appear here. This is the W5-safe reading of "track
them comprehensively".

Built as the standard 7-layer admin vertical slice (mirrors Billing/Payouts/Leads).

**Nav** — one line in `ShellChrome.AdminNavItems` (`src/Tedwren.Client/Services/ShellChrome.cs`):
`new("Worker Passports", Icons.Material.Outlined.Badge, "/admin/worker-passports")`.

**Security** — API group `/api/admin/worker-passports` gated `.RequireAuthorization("PlatformAdmin")`; pages
wrapped in `<AdminGuard>`. Passport-content reads are simply not offered by the endpoint surface.

**List page** `AdminWorkerPassports.razor` (`/admin/worker-passports`) — KPI row + `DataTable`:

- KPI cards (reuse `DashboardCard`): total passports, active-paid, lapsed (read-only), credentials expiring
  ≤60d, credentials expired, shares opened (30d), reminders opened (30d, G3/G4), renewals due this month,
  open account-recovery requests.
- Table columns: masked owner reference (stable passport id / last-3 of phone — **not** full number),
  Created, Credentials (count), Expiring / Expired (counts), Shares (active / opened), Subscription
  (`Active` / `Lapsed` / `None`) via `StatusPill`, Renewal due, Last active. Filters on subscription state,
  has-expiring, has-shares.

**Detail page** `AdminWorkerPassportDetail.razor` (`/admin/worker-passports/{Id:guid}`) — reuse the
`AdminLeadDetail` template (`DetailHeader` + `MudTabs` + `KeyValueList` + `ActivityFeed`):

- Header: masked owner ref + subscription `StatusPill`. No personal identifiers.
- **Overview tab**: operational `KeyValueList` — created, subscription state + renewal date, credential
  counts by verification state (`ReadUnchecked`/`CustomerChecked`/`CscsVerified`) and by currency
  (Valid/ExpiringSoon/Expired via `GetStatus`), reminder-config summary, last activity. No content fields.
- **Activity/Audit tab**: `ActivityFeed` of passport-level events from the WP-14 audit trail — created,
  subscription changes, shares created/opened/revoked, reminders sent/opened, recovery events — **metadata
  only** (actor / timestamp / reason), redacted of any shared content.
- **Billing tab**: link to the commercial Stripe subscription record (reuse billing DTO patterns).

**Data sources.** Metrics are *derived*, needing no passport-content schema: credential counts + expiry from
existing `Person` + `QualificationCard.GetStatus()`; subscription/lapse + renewal from the commercial-plane
Stripe subscription (WP-E); share/reminder/recovery activity from WP-C/WP-D metadata. The earliest
independently-shippable slice (passport counts + credential-expiry metrics from existing
`Person`/`QualificationCard`) needs **no new schema** and could ship without Q1/Q2 — noted for sequencing.

**Files this section will add when built** (not built by this plan): `IWorkerPassportAdminService.cs` +
`Contracts/WorkerPassports/*Dtos.cs` (Abstractions); `WorkerPassportAdminService.cs` (Application) + DI; read
repository/queries (DataAccess, both backends); `WorkerPassportAdminEndpoints.cs` + `Program.cs` wiring
(Api); `ApiWorkerPassportAdminService.cs` + client DI; the two pages + nav line (Client); service + API +
repository tests. All metrics-only; no endpoint returns passport content.

## Verification (when each phase is built)

- **Build/test the whole solution** before any PR: `dotnet build Tedwren.sln`, `dotnet test Tedwren.sln`;
  resolve all compile errors, investigate warnings. Each phase adds its own Application + API + repository
  (skip-guarded) tests, following the existing `*ServiceTests` / `*ApiTests` / `*RepositoryTests` patterns.
- **Secure-by-default check** on every new endpoint: worker-facing passport routes reachable by the worker's
  own auth/link; admin routes `PlatformAdmin`-gated; the marketing checkout's public routes explicitly
  `AllowAnonymous`, everything else authenticated (per `CLAUDE.md`).
- **Rule checks** are acceptance criteria, not afterthoughts: W1 (no install), W2 (lapse = read-only, never
  deleted), W3/W4 (verification wording), W5 (no admin override — verified by the admin section exposing
  metrics only), W7 (informed consent copy at point of sale/share/revoke).
- **Human gate:** WP-A+ code does not begin until Q1–Q4, the DPIA, consumer contract terms, and (for WP-G)
  the CSCS §7.3 letter are signed off.
