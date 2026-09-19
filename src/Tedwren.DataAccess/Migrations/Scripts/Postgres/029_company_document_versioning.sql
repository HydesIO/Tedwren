-- Company document library versioning / supersede chain (MC-27) — PostgreSQL. Idempotent.
-- A new version supersedes the prior document; the prior record is retained, never deleted.

ALTER TABLE CompanyDocuments ADD COLUMN IF NOT EXISTS Version INT NOT NULL DEFAULT 1;
ALTER TABLE CompanyDocuments ADD COLUMN IF NOT EXISTS SupersedesDocumentId UUID NULL;
ALTER TABLE CompanyDocuments ADD COLUMN IF NOT EXISTS SupersededByDocumentId UUID NULL;
