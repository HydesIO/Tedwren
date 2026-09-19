-- RAMS submission & approval (PRD §8.2) — SQL Server. Idempotent. Append-only (resubmission = new version).

IF OBJECT_ID(N'dbo.RamsSubmissions', N'U') IS NULL
CREATE TABLE dbo.RamsSubmissions
(
    Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RamsSubmissions PRIMARY KEY,
    CompanyId      UNIQUEIDENTIFIER NOT NULL,
    FamilyId       UNIQUEIDENTIFIER NOT NULL,
    Version        INT              NOT NULL,
    Reference      NVARCHAR(64)     NOT NULL,
    ContractorName NVARCHAR(256)    NOT NULL,
    Title          NVARCHAR(256)    NOT NULL,
    SiteId         UNIQUEIDENTIFIER NULL,
    SiteName       NVARCHAR(256)    NULL,
    FileReference  NVARCHAR(256)    NULL,
    Status         INT              NOT NULL,
    ReviewNote     NVARCHAR(MAX)    NULL,
    ReviewedBy     NVARCHAR(256)    NULL,
    ReviewedUtc    DATETIMEOFFSET   NULL,
    SubmittedUtc   DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RamsSubmissions_Company_Submitted')
CREATE INDEX IX_RamsSubmissions_Company_Submitted ON dbo.RamsSubmissions (CompanyId, SubmittedUtc);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RamsSubmissions_Company_Family')
CREATE INDEX IX_RamsSubmissions_Company_Family ON dbo.RamsSubmissions (CompanyId, FamilyId);
