-- Company document library versioning / supersede chain (MC-27) — SQL Server. Idempotent.
-- A new version supersedes the prior document; the prior record is retained, never deleted.

IF COL_LENGTH('dbo.CompanyDocuments', 'Version') IS NULL
    ALTER TABLE dbo.CompanyDocuments ADD Version INT NOT NULL CONSTRAINT DF_CompanyDocuments_Version DEFAULT 1;

IF COL_LENGTH('dbo.CompanyDocuments', 'SupersedesDocumentId') IS NULL
    ALTER TABLE dbo.CompanyDocuments ADD SupersedesDocumentId UNIQUEIDENTIFIER NULL;

IF COL_LENGTH('dbo.CompanyDocuments', 'SupersededByDocumentId') IS NULL
    ALTER TABLE dbo.CompanyDocuments ADD SupersededByDocumentId UNIQUEIDENTIFIER NULL;
