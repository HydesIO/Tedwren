-- Induction template authoring: attempt limit, mandatory flag, media URL, site scope (MC-4/MC-15) — PostgreSQL. Idempotent.

ALTER TABLE InductionTemplates ADD COLUMN IF NOT EXISTS AttemptLimit INT NOT NULL DEFAULT 3;
ALTER TABLE InductionTemplates ADD COLUMN IF NOT EXISTS Mandatory BOOLEAN NOT NULL DEFAULT TRUE;
ALTER TABLE InductionTemplates ADD COLUMN IF NOT EXISTS MediaUrl VARCHAR(1024) NULL;
ALTER TABLE InductionTemplates ADD COLUMN IF NOT EXISTS SiteId UUID NULL;
