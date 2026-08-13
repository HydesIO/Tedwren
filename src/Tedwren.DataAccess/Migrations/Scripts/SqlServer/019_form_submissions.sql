-- Forms Library: completed form submissions + captured files (PRD-Phase 2 checklist/inspection engine) — SQL Server. Idempotent.
-- Answers are stored as JSON; submissions are append-only evidence (R4/R10). Files are stored as blobs.

IF OBJECT_ID(N'dbo.FormSubmissions', N'U') IS NULL
CREATE TABLE dbo.FormSubmissions
(
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FormSubmissions PRIMARY KEY,
    CompanyId           UNIQUEIDENTIFIER NOT NULL,
    FormTemplateId      UNIQUEIDENTIFIER NOT NULL,
    FormTemplateVersion INT              NOT NULL,
    FormName            NVARCHAR(256)    NOT NULL,
    Scope               INT              NOT NULL,   -- 0 Organisation, 1 Site, 2 Operator, 3 Induction
    SiteId              UNIQUEIDENTIFIER NULL,
    PersonId            UNIQUEIDENTIFIER NULL,
    AnswersJson         NVARCHAR(MAX)    NOT NULL,
    Status              INT              NOT NULL,   -- 0 Draft, 1 Submitted, 2 Approved, 3 Rejected
    SubmittedUtc        DATETIMEOFFSET   NOT NULL,
    SubmittedBy         NVARCHAR(256)    NOT NULL,
    ReviewNote          NVARCHAR(MAX)    NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FormSubmissions_Company')
CREATE INDEX IX_FormSubmissions_Company ON dbo.FormSubmissions (CompanyId, SubmittedUtc);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FormSubmissions_Template')
CREATE INDEX IX_FormSubmissions_Template ON dbo.FormSubmissions (FormTemplateId);

IF OBJECT_ID(N'dbo.FormSubmissionFiles', N'U') IS NULL
CREATE TABLE dbo.FormSubmissionFiles
(
    Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FormSubmissionFiles PRIMARY KEY,
    CompanyId    UNIQUEIDENTIFIER NOT NULL,
    SubmissionId UNIQUEIDENTIFIER NOT NULL,
    FieldId      NVARCHAR(64)     NOT NULL,
    FileName     NVARCHAR(512)    NOT NULL,
    ContentType  NVARCHAR(128)    NOT NULL,
    Content      VARBINARY(MAX)   NOT NULL,
    UploadedUtc  DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_FormSubmissionFiles_Submission')
CREATE INDEX IX_FormSubmissionFiles_Submission ON dbo.FormSubmissionFiles (SubmissionId);
