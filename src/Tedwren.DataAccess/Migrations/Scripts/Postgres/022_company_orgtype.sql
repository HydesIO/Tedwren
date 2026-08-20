-- Company product discriminator (PRD §2; SF-22/SUB-24/MC-23/R18) — PostgreSQL. Idempotent.
-- orgtype is the typed product a company is on (0 = Subcontractor, 1 = MainContractor), stored beside the
-- deliberately-open free-text type. Nullable so pre-existing companies simply have no product until backfilled.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names on PostgreSQL.

ALTER TABLE companies ADD COLUMN IF NOT EXISTS orgtype int NULL;

UPDATE companies SET orgtype = 1
WHERE orgtype IS NULL AND type IS NOT NULL AND lower(replace(type, ' ', '')) = 'maincontractor';

UPDATE companies SET orgtype = 0
WHERE orgtype IS NULL AND type IS NOT NULL AND lower(replace(type, ' ', '')) = 'subcontractor';
