> **Mirror note:** This is a plain-text Markdown mirror of `Tedwren-Website-Content-Build-Spec-v2.docx`,
> kept for in-repo diffing and search. The `.docx` is the file of record; if it is revised, re-sync this
> mirror in the same change (same convention as the PRD mirror).

---

# TEDWREN LTD
Website Content & Build Specification
v1.0 · Draft for founder and developer review · August 2026
Company: Tedwren Ltd  |  Product names: to be confirmed — see Section 2
Contents

## 1. How to use this document
This is the build-ready content and technical specification for the Tedwren marketing website. It extends and tightens the earlier content spec with the sections a developer needs to actually build the site: full sitemap, page-by-page copy, forms and lead routing, technical and platform requirements, and a pre-launch checklist.
Anything in a shaded box marked “DEV NOTE” is a build instruction rather than copy. Anything in italics under a bold label is draft copy, ready to use as a first pass — refine tone, but don't restructure the hierarchy without good reason (see Section 8).
Open items for the founders — not blocking the build, but blocking sign-off — are collected in Section 13.

## 2. Brand and naming rules — read before writing any code or copy
The company is Tedwren Ltd. That is settled. The customer-facing product names are not: two names have already been withdrawn (INDUCTED, then PERMITTD), and the Worker Passport name is a working title, not a confirmed brand. Naming is on the launch critical path, not the build critical path — it is blocked on trademark clearance, domain availability and app-store name reservation, not on development.

#### What this means for the site
- Never hardcode a product name in a template, image, URL slug, filename, or piece of marketing copy that would be expensive to change.
- Every customer-visible product name (“the Subcontractor product”, “the Main Contractor product”, “Worker Passport”) must be pulled from a single CMS/config source of truth, referenced everywhere by key, not typed inline.
- URL slugs should describe the audience or function, not a brand (/subcontractors, /main-contractors, /worker-passport), so they survive a rename untouched.
- Legal entity references (footer copyright, Terms, Privacy Policy, contracts) use “Tedwren Ltd”, which is settled and will not change.
- Do not register social handles, email-sending domains, or analytics/tag-manager property names against a product name — use a neutral internal codename, exactly as instructed for the product build.

> **DEV NOTE:** Build the copy layer as CMS entries (see Section 12.2), not as text baked into components. When names are confirmed, rename should be a content edit and a redirect map, not a re-deploy.

## 3. Purpose and principles
The website has four jobs: make each buyer recognise their problem, explain the product in about 30 seconds, build enough trust to justify a demo, and create a foundation for organic and referral acquisition. It sells outcomes, not software features, and it does not try to explain the whole platform to any one visitor.

#### Content rules that apply everywhere on the site
- Don't lead with “compliance platform.” Lead with the outcome the visitor came to check.
- Split the visitor by audience within the first screen — subcontractor, main contractor, or worker — and never make one buyer read the other's page to find their own value.
- Hierarchy is Outcome → Problem → How Tedwren solves it → Evidence/features → CTA, on every landing page. Never Platform → Features → Technology → About us.
- No invented traction. No “trusted by leading contractors”, fabricated logos, or fabricated review stars until real customers exist. See Section 9 for how to handle this honestly pre-launch.
- No absolute compliance claims (“100% compliant”, “guarantees compliance”, “ensures legal compliance”). Tedwren provides evidence and workflow controls; it does not assume the contractor's or employer's legal responsibilities. See Section 8.
- Never position Worker Passport as a replacement for, or rival to, the CSCS Digital Skills Passport. See Section 8 — this is a licensing constraint, not a style preference.

## 4. Sitemap
Launch scope is deliberately small. No SEO farm, no resources hub, no case studies that don't exist yet, no individual feature pages beyond what's below. Those grow once real customers supply real language.

| Page | URL | Primary audience | Primary CTA |
| --- | --- | --- | --- |
| Home | / | All — routes to the right audience | I'm a: Subcontractor / Main Contractor |
| Subcontractor | /subcontractors | Specialist subcontractor buyer | Book a demo |
| Main Contractor | /main-contractors | Principal / regional contractor buyer | Book a demo |
| Worker Passport | /worker-passport | Individual construction worker | Get your Worker Passport |
| Pricing | /pricing | All B2B buyers | Book a demo |
| Security & Trust | /security | Procurement, H&S, IT/DPO reviewers | Talk to us |
| About | /about | All — credibility check before a demo | Book a demo |
| FAQ | /faq | All — objection handling | n/a (deflects to relevant page) |
| Book a Demo / Pilot | /demo | B2B buyers ready to convert | Submit form |
| Partners / Affiliates | /partners | Business introducers, advisers, trade bodies, existing customers | Apply to become a partner |
| Compliance Pack Viewer | /pack/[id] | Pack recipients — not a marketing page, see 4.1 | Book a demo (secondary) |
| Contact | /contact | General enquiries, press, partners | Send message |
| Privacy Policy | /legal/privacy | n/a | n/a |
| Cookie Policy | /legal/cookies | n/a | n/a |
| Terms of Service | /legal/terms | n/a | n/a |
| Data Protection & Security | /legal/data-protection | n/a | n/a |
| 404 | n/a | n/a | Return home / Book a demo |


#### 4.1 The compliance pack viewer is a marketing touchpoint, not just a product feature
The Go-to-Market strategy identifies the compliance pack as the primary distribution mechanism into main contractor accounts: a subcontractor customer sends a pack, and the recipient opens it having never heard of Tedwren, with no account required. That page therefore needs a small amount of marketing thinking even though it lives in the product, not the CMS.
- The pack itself stays exactly what it is: fixed evidence, no upsell inside the document.
- The page chrome around the pack (not the pack content) carries a light, non-intrusive footer: “This compliance evidence was produced with Tedwren — [Book a demo]”, styled distinctly from the pack so it's obviously not part of the evidence itself.
- No sign-up wall, ever, at any point in viewing a pack. This is a rule, not a preference — a wall kills the primary acquisition path.

> **DEV NOTE:** This page is served by the product, but its chrome should pull the same header-light/footer-light components and brand tokens as the marketing site, and the “Book a demo” link should carry a UTM tag identifying it as pack-referral traffic so Stage 2 of the GTM funnel is measurable.

## 5. Global components

### 5.1 Header / navigation
- Logo (left) → home.
- Primary nav: For Subcontractors · For Main Contractors · Worker Passport · Pricing · About.
- Persistent CTA button, top-right: Book a demo. On the Worker Passport page only, this swaps to Get your Worker Passport (see 5.4).
- Mobile: collapse to a hamburger menu; keep the primary CTA visible outside the collapsed menu, not inside it — the CTA should never require two taps to reach.

### 5.2 Footer
- Company: Tedwren Ltd, registered office, company number (pull from Companies House filing, not typed by hand — see 12.6).
- Site links: mirror the primary nav plus Security & Trust, FAQ, Partners, Contact.
- Legal: Privacy Policy, Cookie Policy, Terms of Service, Data Protection & Security.
- Social — only include icons for accounts that actually exist at launch. An empty/dead social icon reads worse than no icon.

### 5.3 Trust strip
A compact, reusable component — logos-optional row plus one-line claims — that appears near the bottom of Home, Subcontractor, and Main Contractor pages, and expands into the full Security & Trust page. Build this as one component referenced in three places, not copy-pasted three times, since its content will change fastest once real customers and certifications exist.

### 5.4 Primary CTAs — exactly three, everywhere

| CTA | Audience | Destination | Notes |
| --- | --- | --- | --- |
| Book a demo | Subcontractor / Main Contractor buyers | /demo | Primary B2B action across the whole site |
| Start a pilot | High-intent main contractor / subcontractor buyers | /demo (toggle or second form step) | Surface once the pilot terms are settled — see Pricing page, Section 6.5 |
| Get your Worker Passport | Individual workers | /worker-passport signup flow | The only consumer-facing, self-serve, paid CTA on the site |

Avoid “Learn More / Get Started / Discover / Explore” as button copy anywhere. Every button should say the actual next action.

### 5.5 Cookie / consent banner
- Required because analytics are in use (12.7). Must allow reject-non-essential in one click, not just “Accept all” — a cookie banner with no equal-weight reject option is a common UK ICO enforcement point.
- No analytics, ad, or heatmap script fires before consent is given, categorised as non-essential.

## 6. Page-by-page content

### 6.1 Home — /

#### Hero
Eyebrow
Construction workforce compliance, without the paperwork.
H1
Know who's ready for work. Know who's actually on site. Prove it when it matters.
Supporting copy
Tedwren brings workforce records, qualifications, attendance, inductions and compliance evidence into one platform — built for the way contractors and subcontractors actually work.
Primary CTA
Book a demo
Secondary CTA
See how it works
Audience split
I'm a: [Subcontractor] [Main Contractor]

#### Problem section
H2
Construction keeps checking the same information over and over again.
Body
Every new site means another spreadsheet, another folder of card photographs, another induction and another request for evidence. Qualifications expire unnoticed. Workers get turned away. Timesheets are rebuilt from messages. Site managers spend mornings processing people instead of managing sites. And when an audit or incident happens, somebody has to prove what happened from whatever paperwork they can find. Tedwren turns that fragmented process into a live workforce record.

#### Two products
Reuse the Subcontractor and Main Contractor feature summaries verbatim from Sections 6.2 and 6.3 in condensed card form — headline, three-line promise, six bullet features, “Explore →” link. Do not fork the copy; both should be a single CMS entry rendered short-form here and long-form on the dedicated page, so the two never drift out of sync.

#### Differentiators (four cards)

| Card | Copy |
| --- | --- |
| Nothing to install | Workers use their own phone through the browser. No app needs to be downloaded just to sign in or complete an induction. |
| Works beyond the site gate | Tedwren is designed for traditional construction sites and dispersed schemes such as retrofit and planned-maintenance programmes where there may be no gate, cabin or QR code at the property. |
| Evidence, not just records | Attendance, qualification checks, inductions and decisions create an audit trail designed to show what happened, when and why. |
| Information moves without another account | Subcontractors can send compliance evidence directly to clients. The recipient does not need to create a Tedwren account just to view it. |

“Works beyond the site gate” is the strongest single differentiator in the whole spec — no mainstream competitor addresses dispersed/retrofit sites. Consider giving it a fifth visual treatment on Home (e.g. a small diagram) rather than a plain card, precisely because the market has no equivalent.

#### How it works (five steps, visual)

| # | Step | Copy |
| --- | --- | --- |
| 1 | Add the worker | The company or worker enters their details and qualifications. |
| 2 | Keep compliance current | Tedwren tracks qualifications, cards, insurance and other requirements and warns before they expire. |
| 3 | Prepare before arrival | Where the contractor uses Tedwren, workers can complete onboarding and site induction before their first day. |
| 4 | Record attendance | Workers sign in and out from their phone with location verification. |
| 5 | Keep the evidence | Attendance, inductions, compliance status and decisions remain searchable when they're needed later. |


#### Trust strip + closing CTA
Insert the Trust Strip component (5.3) followed by a closing CTA band: headline restating the outcome, Book a demo button.

### 6.2 Subcontractor landing page — /subcontractors
Treat this as the primary sales landing page at launch — the subcontractor product ships first and is the easier sale, so most paid traffic and referral links should land here, not on Home.

#### Hero
H1
Keep every operative site-ready.
Supporting copy
Know who worked where and for how long, keep qualifications current and send a complete compliance pack to any contractor in seconds.
Microcopy
Built for specialist subcontractors managing roughly 10–100 operatives across multiple sites.
CTA
Book a demo

#### Pain section
H2
Your workforce information shouldn't live in six different places.
Scenario
A client needs evidence for eight operatives starting Monday. Someone finds the cards. Someone checks the spreadsheet. Someone looks for the insurance certificate. Someone works out whether anything has expired. Then somebody builds another Word document containing information you've already supplied before. Tedwren keeps it together.

#### Features

| Feature | Copy |
| --- | --- |
| One workforce register | Workers, trades, cards, qualifications and current status in one place. |
| Never discover an expiry at the gate | Automatic warnings before worker qualifications or company documents expire. |
| Know where your people actually worked | Location-verified sign-in and sign-out creates searchable attendance across your sites. |
| Turn attendance into timesheets | Recorded hours roll into weekly digital timesheets ready for review, approval and export. |
| Send compliance evidence in seconds | Select the crew. Generate the pack. Send a web link, PDF or ZIP. The recipient doesn't need a Tedwren account, and the evidence is fixed at the point it was sent, creating a record of exactly what was supplied. |
| Company documents, not just worker documents | Employer's and public liability insurance, professional indemnity and accreditations sit in the same compliance pack alongside worker records. |


> **DEV NOTE:** The “company documents” feature is in the underlying spec (subcontractor MVP) but was missing from the earlier content draft — add it, it's a real gap the product closes that competitors ignore.

#### Add-on mention
One line, not a full section: “Add live CSCS card verification when you're ready — card capture, reading and manual checking are included from day one.” Keep this understated; it is priced as an add-on, not a headline feature (see Pricing).

#### Closing CTA
Book a demo, plus a secondary line: “Or start an 8–12 week pilot on up to three live sites — see Pricing.”

### 6.3 Main Contractor landing page — /main-contractors

#### Hero
H1
Know who's on site. Know they're compliant. Know why anyone was blocked.
Supporting copy
Get workers site-ready before arrival, manage digital inductions and maintain a defensible record of workforce compliance across every site.
CTA
Book a demo

#### Main proposition
H2
The induction shouldn't start at 7am at the gate.
Body
Send onboarding before the worker arrives. Workers complete their details, upload qualifications and complete the site induction from their own phone. When they arrive, Tedwren checks the information required by that site and records the outcome.

#### Features

| Feature | Copy |
| --- | --- |
| Pre-arrival onboarding | Get worker details and compliance evidence before the first shift. |
| Digital inductions | Video or document content, questions and acknowledgement — managed by your team without requiring developer changes. |
| Site-entry decisions | Check whether the worker is registered, inducted and holds the required current qualifications. If they're blocked, record exactly why. |
| Live workforce | See who's currently on site by worker, employer and trade. |
| Competency cover | Know whether required people such as first aiders or fire marshals are physically on site right now. |
| Muster | Get an immediate workforce list for emergency response. |
| Commercial attendance | Review attendance by subcontractor and valuation period rather than chasing site managers for another spreadsheet. |
| Audit trail | Retrieve the history of a worker, induction, attendance record or entry decision when an audit or incident arrives. |


#### Retrofit / dispersed-site section
Give this a substantial, distinct section on this page — not a footnote — and consider its own SEO landing page once there is enough demand signal to justify it.
H2
Workforce management when there isn't a site gate.
Body
Not every construction site has a turnstile, cabin or QR code. Retrofit, social housing, planned maintenance and dispersed-property programmes can involve hundreds of occupied properties, with operatives travelling directly to individual addresses. Tedwren is being designed for that environment too. Workers can confirm attendance from their own phone without requiring hardware or something physically attached to the property, while individual locations remain grouped under the overall scheme.
Line
One programme. Hundreds of properties. One workforce view.

#### Closing CTA
Book a demo, plus a secondary line pointing to the paid pilot: up to three live sites, 8–12 weeks — see Pricing.

### 6.4 Worker Passport — /worker-passport
Different register from the two B2B pages. The buyer is an individual worker paying for themselves, not a company — write shorter sentences, less industry jargon, and be explicit about price and control from the first screen.

#### Hero
H1
Your qualifications. Your work record. One place.
Supporting copy
Keep your construction cards, tickets, certificates and training together, get warned before they expire, and share what you need when somebody asks for it.
Price line
£10 per year. No employer required. Your record belongs with you.
CTA
Get your Worker Passport

> **DEV NOTE:** The Worker Passport PRD sets the price at £10/worker/year, billed annually. The Pricing & GTM strategy doc references £12/year in one place. Confirm the correct figure with the commercial lead before this goes live — do not launch with two different prices live on different pages.

#### Benefits

| Benefit | Copy |
| --- | --- |
| Keep everything together | CSCS, ECS, CPCS, EUSR, IPAF, PASMA, NPORS, first aid, training certificates and other credentials. |
| Know before something expires | Get reminders before a qualification or ticket lapses. |
| Share what you choose | Send selected credentials without making the recipient create an account. |
| Use it when changing jobs | Your passport isn't tied to one employer. |
| Get through induction faster | Where a contractor uses Tedwren, your existing information can pre-fill parts of the induction for you to confirm. |
| Never locked out for non-payment | If you stop paying, your record goes read-only — you keep access to everything and can export it at any time. It is never hidden, locked or deleted. |


> **DEV NOTE:** The “never locked out” benefit (Rule W2 in the Worker Passport PRD) was missing from the earlier draft and matters for consumer trust — add it. It's also a legally relevant point given this is a consumer contract with cancellation rights, not a B2B SaaS agreement.

#### Critical positioning restriction
Never describe Worker Passport as an alternative to, replacement for, or competitor of the CSCS Digital Skills Passport / My CSCS, in any copy, ad, or metadata (including page titles and meta descriptions — see Section 10). This is a hard licensing constraint tied to CSCS Smart Check access, not a brand-safety preference. See Section 8.2.

#### Sign-up flow (product, but the marketing page must set expectations correctly)
- Consumer checkout, not a B2B enquiry form — collect payment (card) directly on this flow via the payment provider (see 12.8), not through a sales conversation.
- Before payment, show in plain language: what a share exposes, what revocation does and does not do, and what happens to the record if the worker stops paying (PRD Rule W7 — informed consent, in plain words, at the point it matters).
- No app-install requirement anywhere in the flow — works in a phone browser end-to-end (PRD Rule W1).
- Standard UK consumer-contract elements: 14-day cancellation right disclosure, clear ongoing-annual-billing statement, and a working cancellation route — flag to legal before launch, see Section 13.

### 6.5 Pricing — /pricing
Not in the earlier content draft at all. A pricing page is worth adding: several credible competitors (SitePass, AttendIQ) win partly on price transparency against opaque “quote only” incumbents like HammerTech, MSite and Intasite. Recommend publishing indicative bands rather than hiding behind a form.

> **DEV NOTE:** Every number on this page must come from a config/CMS field, not be hardcoded — the commercial strategy explicitly says prices are not locked and will move after the first customer conversations. A hardcoded price is a support ticket and a trust problem the day it changes.
H1
Simple, transparent pricing.
Intro
Priced around how you actually work — by operative for subcontractors, by active site for main contractors. No per-seat games, no mandatory annual lock-in to see a number.

#### Subcontractor pricing

| Plan | Active operatives | Annual | Monthly (flexible) |
| --- | --- | --- | --- |
| Starter | 1–15 | £1,188/yr | £119/mo |
| Growth | 16–50 | £2,388/yr | £239/mo |
| Scale | 51–100 | £4,188/yr | £419/mo |
| Scale Plus | 101–250 | £7,188/yr | £719/mo |
| Enterprise | 251+ | Custom | Custom |

Sites are unlimited and free to record on the subcontractor product — charging per site would encourage under-recording, which weakens the attendance evidence the product exists to produce. State this explicitly on the page as a trust point, not just a footnote.

#### Main contractor pricing

| Portfolio size | Price |
| --- | --- |
| Up to 2 active sites | £399/month minimum |
| Sites 3–5 | +£179 per additional site/month |
| Sites 6–15 | +£149 per additional site/month |
| Sites 16–30 | +£129 per additional site/month |
| 31+ sites | Custom enterprise agreement |

A dispersed scheme (retrofit, planned maintenance, social housing) made up of many individual properties counts as one site, not one per property — state this plainly; it is the single fact that makes the product buyable in a segment with no real competition.

#### Worker Passport
£10 per year, paid by the worker. No employer sign-up needed. [Confirm final figure — see 6.4 dev note.]

#### Pilot

| Term | Detail |
| --- | --- |
| Price | £1,500 |
| Length | 8–12 weeks |
| Scope | Up to three live sites |
| Conditions | Named internal champion, weekly review, agreed success measures |
| Conversion | Fee credited against onboarding if an annual contract is signed promptly |


#### FAQ-style clarifiers on this page
- “What counts as an active operative / active site?” — answer plainly: leavers aren't billed, unused sites aren't billed.
- “What if I go over my band?” — a 10% buffer applies before overage pricing kicks in, and you'll be notified and offered the next tier if it's cheaper.
- Closing CTA: Book a demo to get an exact quote for your operation.

### 6.6 Security & Trust — /security
The earlier draft folded this into a recurring homepage section. It deserves its own page too, because procurement, H&S managers and IT/DPO reviewers specifically go looking for this before a contract is signed — give them a stable URL to send internally.
H1
Built for records that matter.
Intro
Construction workforce information isn't ordinary SaaS data. Tedwren is designed around clear audit trails, historic records that aren't silently overwritten, explicit qualification status, worker-controlled sharing, recorded overrides, location verification, data separation between companies, exportability and UK GDPR principles.

| Area | What we can say now |
| --- | --- |
| Hosting & data residency | UK/EU data hosting — confirm exact provider/region before publishing (see Section 13). |
| Data protection | UK GDPR-aligned. Publish the ICO registration number once registered. |
| Audit trail | Every attendance record, induction, qualification check and site-entry decision is timestamped and retained, not overwritten. |
| Worker consent | Nothing in a Worker Passport is visible to any company without the worker's specific, revocable permission — there is no administrative override. |
| Evidence, not liability transfer | Tedwren provides evidence and workflow controls. It does not assume the contractor's or employer's legal compliance responsibilities. |
| Data on cancellation | Company data is retained under contract terms; a worker's Worker Passport is never deleted or locked for non-payment — it goes read-only and remains exportable. |

Deliberately avoid: “100% compliant”, “guarantees compliance”, “ensures legal compliance”, ISO/Cyber Essentials badges that don't exist yet. Add certifications here the moment they're real, not before.

### 6.7 About — /about
H1
Construction doesn't need more paperwork. It needs better evidence.
Body
Tedwren is building software for the point where workforce management, compliance and site attendance meet. Construction companies already collect enormous amounts of information about the people working for them. The problem is that it's fragmented between spreadsheets, photographs, PDFs, folders, apps and paper — and much of it is collected repeatedly. We're building a platform that makes that information useful: before somebody arrives, while they're working, and when somebody needs to prove what happened afterwards.
Follow with a short founder/team section. No “revolutionising construction”, no “cutting-edge AI-powered ecosystem” — the product is concrete enough to speak for itself.

### 6.8 FAQ — /faq
Not in the earlier draft. Worth adding to pre-empt the objections a buyer will otherwise raise on a sales call, and it captures long-tail search intent cheaply.

| Question | Short answer to draft from |
| --- | --- |
| Is this just induction software? | No — induction is one input into a wider decision. The product decides and evidences whether someone is site-ready, not just whether they watched a video. |
| How is this different from the CSCS Digital Skills Passport? | CSCS own the card and the verification, and Tedwren doesn't compete there. Tedwren is the workflow layer contractors and subcontractors use around qualifications, attendance and evidence — including CSCS credentials, but going far beyond a card wallet. |
| Do workers need to download an app? | No. Everything works in a phone browser from a link — sign-in, induction, sharing. No install, ever, is required for the essential flow. |
| Is my data secure and UK GDPR compliant? | Link to /security. |
| Can I try before I commit? | Yes — an 8–12 week paid pilot on up to three live sites. Link to /pricing. |
| How is pricing worked out? | Subcontractors by active operative band, main contractors by active managed site. Link to /pricing. |
| What happens to a Worker Passport if I stop paying? | It goes read-only. Nothing is deleted, hidden or locked. Link to /worker-passport. |
| Does Tedwren work on retrofit/dispersed sites with no gate? | Yes — this is a specific design goal, not an afterthought. Link to the retrofit section on /main-contractors. |


### 6.9 Book a Demo / Contact — /demo and /contact
Two separate, lightweight forms rather than one generic “contact us” — the intent is different and the routing should be too.

| Form | Fields | Routing |
| --- | --- | --- |
| Book a demo (/demo) | Name, work email, company, role, company type (Subcontractor / Main Contractor / Not sure), approx. headcount or sites, phone (optional), “Interested in a pilot” checkbox | CRM lead, sales-qualified queue; auto-confirmation email with calendar link |
| Contact (/contact) | Name, email, message, reason (General / Press / Partner / Support) | Routed by “reason” to the right inbox; no calendar link |


> **DEV NOTE:** Use a real calendar-booking integration (e.g. a scheduling tool embedded after form submit) rather than “we'll be in touch” — the GTM plan is founder-led sales for the main contractor product, so speed of first contact matters more than lead-scoring sophistication at this stage.

### 6.10 Partners / Affiliates — /partners
Not in the earlier content draft. This page exists to recruit the referral channel the commercial strategy names — business introducers, consultants, H&S advisers, payroll advisers, training providers and trade associations — plus existing customers referring peers. It is an application page, not a self-serve affiliate signup, for reasons set out in Section 7.

#### Hero
H1
Refer a subcontractor. Get paid when they become a customer.
Supporting copy
If you already work with specialist subcontractors — as a consultant, adviser, trainer, payroll provider or trade body — you're already talking to the people Tedwren helps most. Refer them, and earn a share of what they pay in their first year.
CTA
Apply to become a partner

#### Who this is for
- Business introducers and consultants working with construction subcontractors
- H&S advisers and payroll advisers with subcontractor clients
- Training providers and trade associations
- Existing Tedwren customers referring other subcontractors they know

#### Who this is not for
Site managers, or anyone else who controls or influences who gets access to a site, cannot take part in this programme under any circumstances — see Section 7.3. Don't soften this on the page; state it plainly so nobody wastes time applying.

#### How it works

| Step | What happens |
| --- | --- |
| 1. Apply | Tell us who you are and how you work with subcontractors. We review every application — this isn't an open, instant-signup scheme. |
| 2. Get your referral link | Approved partners get a unique tracked link and a simple dashboard showing referrals and payouts. |
| 3. Refer | Send your link to subcontractors who'd benefit. No cold-calling requirement, no minimum volume. |
| 4. Get paid | 20% of your referral's first-year subscription, paid once their payment has cleared. |


#### Terms, stated plainly on the page
- Commission: 20% of the referred subcontractor's first-year subscription revenue.
- Paid only after the customer's payment has cleared — not on sign-up.
- Subject to a 90-day clawback if the customer cancels or the payment is reversed within that window.
- Currently covers the subcontractor product only — main contractor referrals are handled case-by-case; direct interested parties to Contact.

#### Closing CTA
Apply to become a partner — short form, reviewed by the team, not instant approval.

## 7. Affiliate / referral partner programme
This section documents the commercial terms, legal constraints and technical build requirements behind the Partners page in 6.10. It's a separate section from the page copy because the constraints here matter more than the wording, and a developer building the referral-tracking logic needs them together in one place.

### 7.1 Commercial terms (from the Pricing & Go-to-Market strategy)

| Term | Detail |
| --- | --- |
| Who qualifies | Approved business introducers, consultants, H&S advisers, payroll advisers, training providers, trade associations, and customer-to-customer referrals. |
| Commission | 20% of the referred customer's first-year subcontractor subscription revenue. |
| Payment trigger | Paid only after the referred customer's funds have cleared — never on sign-up alone. |
| Clawback | 90-day clawback window if the customer cancels or a payment is reversed after commission has been paid. |
| Product scope | Explicitly modelled around the subcontractor product in the source strategy. Main contractor referrals are not currently priced into this scheme — treat as an open question, not an oversight (see Section 13). |
| Approval model | Applications are reviewed, not instant — this is a deliberate choice, not a launch shortcut. See 7.2. |


### 7.2 Why this is an approval-gated programme, not an open affiliate scheme
Most SaaS affiliate programmes are self-serve: anyone signs up, gets a link, and starts earning. That model doesn't fit here, and the site shouldn't imply otherwise.
- The commercial strategy names specific categories of approved partner (advisers, trainers, trade bodies, existing customers) rather than “anyone with a website” — the page and the sign-up flow should reflect a curated list, not an open funnel.
- Construction referral relationships carry real conflict-of-interest and procurement risk in a way a generic B2C affiliate scheme doesn't — see 7.3.
- An unreviewed, instantly-approved partner promoting the product with their own unapproved claims is a compliance-claims risk (Section 8.1) the team can't control after the fact.

> **DEV NOTE:** Build the application form to create a pending-review record, not an active affiliate account. Referral links and dashboard access should only activate after a human approval step — do not wire this to an automatic “approve on signup” flow, however tempting for launch speed.

### 7.3 Hard rule: never a site-management or site-access channel
This is a legal and ethical constraint pulled directly from the product specifications, not a style preference, and it applies to the whole affiliate programme, not just the Worker Passport product it was originally written about.
- Anyone who controls or influences site access — site managers, gate staff, induction staff — must never be recruited, incentivised, or paid to refer people into Tedwren, in either direction (subcontractors onto a site, or workers into a passport).
- Paying a person who controls who gets onto a site to also sign up the workforce they control creates a direct conflict of interest, compromises consent, and sits badly against a main contractor customer's own Bribery Act controls — this would actively damage the main-contractor sales motion the company depends on.
- The Partners page (6.10) and the application form must not target, advertise to, or accept applications from this group. If in doubt about a specific applicant's role, escalate rather than approve.
This rule is stated explicitly rather than just implied, because it's exactly the kind of shortcut a well-meaning growth idea (“let's incentivise site managers to get workers to sign up”) could reintroduce later without anyone realising it recreates a compliance problem the underlying product documents already ruled out.

### 7.4 Technical requirements
- Unique, trackable referral link or code per approved partner, with attribution persisting through the demo-booking or Worker Passport checkout flow (a partner should get credit even if the referred lead doesn't convert same-session).
- A partner dashboard (can be simple at launch) showing referrals sent, status, and commission paid/pending — doesn't need to be public-facing or self-service beyond this.
- Referral attribution and commission events should log to the same analytics/CRM stack as other lead sources (Section 11), tagged distinctly from organic and paid traffic so the channel's real performance is visible, not blended into “other.”
- Clawback handling (7.1) needs a way to reverse a pending or paid commission against a specific referral record — model this from the start rather than bolting it on after the first cancellation.
- The application form (6.10) should capture enough to support the approval decision in 7.2 — who the applicant is, how they work with subcontractors, and their relationship (if any) to any site or contractor — that last question exists specifically to catch the 7.3 conflict before it's approved.

## 8. Copy and compliance guardrails

### 8.1 Absolute compliance claims — never use
- “100% compliant” / “fully compliant”
- “guarantees compliance” / “ensures legal compliance”
- Anything implying Tedwren, rather than the employer or contractor, carries the legal duty
Tedwren records, checks, and evidences. It does not decide legal compliance on anyone's behalf. This distinction is deliberate in the underlying product specification and should hold in every piece of copy, including ads and social posts, not just the website.

### 8.2 The CSCS constraint
CSCS Smart Check API access is licensed to Tedwren strictly for use inside a main contractor customer's own site-access/induction system, under a tri-partite agreement. Because of this:
- Worker Passport must never be described as offering verification “on demand”, or as a rival/alternative to My CSCS / the CSCS Digital Skills Passport.
- Any verified status shown to a worker must be worded as a dated, attributed record of a check performed by a named contractor on a named date — never as something Tedwren itself verified.
- This restriction applies to page copy, meta descriptions, ad copy, App Store/Play Store listings, and any future press material — not just the Worker Passport page.

### 8.3 No fabricated social proof
Do not publish invented client logos, invented review scores, or vague “trusted by leading contractors” language before there are real customers to back it. See Section 9 for the honest alternative.

## 9. Handling trust signals before there are customers
At launch there are no live customers, so the site should not pretend otherwise. Recommended approach:
- Replace “testimonials” with founder-led credibility: who's building it, why, and what problem it solves — the About page is doing this job already.
- Build the testimonial/logo component now, empty, ready to populate the moment a pilot customer agrees to be named — don't ship it live with fake content in the meantime.
- Where the strategic review's competitor research supports a specific, factual claim (e.g. “card-check delays are a known cause of blocked starts industry-wide”), use that instead of manufactured urgency.
- Be specific about what's live vs. in development where it matters for trust — e.g. live CSCS verification is an add-on in progress, not shipped by default; say so rather than imply otherwise.

## 10. SEO and metadata

| Element | Requirement |
| --- | --- |
| Title tags | Unique per page, audience-led, under ~60 characters. E.g. “Workforce Compliance Software for Subcontractors | Tedwren” |
| Meta descriptions | Unique per page, outcome-led, under ~155 characters. Never mention CSCS in a way that could be read as a rivalry/replacement claim. |
| H1 usage | One H1 per page, matching the hero headline in Section 6. |
| Structured data | Organization schema sitewide; SoftwareApplication or Product schema on /pricing; FAQPage schema on /faq. |
| Canonical URLs | Set on every page; important once /subcontractors and /main-contractors start attracting paid traffic with tracking parameters. |
| Open Graph / social cards | Custom OG image per core page, not one generic sitewide image — the subcontractor and main contractor pages will be shared separately. |
| Sitemap.xml & robots.txt | Generated automatically from the CMS route list, not maintained by hand. |


## 11. Analytics, tagging and measurement
- GA4 (or an equivalent, ideally privacy-respecting analytics tool) behind the consent banner (5.5).
- UTM discipline on every outbound link used in sales, referral, and the compliance-pack footer (4.1), so pack-driven demo bookings are attributable — this is the whole point of the GTM “Stage two” mechanism and it's unmeasurable without tagging.
- Conversion events to track from day one: demo form submit, pilot checkbox ticked, Worker Passport checkout started, Worker Passport checkout completed, pricing page → demo click-through.
- No cross-site advertising pixels (Meta/LinkedIn Insight Tag etc.) fire without explicit consent category, separate from analytics consent.

## 12. Technical and platform requirements

### 12.1 Recommended approach
A headless CMS-backed marketing site (e.g. Next.js front end with a headless CMS such as Sanity, Contentful, or an equivalent the team already knows) is a better fit than a fully custom build or a rigid page builder, because:
- Product names, pricing figures, and feature copy are all explicitly unstable right now (Sections 2, 6.4, 6.5) — a CMS lets the founders update these without a deploy.
- The Subcontractor/Main Contractor feature cards need to render both short-form (Home) and long-form (dedicated page) from one source (6.1) — straightforward with structured content, painful with hardcoded HTML.
- Future growth (case studies, a resources section) is explicitly deferred but should not require a rebuild when it arrives.

### 12.2 Content model (indicative)
- ProductProfile: name (config key + display value), tagline, hero copy, feature list, pricing reference — one entry each for Subcontractor, Main Contractor, Worker Passport.
- PricingPlan: plan name, band description, annual price, monthly price, product reference — rendered on /pricing and pulled into any “from £x/month” mentions elsewhere so numbers never go stale in two places at once.
- FeatureCard, TrustPoint, FAQItem, Testimonial (schema ready, empty at launch — see Section 9): reusable content types, not one-off page blocks.

### 12.3 Accessibility
- WCAG 2.1 AA as the baseline — non-negotiable given the worker-facing product this markets is explicitly designed to be used by people on a basic phone browser, often outdoors, sometimes in bright light or with gloves on.
- Colour contrast, focus states, and tap-target sizing should be tested on the actual mobile breakpoint the workforce will use, not just desktop.

### 12.4 Performance
- Target Core Web Vitals “good” thresholds on mobile specifically — site visitors researching the product skew desktop/office, but Worker Passport sign-up and pack-viewer traffic (4.1) will skew heavily mobile, often on site Wi-Fi or 4G.
- Images optimised/responsive; no render-blocking third-party scripts above the fold.

### 12.5 Responsive breakpoints
Design mobile-first for /worker-passport and the compliance pack viewer chrome specifically — that's where real end-users, not office-based buyers, will land. Standard breakpoints (mobile ~360–480px, tablet ~768px, desktop ~1024px+) are adequate for the rest of the site.

### 12.6 Legal entity and Companies House data
Pull the registered company number and office address for the footer from the actual Companies House filing rather than typing it by hand, and keep it in the same config source as the naming rules in Section 2.

### 12.7 Cookie consent tooling
Use a proper consent-management platform (not a custom banner) so that consent state is logged and scripts genuinely don't fire pre-consent — see Section 5.5 and Section 11.

### 12.8 Worker Passport payments
- Stripe (or equivalent) for consumer card payments — annual billing in advance, per the commercial model.
- Checkout copy must include the informed-consent disclosures required by PRD Rule W7 (what a share exposes, what revocation does/doesn't do, what happens on non-payment) before payment is taken, not buried in Terms afterward.
- Standard UK consumer distance-selling disclosures (14-day cancellation right, clear recurring-billing statement) — flag to legal, see Section 13.

## 13. Open items for the founders — confirm before launch

| # | Item | Why it blocks sign-off, not the build |
| --- | --- | --- |
| 1 | Worker Passport price: £10/yr (PRD) vs £12/yr (Pricing & GTM strategy doc) | The two source documents disagree. Pick one before the page goes live — see 6.4. |
| 2 | Whether to publish exact pricing bands publicly at all, vs. a “from £x, talk to us for a quote” model | The commercial strategy explicitly says prices aren't locked yet and may move after the first five customer conversations. Publishing exact numbers is a trust-building move against opaque competitors, but it also means updating the site fast if prices move — confirm the team is comfortable with that trade-off. |
| 3 | Final wording of the Security & Trust page's specific claims (hosting region, ICO registration number, any certifications) | Needs real, current answers rather than placeholders before this page goes live — it's exactly the page a careful buyer will fact-check. |
| 4 | Worker Passport consumer-contract terms (cancellation rights, distance-selling disclosures) | The Worker Passport PRD flags this itself as a new legal surface (consumer contract terms, not the existing B2B terms) — needs sign-off from whoever handles Tedwren's legal position. |
| 5 | Product names for the two core products and Worker Passport | Not blocking the build (Section 2), but the site cannot go fully live under working titles indefinitely — confirm a rough timeline expectation. |
| 6 | Which social accounts actually exist at launch | Determines what appears in the footer (5.2) — avoid launching with dead icons. |
| 7 | Whether the affiliate/partner programme is ready to go public at launch, or should stay an unlinked application page for now | The commission terms exist in the GTM strategy but the programme has not run with a real partner yet — confirm the team wants to publicly recruit partners before that's been tested. See Section 7. |
| 8 | Vetting process and owner for partner applications | The programme is deliberately approval-only, not self-serve (Section 7.2) — someone needs to own reviewing applications before the form goes live, or submissions will sit unanswered. |


## 14. Pre-launch QA checklist
- Every price and product name on the site traces back to a CMS/config field — grep the codebase for hardcoded “£” and product-name strings before go-live.
- Cookie banner blocks all non-essential scripts until consent; reject-all works in one click.
- No absolute compliance claims anywhere (search the whole site for “guarantee”, “ensure”, “100%”, “compliant” and review each hit).
- No mention of CSCS anywhere reads as rivalry, replacement, or on-demand verification — review every CSCS mention against Section 8.2.
- Compliance pack viewer has no sign-up wall at any point (4.1).
- Mobile Core Web Vitals pass on /worker-passport and the pack viewer specifically.
- WCAG 2.1 AA automated + manual pass, focused on mobile tap targets and contrast.
- All three primary CTAs (5.4) route correctly and are tagged for analytics (11).
- Legal pages (Privacy, Cookies, Terms, Data Protection) reviewed by whoever handles Tedwren's legal position — not launched as placeholder text.
- Footer company number/address matches the current Companies House filing.
- Partners page application form does not accept self-serve signup, and nowhere on the site solicits site managers or anyone controlling site access as an affiliate (Section 7.3).
