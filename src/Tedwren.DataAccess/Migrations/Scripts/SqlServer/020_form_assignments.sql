-- Forms Library: form assignments to sites / operators / inductions (PRD-Phase 2, requirement 5) — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.FormAssignments', N'U') IS NULL
CREATE TABLE dbo.FormAssignments
(
    Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FormAssignments PRIMARY KEY,
    CompanyId            UNIQUEIDENTIFIER NOT NULL,
    FormTemplateFamilyId UNIQUEIDENTIFIER NOT NULL,
    FormName             NVARCHAR(256)    NOT NULL,
    Scope                INT              NOT NULL,   -- 0 Organisation, 1 Site, 2 Operator, 3 Induction
    SiteId               UNIQUEIDENTIFIER NULL,
    PersonId             UNIQUEIDENTIFIER NULL,
    InductionTemplateId  UNIQUEIDENTIFIER NULL,
    Schedule             INT              NOT NULL,   -- 0 AdHoc, 1 Daily, 2 Weekly, 3 Monthly
    FailureAlertEmail    NVARCHAR(256)    NULL,
    CreatedUtc           DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FormAssignments_Company')
CREATE INDEX IX_FormAssignments_Company ON dbo.FormAssignments (CompanyId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FormAssignments_Family')
CREATE INDEX IX_FormAssignments_Family ON dbo.FormAssignments (CompanyId, FormTemplateFamilyId);
