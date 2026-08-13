-- Forms Library: customer-built, per-tenant form templates (PRD-Phase 2 checklist/inspection engine) — SQL Server. Idempotent.
-- Sections and fields are stored as JSON; templates are versioned and append-only (R4/R10/R16), grouped by FamilyId.

IF OBJECT_ID(N'dbo.FormTemplates', N'U') IS NULL
CREATE TABLE dbo.FormTemplates
(
    Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FormTemplates PRIMARY KEY,
    CompanyId    UNIQUEIDENTIFIER NOT NULL,
    FamilyId     UNIQUEIDENTIFIER NOT NULL,
    Name         NVARCHAR(256)    NOT NULL,
    Description  NVARCHAR(1024)   NULL,
    Version      INT              NOT NULL,
    Status       INT              NOT NULL,   -- 0 Draft, 1 Published, 2 Archived
    SectionsJson NVARCHAR(MAX)    NOT NULL,
    CreatedUtc   DATETIMEOFFSET   NOT NULL,
    UpdatedUtc   DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FormTemplates_Company')
CREATE INDEX IX_FormTemplates_Company ON dbo.FormTemplates (CompanyId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FormTemplates_Family')
CREATE INDEX IX_FormTemplates_Family ON dbo.FormTemplates (CompanyId, FamilyId);
