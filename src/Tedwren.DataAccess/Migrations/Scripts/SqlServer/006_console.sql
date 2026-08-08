-- Roles, audit, entitlements & decision store (SF-20, SF-22, SF-23, R10, Q2) — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.ModuleEntitlements', N'U') IS NULL
CREATE TABLE dbo.ModuleEntitlements
(
    Id         UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ModuleEntitlements PRIMARY KEY,
    CompanyId  UNIQUEIDENTIFIER NOT NULL,
    ModuleKey  NVARCHAR(64)     NOT NULL,
    Enabled    BIT              NOT NULL,
    UpdatedUtc DATETIMEOFFSET   NOT NULL
);

-- Q2: one entitlement per (company, module).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ModuleEntitlements_Company_Module')
CREATE UNIQUE INDEX UX_ModuleEntitlements_Company_Module ON dbo.ModuleEntitlements (CompanyId, ModuleKey);

IF OBJECT_ID(N'dbo.AuditEntries', N'U') IS NULL
CREATE TABLE dbo.AuditEntries
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AuditEntries PRIMARY KEY,
    CompanyId   UNIQUEIDENTIFIER NOT NULL,
    OccurredUtc DATETIMEOFFSET   NOT NULL,
    Actor       NVARCHAR(256)    NOT NULL,
    Action      NVARCHAR(512)    NOT NULL,
    Entity      NVARCHAR(256)    NOT NULL,
    Reference   NVARCHAR(128)    NULL,
    Category    NVARCHAR(128)    NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditEntries_Company_Occurred')
CREATE INDEX IX_AuditEntries_Company_Occurred ON dbo.AuditEntries (CompanyId, OccurredUtc);

IF OBJECT_ID(N'dbo.Decisions', N'U') IS NULL
CREATE TABLE dbo.Decisions
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Decisions PRIMARY KEY,
    PersonId    UNIQUEIDENTIFIER NOT NULL,
    SiteId      UNIQUEIDENTIFIER NOT NULL,
    Admitted    BIT              NOT NULL,
    OccurredUtc DATETIMEOFFSET   NOT NULL,
    ChecksJson  NVARCHAR(MAX)    NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Decisions_Person')
CREATE INDEX IX_Decisions_Person ON dbo.Decisions (PersonId, OccurredUtc);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Decisions_Site')
CREATE INDEX IX_Decisions_Site ON dbo.Decisions (SiteId, OccurredUtc);
