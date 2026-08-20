# Subcontractor vs Main Contractor — portal differentiation

> Status: analysis + agreed remediation. Source of truth is `docs/TedwrenPRDv6_4.docx` (PRD v6.4);
> this document references PRD IDs (SF-/SUB-/MC-/R-) rather than reproducing prose. Delivery plan is
> in `docs/plan-and-scope.md`; the working checklist is `TODO.md`.

## Why this exists

Testing feedback: logging in as a **subcontractor** and logging in as a **main contractor**
produces a visually and functionally identical client portal. Per the PRD these are **two MVP
products, one platform… sold separately** (§2) — not two roles on one portal — so they must not
look the same:

- **Subcontractor product** — time & attendance + the compliance pack, for *a compliance/payroll
  admin at a desk*. Console spec **SUB-24**: "dense tables, bulk actions and export".
- **Main contractor product** — workforce management + the site-entry decision, for *a site
  manager glancing at a phone in a cabin*. Console spec **MC-23**: "headcount, exceptions, what
  needs attention today".

## Diagnosis — why the two portals are identical today

The product choice is captured once, before login, then discarded. The root-cause chain:

1. **Company type is write-only.** The onboarding wizard has a real `OnboardingOrgType` enum
   (`Subcontractor` / `MainContractor`, `src/Tedwren.Client/Pages/Onboarding/OnboardingModel.cs`).
   On submit it is flattened to a **free-text string** and stored in `Company.Type`
   (`src/Tedwren.Domain/Entities/Company.cs`, *"Left open per the PRD"*). That is the last time the
   distinction affects anything.

2. **The session identity has no product discriminator.** `CurrentUserDto`
   (`src/Tedwren.Abstractions/Contracts/Identity/IdentityDtos.cs`) carries `Name`, `Role` (the
   `AccessRole` permission role — Administrator/SiteManager/… — **not** the product), `CompanyId`
   and `IsPlatformAdmin`. Nothing carries the product, and `AuthState` / `/api/me` never fetch it.
   The console literally cannot tell which product it is rendering.

3. **Navigation is gated by *purchased modules*, not by product** — which is the PRD-correct
   mechanism (**SF-22**: "navigation shows only what the customer has bought; an unpurchased module
   is not visible as a locked door"). `MainLayout.OnInitializedAsync` → `GatedNavItemsAsync` filters
   `ShellChrome.NavItems` through `GatedRouteModules` against the company's entitlements. The only
   other branch is `IsPlatformAdmin` (admin area vs tenant console). **There is no branch on product
   type.**

4. **…but every company gets the same default module bundle.** `ModuleCatalog`
   (`src/Tedwren.Application/Entitlements/ModuleCatalog.cs`) is a single product-agnostic list with
   fixed defaults (`workforce`, `compliance`, `inductions`, `permits`, `reports` default **on**;
   `time`, `forms`, `integrations` default **off**). Onboarding **never grants a product-specific
   entitlement set**, so a subcontractor and a main contractor receive a byte-identical default
   bundle → identical sidebar → identical UI. `inductions` even defaults **on** for everyone,
   contradicting the §6.1 non-goal + **SUB-11** ("Induction for subcontractors — not building").

5. **No per-product console/page divergence exists at all.** Every page under
   `src/Tedwren.Client/Pages/` is shared; `Dashboard.razor` does not reference product type. So the
   **SUB-24 vs MC-23** dashboard split and the **R18** sign-in wording split are simply not built.

**Bottom line:** the plumbing for *navigation* differentiation (SF-22 entitlements) already works;
what is missing is (a) a durable product discriminator carried from onboarding → company → session,
(b) product-specific entitlement bundles granted at onboarding, and (c) product-aware rendering on
the dashboard, the site-gate/sign-in surface, and a few pages.

## PRD → gap map (what "different" should mean)

| PRD | Intended difference | Status today |
|---|---|---|
| §2, §11 | Two products sold separately; different pricing meters | Single portal, no product concept post-login |
| SF-22 | Nav shows only what's bought | Works, but the default bundle is identical for both products |
| SUB-24 vs MC-23 | Admin desk console (dense tables) **vs** phone/cabin exceptions dashboard | One shared `Dashboard.razor`, no branch |
| §6.1, SUB-11 | Induction **not** built for subcontractors | `inductions` defaults on for everyone |
| SUB-13…26 vs MC-19 | Subcontractor **sends** packs; main contractor **receives** packs | Same "Compliance Packs" surface for both |
| MC-1…7, MC-15, MC-8 | Induction engine + site-entry decision are MC-only | Shown to both |
| R18 / SUB-12 vs MC-8/9 | Subcontractor sign-in says "recorded / site-ready", **never** "permitted/denied"; MC makes a permit/block decision | No wording divergence in Site Gate / sign-in |
| SUB-8 vs MC-24 | Timesheet per-operative-per-week (admin approval) **vs** per-site-per-company (QS reconciliation) | Single time/attendance surface |
| MC-21, MC-22 | Site-scoped managers; PII withheld from commercial roles | Not product-aware |

## Agreed remediation (full divergence)

Keep the PRD's mechanism — product identity is primarily expressed through the **entitlement
bundle (SF-22)** — promoted to a first-class discriminator so the console can also branch layout
and wording where a "module" is the wrong abstraction (dashboard shape, R18 wording). Reuse the
existing entitlement/gating machinery rather than inventing a parallel one. Staged so each phase is
independently testable and never breaks a completed phase.

### Resolved decisions (confirmed with the product owner)

1. **Module → product bundle: strict PRD split.**
   - **Subcontractor** default bundle: `workforce`, `compliance` (packs **send**), `time` (Time &
     Attendance), `reports`. **No** `inductions`, **no** site-entry decision (§6.1, SUB-11).
   - **Main contractor** default bundle: `workforce`, `compliance` (packs **receive**, MC-19),
     `inductions`, site-entry decision, `reports`. Pack-sending is not a core surface.
   - `permits`, `forms`, `integrations` stay **off** by default for both, purchasable by either via
     the existing `AdminCompanyModules` override.
   - "Compliance Packs" therefore renders as **send** for subcontractor and **receive** for main
     contractor (product-aware content on the compliance surface).
2. **`OrgType` is a new typed enum stored *beside* the free-text `Company.Type`.** The enum is the
   reliable product discriminator; the free-text `Type` stays deliberately open per the PRD. No
   destructive migration of the existing field — an additive `OrgType` column + backfill.
3. **One product per company** — a company is either a subcontractor or a main contractor; product
   is a single enum on the session/company, not entitlement-derived, and there is no product
   switcher. (A cosmetic `PlatformSelector` dropdown was previously removed for driving no routing
   or data; this replaces that idea with real, product-driven differentiation.)

### Phases

- **Phase A — product discriminator.** `OrgType` enum in `Tedwren.Domain`, carried on `Company`
  beside `Type`; mapped from `OnboardingOrgType` at wizard submit; surfaced onto `CurrentUserDto`
  and `/api/me` (computed server-side like `IsPlatformAdmin`, never trusted from the client) and
  exposed via `ITenantState`; additive EF migration + backfill from `Type`.
- **Phase B — product entitlement bundles.** A product→module bundle map (strict split above)
  granted at onboarding submit via `IEntitlementService`; this drives the nav split through the
  existing `GatedNavItemsAsync` with no change to `MainLayout`'s logic. Compliance Packs surface
  made send-vs-receive aware. `AdminCompanyModules` override still applies.
- **Phase C — product-aware dashboards.** Branch `Dashboard.razor` on `OrgType`: subcontractor
  dense admin view (SUB-24 — expiry digest SUB-5, timesheets awaiting approval SUB-8/9, packs,
  labour reporting); main contractor phone/cabin exceptions view (MC-23 — who's on site now MC-12,
  competency cover MC-13, blocked-worker exceptions MC-9, muster shortcut MC-14). Reuse the existing
  MudBlazor kit; `tokens.css` only.
- **Phase D — R18 wording + per-product pages.** Site Gate / sign-in result reads "recorded /
  site-ready" for subcontractor (R18, SUB-12) and a permit/block decision for main contractor
  (MC-8/9); timesheet audience divergence (SUB-8 vs MC-24); verify no MC-only route leaks into the
  subcontractor experience.

## Verification

- `dotnet build Tedwren.sln` and `dotnet test Tedwren.sln` clean.
- Onboarding as `Subcontractor` grants the subcontractor bundle (no `inductions`); as
  `MainContractor` grants the MC bundle (`EntitlementServiceTests` / API tests extended).
- Run the client, sign in as each demo product (`demo.sub@tedwren.example` /
  `demo.main@tedwren.example`): different sidebars; subcontractor has no Inductions / no site-entry
  decision; main contractor has induction + site-gate decision; dashboards differ (SUB-24 vs
  MC-23); Site-Gate wording differs (no "permitted/denied" for subcontractor).
- No regression to the platform-admin (`IsPlatformAdmin`) or module-override
  (`AdminCompanyModules`) paths.
