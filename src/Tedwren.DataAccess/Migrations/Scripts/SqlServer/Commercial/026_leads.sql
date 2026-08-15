-- Sales leads (Web Plan §7): the pipeline of prospective customers — SQL Server, commercial database.
-- Idempotent. Not tenant-scoped (no account behind a lead yet). Account/affiliate are soft references
-- (Guids into other databases), so there are no cross-database foreign keys. Money is a plain decimal.

IF OBJECT_ID(N'dbo.Leads', N'U') IS NULL
CREATE TABLE dbo.Leads
(
    Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Leads PRIMARY KEY,
    CompanyName        NVARCHAR(256)    NOT NULL,
    ContactName        NVARCHAR(160)    NULL,
    ContactEmail       NVARCHAR(256)    NULL,
    Location           NVARCHAR(160)    NULL,
    Model              INT              NOT NULL CONSTRAINT DF_Leads_Model DEFAULT 0,
    NumberOfSites      INT              NULL,
    EstimatedRevenue   DECIMAL(18, 2)   NULL,
    Status             INT              NOT NULL CONSTRAINT DF_Leads_Status DEFAULT 0,
    AccountManager     NVARCHAR(160)    NULL,
    CompanyNumber      NVARCHAR(32)     NULL,
    AffiliateId        UNIQUEIDENTIFIER NULL,
    ConvertedAccountId UNIQUEIDENTIFIER NULL,
    Source             NVARCHAR(64)     NULL,
    CreatedUtc         DATETIMEOFFSET   NOT NULL,
    UpdatedUtc         DATETIMEOFFSET   NOT NULL
);

-- Recently-active ordering and status filtering.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Leads_UpdatedUtc')
CREATE INDEX IX_Leads_UpdatedUtc ON dbo.Leads (UpdatedUtc DESC);
