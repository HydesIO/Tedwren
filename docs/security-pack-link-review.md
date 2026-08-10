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

## Residual risks & hardening actions (to complete before launch)
1. **Brute-force of the passcode.** The token is unguessable, but once a token leaks (e.g. a forwarded email),
   the passcode is the only remaining secret. **Action:** add server-side rate limiting / lockout per token on
   `view`/`download` (e.g. exponential backoff after N failed passcodes) and alert the sender on repeated
   failures. Not yet implemented.
2. **Token entropy.** Confirm `Token` is generated from a CSPRNG with ≥128 bits of entropy and is URL-safe.
   **Action:** audit the token generator; add a unit test asserting length/charset/uniqueness.
3. **Passcode hashing strength.** Confirm `PasscodeHash` uses a slow, salted KDF (e.g. PBKDF2/Argon2), not a
   fast hash. **Action:** verify the hashing implementation and document the algorithm/parameters.
4. **Transport & caching.** Ensure the API enforces HTTPS (it calls `UseHttpsRedirection`) and that pack
   responses set `Cache-Control: no-store` so snapshots aren't cached by intermediaries. **Action:** add the
   no-store header to the download/view responses.
5. **Enumeration timing.** Make refusal responses constant-time / uniform so a caller cannot distinguish
   "unknown token" from "wrong passcode" by timing or message. **Action:** review `ViewAsync` refusal paths.
6. **Link lifetime hygiene.** Encourage short default expiry and one-off passcodes; consider single-use or
   view-count caps for the most sensitive packs. **Action:** product decision.
7. **PII minimisation.** Confirm the snapshot includes only what the recipient needs (no full DOB, no card
   images unless required). **Action:** review `PackSubjectDto`/`PackCardDto` fields against SUB-15.

## Independent review
This internal review does not replace the **independent security review** required by the launch gate; it
scopes that engagement. Priorities 1–5 above are the concrete engineering items to close first.
