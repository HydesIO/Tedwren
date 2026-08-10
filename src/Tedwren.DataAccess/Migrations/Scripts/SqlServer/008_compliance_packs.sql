-- Compliance packs: fixed-at-send snapshots shared with account-free recipients (SUB-13–SUB-26, R7–R9) — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.CompliancePacks', N'U') IS NULL
CREATE TABLE dbo.CompliancePacks
(
    Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompliancePacks PRIMARY KEY,
    CompanyId          UNIQUEIDENTIFIER NOT NULL,
    Title              NVARCHAR(256)    NOT NULL,
    SiteId             UNIQUEIDENTIFIER NULL,
    SiteName           NVARCHAR(256)    NULL,
    FromDate           DATE             NULL,
    ToDate             DATE             NULL,
    CreatedBy          NVARCHAR(256)    NOT NULL,
    CreatedUtc         DATETIMEOFFSET   NOT NULL,
    ExpiresUtc         DATETIMEOFFSET   NOT NULL,
    Token              NVARCHAR(128)    NOT NULL,
    PasscodeHash       NVARCHAR(256)    NOT NULL,
    Status             INT              NOT NULL,
    SupersededByPackId UNIQUEIDENTIFIER NULL,
    SubjectsJson       NVARCHAR(MAX)    NULL
);

-- R9: the token is the recipient's only handle and must be unique.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CompliancePacks_Token')
CREATE UNIQUE INDEX UX_CompliancePacks_Token ON dbo.CompliancePacks (Token);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompliancePacks_Company')
CREATE INDEX IX_CompliancePacks_Company ON dbo.CompliancePacks (CompanyId, CreatedUtc);

IF OBJECT_ID(N'dbo.PackAccessEvents', N'U') IS NULL
CREATE TABLE dbo.PackAccessEvents
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PackAccessEvents PRIMARY KEY,
    PackId      UNIQUEIDENTIFIER NOT NULL,
    Kind        INT              NOT NULL,
    OccurredUtc DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PackAccessEvents_Pack')
CREATE INDEX IX_PackAccessEvents_Pack ON dbo.PackAccessEvents (PackId, Kind);
