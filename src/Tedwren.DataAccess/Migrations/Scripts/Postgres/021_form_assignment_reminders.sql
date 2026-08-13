-- Forms Library: recurring-reminder idempotency marker on form assignments (PRD-Phase 2, R12) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.
-- lastreminderutc records when the recurring-reminder job last emailed a "form due" reminder, so a cadence is
-- reminded at most once per period.

ALTER TABLE formassignments ADD COLUMN IF NOT EXISTS lastreminderutc timestamptz NULL;
