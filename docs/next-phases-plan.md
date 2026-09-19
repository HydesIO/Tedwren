# Tedwren — Next Phases Plan & Scope of Works

> **Status:** proposed for review (not yet started). This document scopes the *next* stage of
> development. It sits alongside [`plan-and-scope.md`](plan-and-scope.md) (the original phased
> roadmap) and the per-track plans (`forms-library-plan.md`, `worker-passport-plan.md`).
> **Source of truth remains PRD v6.4** (`docs/TedwrenPRDv6_4.docx`); this plan references PRD IDs
> (SF-/SUB-/MC-/R-) and PRD §8 "Phases after the MVPs", it does not restate them.

## 1. Purpose

Everything the original plan called "MVP" has shipped. This document answers the question *what
next, and in what order*, and its bias is the one the request set: **make the platform more robust**
— trustworthy and deployable first, then expand into the next commercial module the PRD sequences.
It is a plan to review and decide from; no code is written against it until it is agreed.

## 2. Where the platform actually is (grounded, September 2026)

A read of the code — not just the tracker — puts the build well beyond the MVP line:

| Delivered | Evidence |
|---|---|
| **Shared foundation** (SF): people/companies/engagements, qualification cards + competency, expiry engine, sites + geofence + dispersed schemes, sign-in/attendance, roles/audit/entitlements | `Tedwren.Domain` (51 entities), `Application/{Qualifications,Expiry,Sites,Attendance,Audit,Entitlements}` |
| **Subcontractor MVP**: timesheets (stateful, correctable, approvable), compliance pack (link + PDF + ZIP, passcode, tracking, revoke) | `Application/{Timesheets,CompliancePacks}`, `/api/timesheets`, `/api/packs` |
| **Main contractor MVP**: digital induction (phone, server-scored quiz, consent), five-check site-entry decision (fail-closed, decision record, override, muster) | `Application/{Inductions,SiteEntry,Decisions}` |
| **First commercial module — Forms Library** (PRD-Phase 2 checklist/inspection engine): per-tenant builder, versioned templates, submissions, branded PDF, assignment, induction embedding, recurring reminders, server-side entitlement gate | `Application/Forms`, `/api/forms`, `Client/Pages/Forms` (plan Phases 19–25) |
| **Platform operations plane**: admin console, GoCardless mandates/payments/webhooks/payouts, leads, affiliates + e-sign, launch list, demo-data service | `Application/{Billing,Leads,Affiliates,LaunchList,DemoData}`, `/api/admin`, separate `Tedwren.DataAccess.Commercial` |
| **Marketing site** (`Tedwren.Web`), **JWT auth** (secure-by-default fallback policy, auditor read-only), **Resend email**, **R12 job heartbeat**, **MudDialog design standard**, a **27-issue UAT remediation** pass | across the solution; ~531 tests (Application 225, Api 131, Web 81, Domain 45, Client 27, DataAccess 4 + 18 skip-guarded) |

**But it is a feature-complete MVP, not a launched product.** The gaps below are what stand between
"it demos" and "a paying customer can trust it".

### 2.1 The robustness gaps the audits surfaced

- **Security — committed secrets (P0).** A *live* SQL Server credential is committed in
  `src/Tedwren.Api/appsettings.json` (already tracked for rotation in `TODO.md`); the JWT signing key
  is a committed default (`JwtOptions.cs`), so Administrator tokens are forgeable by anyone with
  source access; the seeded platform-admin accounts (`AdminUserSeeder.cs`) share a committed default
  password; and an `Auth:TestBypass` scheme authenticates every request as Administrator if the flag
  is ever true in a deployed environment.
- **Silent delivery gaps.** SMS is a stub (`OutboxSmsSender`) with no real provider, so worker
  qualification-expiry warnings (SF-9) are dropped to an outbox in every environment. The R12
  heartbeat monitor (`JobHeartbeatMonitor`) watches only 2 of the 5 scheduled jobs and runs *inside*
  the scheduler loop it is meant to watch — no independent watchdog.
- **Public attack surface.** The token/kiosk anonymous groups (induction, site-entry, onboarding,
  packs) are **not** rate-limited (only signups/leads/affiliate-agreements are), and two anonymous
  **file-upload** endpoints (trade document, induction form) accept binary from unauthenticated
  callers.
- **PRD data-model debt.** The **Asset / Plant register** entity the PRD said should exist "from the
  MVP so that this is an addition rather than a rewrite" (§8.2) was never stubbed — adding it later is
  now a genuine data-model addition.
- **Storage strategy.** All binary assets (card photos, avatars, form-submission files) are stored as
  BLOB rows in SQL via `IImageStore` / `FormSubmissionFiles`. Correct and R9-safe today, but a
  backup/bloat/scaling consideration with no object-storage option behind the interface.
- **Doc drift.** `README.md` still describes the repo as a "UI/UX Base Project" with backend "out of
  scope" and references the removed `SampleData` project.

## 3. Strategic recommendation

Follow the PRD's own §11 sequencing, read through the "more robust" lens:

1. **Harden to launch-ready first (Track A).** The security items are not optional polish — a
   forgeable admin token and a committed live DB credential are the single largest risks in the
   product. This track is the literal meaning of "more robust" and it gates the first real customer.
2. **Start the one thing with external lead time now, in parallel (Track B).** PRD §11 names a hard
   external dependency: the **CSCS Smart Check** commercial agreement, "the longest lead time on the
   project… start it immediately even though [it] is built after both MVPs." The build is small (the
   data-model seam already exists); the *agreement* is the long pole and is a business action to begin
   today.
3. **Then the largest revenue expansion (Track C): Health, Safety & Compliance (PRD-Phase 2).** Its
   hardest component — the configurable checklist/inspection engine — is already built (Forms
   Library). The rest is high-value, well-understood, and reuses patterns already in the codebase
   (tokenised submit-and-review, the expiry engine, signature capture). Sold main-contractor-first,
   sell-before-build.

Everything after that (QA, Pay & cost, Verified identity, Cross-company sharing, Integrations, Worker
Passport) is deliberately deferred with reasons in §7.

> **The strategic fork, stated plainly for your decision:** if the near-term goal is *first paying
> customer / pilot*, do Track A end-to-end and stop — Track C is not needed to sell either MVP. If the
> goal is *widen the offer to main contractors*, Track A's P0 security subset is the floor and Track C
> is the growth. My recommendation is A (P0 + P1) → C, with B's *agreement* kicked off on day one. Tell
> me which goal is primary and I'll collapse the plan to it.

---

## Track A — Launch Readiness (make what exists trustworthy)

This completes the original **Phase 18** (hardening + PG gate) and folds in the security findings the
audits surfaced. Each workstream is independently shippable and testable. Priority in brackets.

### LR-1 — Secrets & auth hardening **(P0, security)**
- Remove the SQL Server credential from `appsettings.json`, source it from environment/secret store,
  and **rotate** the password on the DB server. *(Raise: the credential is in git history; rotation is
  the real fix — history scrubbing is a secondary, optional decision.)*
- Make the JWT signing key a **required** environment value; refuse to start in Production if it is the
  committed default or unset. Tokens signed with the old key must stop validating after rotation.
- Remove the committed seeded-admin password; require it via secret and force a change on first login;
  never seed the three named platform admins with a shared default.
- Guarantee `Auth:TestBypass` can only be true under the test host (Environment-guarded; Production
  startup refuses it).
- **Testable:** Production startup rejects insecure config; a token forged with the default key is
  rejected; an integration test asserts TestBypass is off outside the test host.

### LR-2 — Notification delivery & job resilience **(P0/P1, R12)**
- Wire a **real `ISmsSender`** (e.g. Twilio/Vonage) behind the existing interface, conditionally
  registered exactly as `ResendEmailSender` is (outbox stays the dev default). Closes the silent drop
  of SF-9 worker warnings and unblocks onboarding-link-by-SMS (the highest-friction subcontractor
  moment, PRD Q7).
- Broaden `JobHeartbeatMonitor` to **all** jobs (ExpiryScan, WeeklyDigest, FormReminder, OvernightCheck,
  BillingReconciliation), add an **independent watchdog** outside the scheduler loop, and ensure the ops
  alert uses a channel that actually delivers in Production (not the outbox).
- **Testable:** SMS path unit-tested; heartbeat asserts coverage of every `JobName`; watchdog fires
  when the scheduler is disabled/dead.

### LR-3 — Public attack-surface hardening **(P0/P1, security)**
- Apply the existing `"public"` rate-limit policy to the currently-unthrottled anonymous groups
  (`/api/inductions`, `/api/site-entry`, `/api/onboarding`, `/api/packs`).
- Harden the two anonymous **file-upload** endpoints (trade document, induction form): size caps,
  content-type allow-list, malware posture, per-token quota.
- Commission the **independent security review of the public pack link** (PRD §11 — "the only public
  route to personal data… deserves disproportionate attention"), working the outstanding items already
  listed in `docs/security-pack-link-review.md` (sender-alerting, message unification, HSTS, PII
  minimisation, single-use links). *External dependency — schedule the reviewer now.*
- **Testable:** rate-limit tests per anonymous group; upload-guard unit/integration tests.

### LR-4 — Storage strategy **(P1)** *(delivered)*
- Introduce an object-storage `IImageStore` implementation (Azure Blob / S3-compatible) behind the
  existing interface, keeping DB-BLOB as the fallback/default; UK region only (R13). Removes the DB
  bloat/backup pressure of card photos, avatars and form files living as SQL rows.
- **Testable:** the blob implementation passes the same `IImageStore` contract tests as the DB one.
- **✅ Delivered:** `S3ImageStore` (AWSSDK.S3) behind the interface, selected by `Storage:Provider=S3`
  (default stays `Database`); iDrive e2 supported via `ServiceUrl` + path-style. Fail-fast on missing
  credentials; non-GUID references rejected on read (R9); `docs/object-storage.md` covers setup.
  *Follow-up:* a one-off backfill of existing DB-stored images into the bucket if desired.

### LR-5 — Load, accessibility & backup **(P1, partly external)**
- Sustained **load/soak** test against R14 (<3 s site-entry decision); extend the existing
  `SiteEntryLatencyTests` single-shot assertion into a load profile.
- **Accessibility** audit (WCAG 2.2 AA) across the console; fix findings.
- **Backup/restore** rehearsal for both engines.

### LR-6 — Governance & finish-the-edges **(P2, quick wins)**
- Rewrite the stale `README.md` to match reality; re-sync any drifted docs.
- Persist the remaining demo write-actions flagged in `TODO.md` (operative edit, site edit, general
  settings, permits save) — each a small dedicated write endpoint.
- Complete the **Permit lifecycle**: `PermitStatus` currently has only `Draft`/`Issued`; add
  approve/close/expire states so permits are genuinely "issued, approved, time-bound, closed and
  audited" (PRD §8.2) — a small, self-contained upgrade of an already-built feature.

---

## Track B — CSCS live verification (PRD-Phase 1)

PRD §8.1 + §11. **Ships after both MVPs, but its agreement starts on day one** because it has the
longest external lead time.

- **Commercial (now, not code):** begin the **CSCS Smart Check commercial agreement**. Flagged here so
  it is on the critical path from the start; nothing in Tracks A or C waits on it.
- **Build (small, high-leverage):** the data-model seam already exists — `CardVerificationState.CscsVerified`
  and `QualificationType.IsCscsVerifiable` are modelled but never produced. Add an
  `ICscsVerificationService` behind that seam with an **`UnconfiguredCscsClient`** no-op (mirroring
  `UnconfiguredGoCardlessClient`), so induction and the gate can consume it now. Honour §8.1:
  expired card **blocks** the induction; unrecognised card lets the induction continue but **blocks
  entry** until a manager checks it; if CSCS is unreachable, **fall back to human check — never block
  the induction**; a live-verified card is labelled differently from a customer-checked one; the same
  card against two people is **flagged, neither named** (PRD Q11).
- **Entitlement:** a new paid add-on module, server-side gated like `forms`.
- **Testable:** the seam is exercised with a fake verifier; unreachable → human-fallback proven; the
  entitlement gate returns 200/403/200.

---

## Track C — Health, Safety & Compliance module (PRD-Phase 2)

"The largest single revenue expansion and the most requested capability" (PRD §8). Sold main-contractor
first. **The checklist/inspection engine is already built** (Forms Library); this track adds the
surrounding capabilities, each reusing an existing pattern. Sequenced by value and dependency;
sell-before-build applies to each.

### HSE-1 — Plant & equipment register *(closes the PRD data-model debt)*
- New **`Asset`** entity (owner, location, certification, inspection history) — the entity the PRD said
  should have existed from the MVP. Reuse the **expiry-warning engine** for certification expiry (same
  schedule as a card) and the **Forms engine** for inspections. Stub it deliberately so later HSE items
  attach cleanly.

### HSE-2 — RAMS submission & approval
- Reuse the **trade-onboarding pattern already built** (`TradeInvite`: submit-from-tokenised-link,
  review queue, approve/reject/return-with-reason, versioned). RAMS is the same shape: contractor
  submits from an emailed link with no account and gets a reference; the manager approves/rejects/asks
  for changes (rejection needs a written reason); resubmission creates a new version, earlier versions
  intact; **until approved, that contractor's workers can't start on that site**; a "> 48 h awaiting
  review" view. Fold the **RAMS check into the site-entry decision** when the module is held — this is
  what §8.2 means by "completes the site-entry decision".

### HSE-3 — Document distribution & acknowledgement *(delivered — core; targeting/schedule deferred)*
- Upload any document and distribute to individuals / a site / a trade / a skill group / the whole
  company in one action; a **digital signature records receipt and acceptance**; re-acknowledgement on a
  schedule or at next clock-in; a **completion matrix** (who has and hasn't signed) that becomes a
  further proof point in the compliance pack. Reuses `CompanyDocument`, the induction signature capture,
  and the notification engine.
- **Delivered:** `DocumentDistribution` + `DocumentAcknowledgement` (append-only receipt), the
  `/api/documents` group (`hse`-gated, fails closed), the **Documents** page with the distribute dialog
  and the **completion matrix** + per-recipient sign action, and unit/API tests.
- **Deferred (follow-ups):** structured targeting (resolve a site / trade / skill group / whole-company
  roster into recipients — `PersonId` column provisioned); the anonymous **emailed acknowledgement link**
  (token pattern) so a recipient signs without a login; scheduled/at-clock-in **re-acknowledgement**; and
  folding the matrix into the compliance pack as a proof point.

### HSE-4 — Near-miss / hazard reporting + accident/incident (RIDDOR) *(delivered — core; geolocation/notify deferred)*
- Worker reports from their phone with photo + location (reuse SF-14 geolocation + `IImageStore`);
  responsible manager notified immediately; categorise, assign, close; leading-indicator statistics.
- Structured **accident/incident** record with investigation fields, actions, close-out, a
  **RIDDOR-reportable flag** and export.
- **Delivered:** `HazardReport` + `IncidentReport` (two SRP entities), the `hse`-gated `/api/safety`
  group (`/hazards*` report→assign→close + leading-indicator stats; `/incidents*` report→investigate
  →close with the **RIDDOR flag/category**), the tabbed **Safety Events** page (hazard tiles + report/
  triage dialogs; incident record + investigation dialog), and unit/API tests.
- **Deferred (follow-ups):** on-device geolocation capture (lat/lng columns provisioned); immediate
  manager notification on report (reuse the notification engine); resolving reporter/assignee from the
  people roster; and RIDDOR export.

### HSE-5 — Library versioning, unified evidence export, HAVs & carbon *(HAVs delivered; rest queued/blocked)*
- Add **versioning/supersede** to `CompanyDocument` (mirror `QualificationCard`'s supersede chain) →
  the "company file library, always at the current version" that supersedes MC-27. **✅ Delivered:**
  `CompanyDocument` supersede chain (`Version`/`Supersedes`/`SupersededBy`, retained history), the company
  detail lists current versions only, `/api/organisation/.../documents/{id}/versions` (history + new
  version), and the Documents tab's **New version** + **History** actions. *Follow-up:* per-version file
  upload; a `FamilyId` to simplify chain queries.
- **Unified compliance evidence export** spanning inductions + acknowledgements + inspections + permits +
  competency (extend `PackComposer` / the `Export` writers) — the ISO 45001 / project-audit export.
  **✅ Delivered:** `EvidenceExportService` assembles permits, RAMS, plant, hazards, incidents, HAVs and
  document acknowledgements into a ZIP (one CSV per section + manifest, via the shared `CsvWriter`); the
  `hse`-gated `/api/evidence` (`/summary` + `/export`) and an **Evidence Export** page. *Follow-up:* add
  competency/inductions/inspection sections, an Excel/PDF rendering, and a site/date filter.
- **Hand-arm vibration (HAVs)** monitoring (exposure vs HSE action/limit values, proactive alerts) — a
  statutory duty and a standalone reason to buy the module. **✅ Delivered:** `HavsExposureRecord` +
  `HavsCalculator` (HSE A(8) methodology: EAV 2.5 m/s²/100 pts, ELV 5.0 m/s²/400 pts), the `hse`-gated
  `/api/havs` group, the **Vibration (HAVs)** page (dynamic per-tool entry + band-aware result +
  breakdown), and unit/API tests. *Follow-up:* proactive over-EAV/ELV alerts via the notification engine.
- **Social value / carbon** reporting — **blocked:** depends on the MC-26 travel/vehicle capture, which
  is not built. Do not build until MC-26 exists.

---

## 6. Indicative effort & suggested sequencing

T-shirt sizes are indicative only, for sequencing — not a fixed estimate (PRD §11 asks for a separate
formal estimate per component, which this plan enables but does not replace).

| Phase | Size | External dep? | Note |
|---|---|---|---|
| LR-1 Secrets & auth | S–M | — | Do first. Highest risk, mostly config + startup guards. |
| LR-2 SMS & job resilience | M | SMS provider account | Closes a silent promise-vs-reality gap. |
| LR-3 Attack surface | M | security reviewer | Reviewer is the long pole. |
| LR-4 Storage | M | blob account | Behind existing interface. |
| LR-5 Load/a11y/backup | M–L | — | Partly manual/rehearsal. |
| LR-6 Governance edges | S | — | Quick wins; permit lifecycle. |
| B CSCS seam | S (build) | **CSCS agreement (long)** | Start the agreement now. |
| HSE-1 Asset register | M | — | Pays down PRD debt. |
| HSE-2 RAMS | M | — | Reuses trade-onboarding. |
| HSE-3 Doc distribution | M | — | Reuses signature + notify. |
| HSE-4 Near-miss/incident | M–L | — | New workflow entities. |
| HSE-5 Versioning/export/HAVs/carbon | L | MC-26 data (carbon) | Bundle; carbon gated on travel data. |

Recommended order: **LR-1 → LR-3 → LR-2 → (LR-4, LR-5, LR-6 as capacity allows)**, with **B's
agreement kicked off on day one**, then **HSE-1 → HSE-2 → HSE-3 → HSE-4 → HSE-5**.

---

## 7. Deliberately deferred (with reasons — raised, not dropped)

- **PRD-Phase 3 — Quality Assurance** (installation/test sheets, snagging, plot tracker, handover):
  same form engine as Phase 2, *different buyer* (§8.3). Follows HSE.
- **PRD-Phase 4 — Pay & cost** (pay-rules engine, leave/absence, labour cost, Sage/Xero/CIS export,
  expenses): sits on data the subcontractor MVP already holds; timesheets are the attach point but carry
  **hours only** today (`TimesheetEntry` has no rate/cost dimension). Note the seam now; build after QA.
- **PRD-Phase 5 — Verified identity** (biometrics): deliberately last for the data-protection burden.
  **Blocking decision needed early:** PRD Q14 — the route to verified identity given R1 (no app) —
  "may affect how worker identity is modelled from the beginning… we would like your view before Phase 2
  starts." Surface this to Leigh/James before HSE architecture is fixed.
- **PRD-Phase 6 — Cross-company sharing:** needs market density; the un-retrofittable half (MC-20
  consent) is already captured, so nothing is foreclosed.
- **PRD-Phase 7 — Integrations & report library:** follows everything. *Watch Q19 — Procore may get
  pulled forward if the first five main-contractor conversations demand it.*
- **Worker Passport (third product):** planning-complete (`worker-passport-plan.md`) but **gated on
  legal** (PRD Q1 data controller, Q2 identity collision, a DPIA, consumer contract terms) — held for
  Leigh's sign-off, not an engineering decision.
- **PostgreSQL parity gate:** deferred at the product owner's direction. **SQL Server is the launch
  engine**; the Postgres migration scripts are kept current, but standing up a Postgres test suite and
  running the parity gate is out of scope for this stage — revisit only if a customer requires Postgres.

## 8. Open decisions to put to Leigh & James (PRD §10.1)

Needed to shape (not just detail) the build:

- **Q14** — identity/app route for Phase 5 (affects identity modelling *now*).
- **Q18 / §9** — is a module priced per site or per operative? **Resolved: the HSE module is billed per active
  site** (product owner). Which-meter/which-band stays **configuration** (PRD §9 — not hard-coded); the HSE
  features ship as an on/off module and the per-site billing is a commercial-config concern for the billing layer.
- **Q21 / Q22** — the default qualification and insurance/accreditation libraries (needs a construction
  practitioner, not a developer).
- **Q3** — does the cross-company sharing surface belong in the MC MVP after all (PRD instinct: no).

## 9. Cross-cutting standards & verification

Unchanged from `CLAUDE.md` and `plan-and-scope.md` §"Cross-cutting engineering standards": SRP,
per-class/per-method summary comments, async, reuse-first over the existing kit, Dapper dual-engine,
JSON settings, tokens-only styling, the MudDialog standard, and secure-by-default endpoints. Every
phase is **independently testable**, must **not break a completed phase**, updates `TODO.md`, and
references PRD IDs rather than restating them. New paid capabilities are **server-side, fail-closed**
entitlement-gated (Q2) exactly as the Forms module's `ModuleGate` already is.
