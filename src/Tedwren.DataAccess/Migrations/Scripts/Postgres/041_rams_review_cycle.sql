-- Phase 4 RAMS review: a live-version pointer on RAMS submissions (the approved version operatives sign, spec
-- Stage 3) and the subcontractor's RAMS submission family on its onboarding config (links its uploaded RAMS to
-- the review history / review cycle) — PostgreSQL. Idempotent, additive columns. Lowercase identifiers.

ALTER TABLE ramssubmissions ADD COLUMN IF NOT EXISTS islive boolean NOT NULL DEFAULT false;

ALTER TABLE subcontractoronboardingconfigs ADD COLUMN IF NOT EXISTS ramsfamilyid uuid NULL;

-- Idempotency marker for the RAMS re-review reminder engine (beyond PRD; behind the subcontractor-onboarding flag).
ALTER TABLE subcontractoronboardingconfigs ADD COLUMN IF NOT EXISTS lastramsreviewreminderutc timestamptz NULL;
