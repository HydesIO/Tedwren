-- Company document library: file reference + versioning / supersede chain (MC-27) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared repository SQL resolves. Previously only added by the EF migrations
-- (AddTradeOnboarding: FileReference; AddCompanyDocumentVersioning: Version + supersede links), so a database
-- provisioned by the startup MigrationRunner alone lacked them and document reads/writes failed.

ALTER TABLE companydocuments ADD COLUMN IF NOT EXISTS filereference varchar(128) NULL;
ALTER TABLE companydocuments ADD COLUMN IF NOT EXISTS version integer NOT NULL DEFAULT 1;
ALTER TABLE companydocuments ADD COLUMN IF NOT EXISTS supersedesdocumentid uuid NULL;
ALTER TABLE companydocuments ADD COLUMN IF NOT EXISTS supersededbydocumentid uuid NULL;
