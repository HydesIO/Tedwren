# Profile page — development plan

> Status: **plan only** (no implementation in this change). This document records the agreed scope
> and approach for the self-service Profile page in the tenant console (main contractor /
> subcontractor area). The PRD (`docs/TedwrenPRDv6_4.docx`) remains the source of truth; where this
> plan touches a requirement, cite the SF/SUB/MC/R id in the implementing commit and `TODO.md`.

## Context

The console has an avatar/profile dropdown (`ProfileMenu`) whose **"Profile" link is a dead link**
(`ProfileHref` defaults to `"#"`) — there is no profile page anywhere in the app. A signed-in
console user (main contractor or subcontractor) currently has **no way to** edit their own details,
set a mobile number, change their password, upload an avatar, edit their company's details, or
manage billing. This plan builds that page and the missing backend behind it, and removes the
redundant "Settings" item from the profile dropdown.

Exploration confirmed the following are **net-new** (do not exist yet):

- `User` has **no** `Mobile` and **no** avatar/image field (`src/Tedwren.Domain/Entities/User.cs`).
- There is **no self-service** "edit my own details" path — `IUserService.UpdateUserAsync` is
  admin-only and edits name + role only.
- There is **no authenticated change-password** — only forgot-password → email reset link.
- There is **no avatar-upload endpoint** (the base64 → `IImageStore.SaveAsync` → `/api/images/{id}`
  pattern from onboarding is the precedent to reuse).
- Billing (GoCardless direct debit) exists but is **Tedwren-platform-admin only**
  (`/api/admin/billing`, `PlatformAdmin` policy) — there is **no company-scoped, customer-facing**
  billing surface.

## Agreed decisions

| Decision | Choice |
|---|---|
| Settings removal | Remove **only** the profile-dropdown "Settings" link. Keep the sidebar "System Configuration" page and the admin "Admin Settings" item as-is. |
| Billing | **Full self-service** — a company Administrator can view their subscription/mandate and set up / change direct debit from Profile. |
| Company details | Editable by **Administrators only**; all other roles see them read-only. |

> **PRD alignment to confirm before building the billing section:** customer-facing self-service
> billing/direct debit is not clearly one of the existing admin-scoped billing requirements. Confirm
> it against `docs/TedwrenPRDv6_4.md` (Section 8 commercial modules) and cite the correct id; if the
> PRD only sanctions admin-scoped billing, raise the discrepancy rather than inventing the
> requirement.

## Approach

One new `/profile` page composed of the existing MudBlazor kit, backed by a new self-service
account service and a small set of authenticated `/api/me/*` endpoints. Reuse existing patterns
throughout — **no new component patterns**.

### 1. Domain — `src/Tedwren.Domain/Entities/User.cs`
Add `string? Mobile` and `string? AvatarImageReference` (stores the `IImageStore` reference id,
served via `GET /api/images/{id}`, consistent with R9 image handling). No other entity changes;
company fields already exist on `Company.cs`.

### 2. Persistence — EF migration + Dapper
- Add an EF migration for the two new `Users` columns per `docs/ef-migrations.md`.
- Update the Dapper User SQL in **both** dialects (SQL Server + PostgreSQL) under
  `src/Tedwren.DataAccess/Repositories/` — the shared base + dialect SQL for User
  SELECT/INSERT/UPDATE so `Mobile` / `AvatarImageReference` round-trip. Add an
  `UpdateProfileAsync`-style repo method (name/email/mobile/avatar) that does **not** touch
  role/status (self-service must not let a user change their own role — tenant-safety, R15).

### 3. Contracts / DTOs — `src/Tedwren.Abstractions/Contracts/`
- New `Account/` (or extend `Identity/`) DTOs:
  - `MyProfileDto(Name, Email, Mobile, Role, RoleLabel, CompanyId, CompanyName, AvatarUrl,
    IsAdministrator)` — `AvatarUrl` resolved to `/api/images/{ref}` (or null).
  - `UpdateMyProfileRequest(Name, Email, Mobile)`.
  - `ChangePasswordRequest(CurrentPassword, NewPassword)`.
  - `UpdateAvatarRequest(ImageBase64, ContentType)` (mirror onboarding card capture).
- Extend `CurrentUserDto` (`Identity/IdentityDtos.cs`) with `AvatarUrl` so the top-bar avatar can
  render without a second call.
- Company edit reuses existing `UpdateCompanyRequest` / `CompanyDetailDto`
  (`Organisation/OrganisationDtos.cs`).
- Billing reuses existing `CompanyBillingOverviewDto` / `MandateSetupResultDto`
  (`Billing/BillingDtos.cs`).

### 4. Application services — `src/Tedwren.Application/`
- New `IProfileService` (`src/Tedwren.Abstractions/Services/IProfileService.cs`) + impl:
  `GetMyProfileAsync()`, `UpdateMyProfileAsync(UpdateMyProfileRequest)`,
  `ChangePasswordAsync(ChangePasswordRequest)`, `UpdateAvatarAsync(UpdateAvatarRequest)`.
  Resolves the caller from `ICurrentUserService`; **never** trusts a client-supplied user id.
  - Change-password verifies the current password with the existing `PasswordHasher.Verify` and
    stores `PasswordHasher.Hash(new)` + `PasswordSetUtc` (reuse the hasher used by
    `src/Tedwren.Application/Auth/AuthService.cs`).
  - Avatar decodes base64 and calls the existing `IImageStore.SaveAsync(bytes, contentType)`
    (`src/Tedwren.Application/Persistence/IOnboardingLinkRepository.cs`), stores the returned
    reference on the user.
- Company editing on Profile reuses `IOrganisationService.UpdateCompanyAsync(companyId, …)`. If a
  by-id "my company" fetch is missing, add a thin `GetCompanyByIdAsync` (the caller's `CompanyId`),
  or resolve slug → detail; keep it minimal.
- **Self-service billing:** add a company-scoped facade (e.g. `IMyBillingService` or new methods)
  that force `companyId = caller's CompanyId` and **require the Administrator role**, delegating to
  the existing `BillingService` (`GetCompanyBillingAsync`, `StartMandateSetupAsync`,
  `CancelMandateAsync`). Do **not** relax the existing `PlatformAdmin` billing surface.

### 5. API endpoints — `src/Tedwren.Api/Endpoints/`
- New authenticated group (no `.AllowAnonymous()` — secure-by-default per CLAUDE.md), extending
  `CurrentUserEndpoints.cs` or a new `ProfileEndpoints.cs`:
  - `GET  /api/me/profile`
  - `PUT  /api/me/profile`
  - `POST /api/me/password`
  - `POST /api/me/avatar`
- New **company-scoped billing** endpoints requiring auth + Administrator + own-company:
  `GET /api/me/billing`, `POST /api/me/billing/mandate` (start setup → returns `AuthorisationUrl`),
  `POST /api/me/billing/mandate/cancel`. Reuse the existing `GET /api/images/{id}` for avatar
  serving (`ImageEndpoints.cs`).

### 6. Client services — `src/Tedwren.Client/Services/`
Add `ApiProfileService.cs` (and billing wrapper) mirroring the existing `Api*Service.cs` wrappers;
register in DI alongside the others. Update `ApiCurrentUserService` to carry `AvatarUrl`.

### 7. UI — new page + shell wiring
New `src/Tedwren.Client/Pages/Profile/Profile.razor`, `@page "/profile"`. Follow the
load → edit → save → snackbar pattern of `Pages/SystemConfiguration/SystemConfiguration.razor` and
`Pages/Users/UserDetail.razor`. Compose from existing kit only: `PageHeader`/`DetailHeader` +
`DashboardCard` + `FormSection` + `Tedwren*` inputs + `FormActions` + `ISnackbar` + `ConfirmDialog`;
colours from `tokens.css`.

Sections:
1. **Personal details** — name, email, mobile (`TedwrenTextField`); Save → `UpdateMyProfileAsync`.
2. **Avatar** — `TedwrenFileUpload` (image accept), preview, Save → `UpdateAvatarAsync` (base64,
   following the `DynamicFormRenderer.GetFiles()` base64 approach).
3. **Security** — current / new / confirm password (`InputType.Password`) → `ChangePasswordAsync`;
   client-side confirm-match validation.
4. **Company / organisation** — registration-scoped fields; **editable only when the current role is
   Administrator**, otherwise a read-only `KeyValueList`. Save → `UpdateCompanyAsync`.
5. **Billing** (Administrators only) — subscription + mandate status via `CompanyBillingOverviewDto`;
   "Set up / change direct debit" → `StartMandateSetupAsync` → redirect to `AuthorisationUrl`;
   "Cancel mandate" behind `ConfirmDialog`.

All MudBlazor inputs use `@bind-Value` / `Value`+`ValueChanged` (CLAUDE.md live-binding rule — no
one-way `Value=` on mutable fields).

**Shell wiring** (`src/Tedwren.Client/Layout/MainLayout.razor`):
- Pass the real avatar URL to `AppTopBar` (`CurrentUserAvatarUrl`) from the current user, instead of
  `string.Empty`.
- Wire `OnSignOut` through to the dropdown (currently a no-op there) and set `ProfileHref="/profile"`
  (via `AppTopBar` → `ProfileMenu`; add passthrough params on `AppTopBar.razor` if absent).

**Remove the profile-dropdown "Settings" link** in
`src/Tedwren.UiComponents/Navigation/ProfileMenu.razor` (delete the Settings `<a>` item and the
now-unused `SettingsHref` param). Leave sidebar "System Configuration" and admin "Admin Settings"
untouched.

### 8. Docs & tracking
- Update `TODO.md` (new entries with phase/area and the PRD id once confirmed) and
  `docs/plan-and-scope.md` if this adds a discrete deliverable.
- Add the Profile page to `docs/component-catalogue.md` only if any new shared component is
  introduced (goal: none — reuse only).

### 9. Tests — `tests/*` (xUnit)
- Application: `ProfileService` unit tests — update details; change password (wrong current →
  rejected, correct → rehashed); avatar save; and that role/status **cannot** be self-changed.
- API: `Tedwren.Api.Tests` (in-memory, per CLAUDE.md) — `/api/me/*` require auth (401 anonymous);
  company edit + billing endpoints reject non-Administrators (403); billing is company-scoped to the
  caller. Tests must not mutate production records (isolated/transactional data).

## Verification (when implemented)
1. `dotnet build Tedwren.sln` — zero errors; investigate any new warnings.
2. `dotnet test Tedwren.sln` — all suites green (new Profile/account tests included).
3. `dotnet run --project src/Tedwren.Api` + `dotnet run --project src/Tedwren.Client`, sign in and:
   - Open the avatar dropdown → "Profile" navigates to `/profile`; the "Settings" item is gone.
   - Edit name/email/mobile, save → snackbar success; reload shows persisted values.
   - Upload an avatar → it appears top-right (initials fallback otherwise).
   - Change password → sign out, sign back in with the new password.
   - As an Administrator: edit company details and see the billing section + start a direct-debit
     mandate setup (redirect to the GoCardless authorisation URL). As a non-admin: company + billing
     are read-only / hidden.

## Open items to confirm during build
- **PRD alignment for self-service billing** (see PRD note above) — confirm and cite the id, or
  raise the discrepancy.
- **Email as sign-in identity:** editing email changes the login. Decide whether email edits are
  allowed self-service or need re-verification; the simplest first cut is to allow name/mobile freely
  and treat email edits conservatively.
