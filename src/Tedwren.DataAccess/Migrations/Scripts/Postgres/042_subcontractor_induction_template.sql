-- Phase 5a operative induction (Gate 4): link a subcontractor's onboarding config to the main contractor's
-- induction template the operatives must complete (spec Stage 4; the induction is always the MC's own, §6.1) —
-- PostgreSQL. Idempotent, additive column. Lowercase identifiers.

ALTER TABLE subcontractoronboardingconfigs ADD COLUMN IF NOT EXISTS inductiontemplateid uuid NULL;
