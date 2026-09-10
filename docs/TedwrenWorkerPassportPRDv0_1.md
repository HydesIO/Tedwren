# TEDWREN LTD — Worker Passport

**Product Requirements — v0.1, draft for review**

A third product: an individual worker's own credential record, built on the existing platform.

August 2026 · For: Leigh Hydes (CTO) and James Wheeler (Commercial Director)

Status: draft. Comment on this, then it folds into PRD v6.4 as Section 5.4.

> **In-repo mirror.** This is a plain-text mirror of `docs/TedwrenWorkerPassportPRDv0_1.docx`, kept for
> in-repo diffing and search per the PRD `.md`-mirror convention. The `.docx` is the file of record. This
> document is **not yet** part of PRD v6.4 — it becomes §5.4 only after Leigh and James have commented.

## How to read this

This is deliberately a separate document rather than an edit to PRD v6.4. Two reasons: v6.4 is already long
enough that a new section would be read last if at all, and this needs your comments before it is merged
rather than after. Once both of you have marked it up it becomes Section 5.4 of the PRD and this file is
retired.

It follows the same conventions as v6.4. Requirements are what and why, not how. Anything in Section 6 is a
rule rather than a requirement — a commercial, legal or safety constraint we would defend in a room, so if
one looks wrong, have the conversation rather than working around it. Requirement IDs use a **WP** prefix
and will keep those numbers when merged.

Four questions in Section 9 are open and marked for Leigh. Each carries a recommended default so there is
something to push against rather than a blank. The defaults are not decisions; they are starting positions.

## 1. Context

### 1.1 What this product is

An individual construction worker's own record of who they are and what they are qualified to do. Cards,
tickets, certificates, training, personal details and emergency contacts, held once, kept current, and
shared by the worker with whoever needs to see them.

It is for individuals. Not for companies, not for a self-employed person with two lads, not as a cheap tier
of the subcontractor product. If a buyer wants to manage other people, they want the subcontractor product.
Holding that line matters because the moment the passport gains a second seat it becomes a competing,
cheaper version of a product we already sell.

It sits below both existing products in scale and price, and alongside them in build. It shares their data
foundation entirely.

### 1.2 Why we are building it

- It is the worker side of a platform that currently has none. Both existing products hold data about
  workers; neither gives the worker anything.
- It removes the slowest part of a main contractor induction. A worker who arrives with a passport skips
  personal details, emergency contacts and qualifications and goes straight to the site video and questions.
- It is the first step of the cross-company worker record described in PRD Section 8.6, which is where the
  platform's long-term defensibility sits. Building the worker-held side now means Phase 6 is a permissions
  layer rather than a new product.
- It is a low-commitment first relationship with workers who may later meet us at a gate.

### 1.3 What it is not

It is not a rival to the CSCS digital skills passport and must not be built or described as one. Section 2
sets out why in detail; Section 7 sets out the licensing constraint that follows. The short version is that
CSCS already do card storage and verification, free, and we will not win that fight and should not pick it.

## 2. Competitive position — My CSCS

### 2.1 What they have

CSCS ship My CSCS, which they brand as "your Digital Skills Passport". It is free to download and free to
use. It contains:

- The digital CSCS card, live as soon as an application is approved, showing photograph, qualifications,
  training, occupation and expiry.
- Verification through CSCS Smart Check by dynamic QR code or manual entry. Real, live, against the scheme
  database.
- An AI-assisted card application and renewal flow.
- My Skills — a store for training and courses outside the card itself: SMSTS and SSSTS, CPD certificates,
  employer-led training and toolbox talks, fire safety and asbestos awareness proof.
- Sharing: a CV-style skills summary the worker can send.

Assume feature parity is not available to us on any of the above. They own the card and they own the
verification.

### 2.2 What is actually wrong with it

The GB App Store rating is poor. It would be convenient to read that as a product-quality opening, and that
reading is wrong.

The complaints that surface are about card application, payment and support, not about storage. Payments
taken against failed applications; the flow stalling at the payment step; applications sitting past the
stated turnaround with no tracking or notification; qualifications from recognised awarding bodies not being
accepted; support tickets closed without response. CSCS's own responses to reviewers repeatedly redirect
people to CSCS Online instead of the app.

So the wedge is not "ours is a better wallet". Theirs works and it is free. The wedge is that their passport
is welded to a transactional process people resent, and that reputation attaches to everything inside the
app.

Caveat for James: this is drawn from reviews visible in search, not a systematic pull of the full review
set. Before any of it goes in a customer-facing or investor-facing document, someone should export the
actual reviews and count the themes.

### 2.3 The four things they structurally cannot do

These are the product. Everything in Section 4 exists to deliver one of them.

| # | Gap | Why it is structural |
|---|---|---|
| 1 | Multi-scheme credentials with real expiry logic | The card is CSCS. My Skills holds everything else as an uploaded attachment. There is no concept of ECS, CPCS, EUSR, IPAF, PASMA, NPORS, first aid, face fit, asbestos awareness or plant tickets as managed credentials with their own renewal cycles. We can treat every credential type the same way. |
| 2 | A receiving end | CSCS state plainly that only individuals can use the app and that employers, training providers and third parties should use CSCS Online. A worker can share a summary; nobody is set up to receive it into a compliance workflow. That workflow is our entire platform. |
| 3 | Induction pre-fill | CSCS do not run inductions and will not. This is the only gap that is genuinely defensible, and it only has value once the main contractor product is live. |
| 4 | Expiry monitoring across everything | Card expiry is inherent to a card. What costs a worker a day's work is asbestos awareness, face fit or first aid lapsing without warning. |

## 3. Goals and success measures

| # | Goal | How we know |
|---|---|---|
| G1 | A worker can get their whole working record into one place in under ten minutes on a phone | Median time from first screen to a passport containing card, personal details and emergency contacts. Target under ten minutes, measured, not estimated. |
| G2 | A worker with a passport completes a main contractor induction materially faster than one without | Median induction completion time, passport holders versus not, on the same site. Target: at least three minutes and two screens removed. |
| G3 | A worker never loses a day to an expiry they did not know about | Proportion of credentials that lapse without the worker having opened a reminder. Target: below 5%. |
| G4 | A worker can prove what they hold to someone who is not our customer | Shares sent, and share links opened by the recipient. The second number is the real one. |
| G5 | Renewal | Proportion of passports renewed at month twelve. This is the number that decides whether the product continues. See Section 8. |

Explicit non-goal as a success measure: total signups. Volume without renewal is a cost line, not traction.

## 4. Requirements

### 4.1 Dependencies on the shared foundation

The passport adds no new identity or credential model. It reads and writes the existing one. These are
already specified in PRD v6.4 and are listed here so the dependency is explicit rather than assumed.

| From v6.4 | What the passport relies on it for |
|---|---|
| SF-1 | One person, one underlying record, keyed to mobile number, however many companies engage them. The passport is a view of that record owned by the worker, not a second record. |
| SF-2 | Each company sees only its own view of a person. The passport must not become a hole in this. |
| SF-3 | Archive and reactivation. A worker leaving an employer must not affect their passport. |
| SF-4 | Self-service from a link with nothing installed. The passport extends this rather than replacing it. |
| SF-5 | Card capture by photograph with card number, holder name and expiry read into structured fields. |

If any of these need changing to accommodate the passport, that is a PRD change and should be raised as one
rather than forked.

### 4.2 Passport requirements (WP)

P0 ships in the first release. P1 is in scope for the product but can follow.

| ID | Requirement | P |
|---|---|---|
| WP-1 | A worker can create a passport themselves, without an employer, without an invitation, and without any company existing on the platform. | P0 |
| WP-2 | The passport is keyed to the same person record as SF-1. A worker who already exists because an employer added them does not get a second record when they sign up; they gain ownership of a view of the one that exists. | P0 |
| WP-3 | A worker can hold any credential type, not only CSCS-logo cards: scheme cards, plant tickets, training certificates, medicals, licences. Each carries an issuer, a reference, an issue date and an expiry date as structured fields, not as an attached image alone. | P0 |
| WP-4 | Every credential carries a status: self-declared, employer-confirmed, or verified at a check. The three are visually and semantically distinct everywhere they appear, including in anything shared outward. See Rule W3 for who may write the third state. | P0 |
| WP-5 | Personal details and emergency contacts are held once and maintained by the worker. | P0 |
| WP-6 | Expiry monitoring across every credential, with reminders before lapse. Reminder timing is configurable by the worker, with a sensible default. Reminders are the product's main recurring contact with the worker and the main argument for renewal. | P0 |
| WP-7 | A worker can share a selected set of credentials with a named recipient. The recipient needs no account. The share is time-limited, revocable, and shows the credential status from WP-4 without alteration. | P0 |
| WP-8 | A worker can see every share they have made, when it was opened, and revoke any of them immediately. Revocation stops future access; it does not recall what has already been seen, and the worker is told this in plain words at the moment they revoke. | P0 |
| WP-9 | Induction pre-fill: where a worker with a passport begins an induction on a main contractor customer's site, personal details, emergency contacts and qualifications are pre-populated so the worker moves to the site video and questions. The worker confirms the pre-filled data before it is accepted. See Section 9.1, Q4. | P0 |
| WP-10 | A worker can export everything in their passport in a form they can keep and use elsewhere, at any time, without asking us. | P0 |
| WP-11 | A worker can delete their passport. Deletion removes their ownership and the passport view; it does not delete records an employer holds about them independently under that employer's own lawful basis, and the worker is told this clearly before confirming. | P0 |
| WP-12 | Non-payment does not lock a worker out of their own record. See Rule W2 — this is a rule, not a preference, and the mechanism is in Section 8. | P0 |
| WP-13 | Account recovery when a worker changes mobile number, without which SF-1 identity becomes a trap rather than a feature. Construction phone numbers change often. | P0 |
| WP-14 | Every read, share, revocation and status change against a passport is recorded with actor, timestamp and reason, retrievable later. The worker can see their own audit trail. | P0 |
| WP-15 | A digital credential the worker can present at a gate or a site office — a QR code or a wallet pass. What that QR resolves to is constrained by Rule W4. | P1 |
| WP-16 | A worker can record which sites and employers they have worked for, from their own attendance where it exists on the platform. This is their work history, not an employer's record of them. | P1 |
| WP-17 | Where a worker's credential is verified at a check by a main contractor customer, the outcome is written back to the passport as a dated record of that check. Subject to Rule W3 and Section 7. | P1 |
| WP-18 | A worker can nominate a person or company to be notified when a credential is about to expire — a supervisor, an agency, a partner. Opt-in only, revocable, and never automatic. | P1 |

## 5. Non-goals

### 5.1 Not being built, at all

| Not building | Why |
|---|---|
| A rival card scheme or a competing digital skills passport | CSCS own the card and the verification. We are building the worker side of our platform, not a competitor to theirs. |
| Verification on demand from the passport | Prohibited by the CSCS licence. See Section 7. This is the single most likely thing to be built by accident. |
| A second seat, a team view, or any way to manage another person | That is the subcontractor product. The moment the passport can hold two people it undercuts a product we sell. |
| Worker scoring, ranking, ratings or any comparative judgement | A worker will not adopt a product that grades them, and it would poison the consent model the platform depends on. |
| Right to work documents and immigration status | RTW belongs at employment onboarding under an employer's duty, not in a worker-held wallet. Holding it here creates discrimination and liability exposure without discharging anyone's obligation. Revisit only alongside the RTW add-on and only with legal sign-off. |
| Job matching, recruitment, or advertising to workers | Different business. It would also make every worker rightly suspicious of why we hold their data. |
| Payroll, payments to workers, or anything that looks like it | Out of scope for this product and sequenced elsewhere in the PRD. |

### 5.2 Deferred, and the design must not foreclose it

- Cross-company sharing under PRD Section 8.6. The passport is the worker-held half of it. Permissions must
  be modelled as specific, revocable grants from day one even though only the worker-initiated share in WP-7
  ships first.
- Live CSCS verification, PRD Phase 1. WP-17 depends on it entirely and should be built behind the same
  interface.
- Other scheme integrations — ECS, CPCS, EUSR, NPORS. Each will have its own licence terms and none should
  be assumed.
- An employer paying for a worker's passport. Commercially attractive, and it changes the consent position
  materially. Do not build it in, do not design it out.

## 6. Rules that must hold

As with PRD Section 7: these are constraints rather than requirements. If one looks wrong, have the
conversation.

| # | Rule | Why |
|---|---|---|
| W1 | No app install is ever required to use the passport. Everything essential works in a phone browser from a link. An app may exist and may be the better route for reminders and wallet passes, but it is never the only route. | Extends R1. The adoption barrier is the install, and a worker standing at a gate will not clear it. |
| W2 | A worker's record is never locked, hidden or deleted because they stopped paying. Lapse moves the passport to read-only and stops reminders and sharing; the worker keeps access to everything and can export it at any time. | Holding a tradesman's credentials hostage would be indefensible, and in this industry the reputational damage would arrive faster than the revenue. |
| W3 | Only the main contractor product, running a check under a customer's own CSCS licence, may write the verified state in WP-4. The passport never initiates a verification and never presents itself as a verification service. | The CSCS licence permits checks only through a Service User's site access or induction system. See Section 7. Breaching this risks the IT Partner relationship the whole roadmap depends on. |
| W4 | Nothing shared or scanned out of the passport is ever presented as CSCS-verified unless it carries a real, dated check performed under W3, and the wording must make clear that the check was performed by a named party on a named date rather than by us. | Misrepresenting verification status is the fastest way to destroy both worker trust and the CSCS relationship. |
| W5 | No employer, contractor or third party sees anything in a passport without the worker's specific, revocable permission. There is no administrative override and no company-level switch. | Mirrors MC-20 and PRD 8.6. Consent obtained any other way is not freely given, and it cannot be retrofitted. |
| W6 | The passport is never presented as proof of right to work, identity, or fitness to work. | It is none of those things. Allowing anyone to treat it as one transfers a duty we cannot discharge onto a product that cannot carry it. |
| W7 | The worker is told, in plain words at the point it matters, what a share exposes, what revocation does and does not do, and what happens to their record if they stop paying. | Consent that is not informed is not consent, and the first surprise destroys a product that lives on trust. |

## 7. The CSCS constraint

This section is short and it is the most important constraint in the document. It should be read before any
design work touching credentials.

### 7.1 What the licence permits

CSCS Smart Check API access is granted to approved IT Partners to build into their customers' site access
and induction systems. Every API key requires a tri-partite agreement between the IT Partner, the Service
User and CSCS. The Service User is defined as the employer or main contractor with primary control and
responsibility for the site, and checks are permitted solely in connection with cardholder access to that
party's sites through access control and entry systems. The licence is non-transferable and carries no right
to sublicence. IT Partners are additionally bound by a Licensee Partner Policy.

### 7.2 What that means here

- A worker-owned passport cannot hold a Smart Check licence. There is no Service User, no site, and no access
  control system. There is no version of this product that can call the API in its own right.
- It also cannot be granted one indirectly. No sublicensing means our main contractor customer's licence
  cannot be extended to cover checks the passport initiates.
- Verified data can still reach the passport, but only in one direction. When a main contractor customer runs
  a check at induction or at a gate under their own licence, that check is lawful. The result is personal
  data about the worker, and the worker being shown their own result, dated and attributed, is a different
  thing from us running a verification service. That is what WP-17 does and it is the only route.

### 7.3 What we need confirmed

The position in 7.2 is our reading, not CSCS's. Two questions go to CSCS in writing before WP-17 is built,
and they should go in the same letter as the outstanding subcontractor question from the PRD:

- Whether a verification result obtained lawfully by a Service User may persist in a worker-held record as a
  dated record of that check, and under what conditions it may be displayed.
- A copy of the Licensee Partner Policy, which we have not seen and which will constrain what we may display,
  cache and share.

Until both are answered, WP-17 stays behind an interface and ships nothing. Everything else in Section 4
proceeds.

## 8. Commercial model

Included because it shapes the product, not because it is settled.

| Item | Position |
|---|---|
| Price | £10 per worker per year, billed annually in advance. |
| Who pays | The worker. Employer-funded access is deferred, not designed out — see 5.2. |
| What lapse does | Read-only. Reminders and sharing stop. Nothing is hidden, deleted or locked. Rule W2. |
| Free tier | Not proposed. If one is introduced later the natural line is that holding and exporting your record is free and reminders and sharing are paid, because that is the line Rule W2 already draws. |
| Counted as | Active paid passports. A lapsed passport is not billable and not counted. |

Please do not build the price into the code. The number will move.

### 8.1 The commercial risk James and I should be explicit about

The paying customer is an individual buying something that is convenient rather than compulsory, from a
company they have not heard of, in competition with a free product from the body whose card they already
carry. Two consequences the build should account for:

- Renewal is the number that decides the product, and annual upfront billing means we do not see it until
  month twelve. Instrument G3 and G4 from day one — reminder opens and share opens are the leading
  indicators of whether anyone will renew, and they are visible in month two.
- Support cost scales with users while revenue per user does not. Every requirement in Section 4 that avoids
  a support ticket — WP-13 account recovery in particular — is worth more than it looks.

## 9. Open questions

### 9.1 For Leigh — these change the shape of the build

Each carries a recommended default so there is a position to argue with. None is decided.

| Q | Question | Recommended default |
|---|---|---|
| Q1 | Who is the data controller of a self-registered passport? If the worker signs up before any employer relationship exists, are they the controller with us as processor, or are we controller throughout? | This is a legal determination as much as a technical one and it should not sit with you alone — but it gates the schema, so it needs answering before tables are built. Our starting position: the worker is controller of the passport view, we are controller of the underlying platform record, and the two are separated in the model from day one. If that is expensive, say so early. |
| Q2 | Collision. A worker holds a passport. A subcontractor or main contractor customer then adds them as an operative on the same mobile number. SF-1 says one record. What does the employer see? | Nothing from the passport without a specific WP-7 style share. The employer sees only what they have collected themselves, exactly as SF-2 requires. The passport becomes an offer to the worker to share, never an automatic disclosure. This is the most consequential rule in the document and it is worth your challenge if you think it is the wrong shape. |
| Q3 | App or mobile web? Rule W1 requires that everything essential works in a browser. Reminders and wallet passes are a genuine argument for a native app. | Mobile web first, with a native app as a fast follow if reminder engagement in G3 justifies it. But this is the decision most likely to be cheaper made once, up front, so if you would rather build native from the start, make the case and we will take it. |
| Q4 | Does WP-9 pre-fill the induction, or write through it? Pre-fill means the worker confirms everything and the contractor's record is their own. Write-through is faster and moves where liability sits. | Pre-fill. The contractor is buying a record they own and can defend eighteen months later, and a pre-filled field the worker confirmed is defensible in a way an imported one is not. If the time saving from write-through is materially larger than we think, say so. |

### 9.2 For James

- Does £10 survive contact with a worker who has just been told My CSCS is free? Worth asking directly in the
  first ten conversations rather than inferring it from conversion.
- Who do we sell this to first, and how do they hear about it? The referral route proposed in the original
  strategy is not available — see 9.3.
- Do we say anything publicly about the passport before the main contractor product is live, given WP-9 is
  the differentiator and it does not work until then?

### 9.3 Legal and compliance

- The controller question in Q1 needs a view before the schema is fixed.
- A DPIA is required. A worker-held store of identity, qualifications and emergency contacts, shared with
  third parties, meets the threshold comfortably.
- Any referral or incentive scheme routed through site management is off the table. Paying a person who
  controls site access to sign up the workforce they control compromises the consent Rule W5 depends on, and
  sits badly against main contractor Bribery Act controls.
- Consumer contract terms, not business terms. The buyer is an individual, which brings cancellation rights
  and unfair terms provisions that our existing terms do not address.

## 10. What we need from Leigh

| # | Ask | By |
|---|---|---|
| 1 | Answers or challenges to Q1 to Q4 in Section 9.1. Q1 and Q2 block the data model; Q3 and Q4 block the delivery approach. | Before estimating |
| 2 | A view on whether the passport can genuinely be built alongside the main contractor product without slowing it. You have said it can. Confirm it once you have read Section 4, because Section 4 is longer than the conversation was. | With the estimate |
| 3 | An estimate in the same shape as the v6.4 estimate, split P0 and P1, so the two can be compared. | — |
| 4 | Anything in Section 6 you think is wrong. Those are the rules and they are the expensive things to discover late. | With comments |
| 5 | A view on whether WP-17 should be built behind an interface now or left out entirely until CSCS answer Section 7.3. | With comments |

Once both of you have commented, this becomes Section 5.4 of PRD v6.4 and the requirement numbers carry over
unchanged.

## Sources

- CSCS Smart Check correspondence to J. Wheeler — tri-partite licence extracts, Background D, clauses 2.1.1
  and 2.1.2.
- CSCS — My CSCS: your Digital Skills Passport; My Skills, part of your Digital Skills Passport; CSCS Smart
  Check and IT Partners pages (cscs.uk.com, cscsgroup.co.uk), accessed August 2026.
- My CSCS listing and user reviews, Apple App Store GB and Google Play, accessed August 2026. Indicative only
  — see the caveat in Section 2.2.
- Tedwren PRD v6.4 — SF-1 to SF-5, MC-20, Rule R1, Sections 7, 8.1 and 8.6.
