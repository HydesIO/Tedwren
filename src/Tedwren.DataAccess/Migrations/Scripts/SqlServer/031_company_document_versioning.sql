-- Company document library: file reference + versioning / supersede chain (MC-27) — SQL Server. Idempotent ALTER.
-- The Dapper CompanyDocumentRepository reads/writes these columns, but they were only ever added by the EF
-- migrations (AddTradeOnboarding: FileReference; AddCompanyDocumentVersioning: Version + supersede links) — never
-- by the startup MigrationRunner scripts, so a database provisioned by the MigrationRunner alone lacked them and
-- document reads/writes failed with "Invalid column name 'FileReference'". This restores raw-script ↔ EF parity.
-- Each ALTER is guarded and self-contained (no reference to the new column in the same batch — see 023/031-commercial).

IF COL_LENGTH(N'dbo.CompanyDocuments', N'FileReference') IS NULL
ALTER TABLE dbo.CompanyDocuments ADD FileReference NVARCHAR(128) NULL;

IF COL_LENGTH(N'dbo.CompanyDocuments', N'Version') IS NULL
ALTER TABLE dbo.CompanyDocuments ADD Version INT NOT NULL CONSTRAINT DF_CompanyDocuments_Version DEFAULT 1;

IF COL_LENGTH(N'dbo.CompanyDocuments', N'SupersedesDocumentId') IS NULL
ALTER TABLE dbo.CompanyDocuments ADD SupersedesDocumentId UNIQUEIDENTIFIER NULL;

IF COL_LENGTH(N'dbo.CompanyDocuments', N'SupersededByDocumentId') IS NULL
ALTER TABLE dbo.CompanyDocuments ADD SupersededByDocumentId UNIQUEIDENTIFIER NULL;
