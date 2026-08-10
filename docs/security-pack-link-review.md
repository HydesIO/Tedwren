# Security review — public compliance-pack link

The compliance-pack recipient link (`/pack` in the client, `/api/packs/view|download.*` in the API) is the
**only public route to personal data** in Tedwren, so it warrants a focused review. This document records the
current controls, the residual risks, and the hardening actions to complete before launch. It is a working
review, not a sign-off.

## What the link exposes
A fixed-at-send snapshot (R7) of chosen operatives' compliance: names and their qualification cards
(qualification, issuer, expiry, verification state). No login, no account (R8).

## Controls in place today
| Control | Where | Notes |
|---|---|---|
| Opaque token, not guessable | `CompliancePack.Token`, unique index (R9) | Token is the only handle; there are no permanent public asset URLs. |
| Passcode gate | `ViewAsync` / `Download*Async` require token **and** passcode | Passcode is stored hashed (`PasscodeHash`), never returned. |
| Expiry | `ExpiresUtc`; access refused past expiry | Configurable at send (`ExpiryDays`). |
| Revocation | `RevokeAsync` (SUB-21) | A revoked pack is immediately inaccessible. |
| Re-issue supersedes | `SupersededByPackId` (R7) | A superseded snapshot stops serving. |
| Access logging | `PackAccessEvent` on open/download (SUB-20) | Sender sees opened/downloaded tallies. |
| No account data leakage | Recipient DTO is the snapshot only | No console/tenant data crosses the boundary. |
| Fail-closed refusals | API returns 403 with a reason; never the pack | Wrong/expired/revoked all refuse uniformly. |

## Residual risks & hardening actions
1. **Brute-force of the passcode.** ✅ **Done.** Per-token online rate limiting is in place
   (`IPackAccessThrottle` / `InMemoryPackAccessThrottle`): after 5 failed passcode attempts a token is locked
   out for 15 minutes; a success clears the count. A distributed cache would back this in a multi-instance
   deployment; sender alerting on repeated failures is still to add.
2. **Token entropy.** ✅ **Verified.** `PackToken.Generate` uses a 256-bit CSPRNG (`RandomNumberGenerator`),
   URL-safe base64. Meets the ≥128-bit requirement.
3. **Passcode hashing strength.** ✅ **Done.** `PackPasscode` now uses **PBKDF2-SHA256, 210,000 iterations**,
   per-passcode 16-byte salt, constant-time compare; the legacy salted-SHA-256 format is still verifiable for
   any pre-existing hashes.
4. **Transport & caching.** ✅ **Done (caching).** The recipient `view`/`download.*` endpoints set
   `Cache-Control: no-store, no-cache, must-revalidate` + `Pragma: no-cache` via an endpoint filter. HTTPS is
   enforced by `UseHttpsRedirection`. HSTS in production is still to confirm.
5. **Enumeration timing.** Refusals are message-distinct (unknown link vs incorrect passcode vs expired/
   revoked). Tokens are 256-bit so enumeration is infeasible; passcode compare is constant-time. **Action
   (open):** consider unifying the unknown-token and wrong-passcode messages to remove the oracle.
6. **Link lifetime hygiene.** Short default expiry and one-off passcodes; consider single-use or view-count
   caps for the most sensitive packs. **Action (open):** product decision.
7. **PII minimisation.** Confirm the snapshot includes only what the recipient needs (no full DOB, no card
   images unless required). **Action (open):** review `PackSubjectDto`/`PackCardDto` fields against SUB-15.

## Independent review
This internal review does not replace the **independent security review** required by the launch gate; it
scopes that engagement. Priorities 1–5 above are the concrete engineering items to close first.
