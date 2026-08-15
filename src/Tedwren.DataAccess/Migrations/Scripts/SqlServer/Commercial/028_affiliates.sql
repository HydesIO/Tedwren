-- Affiliates (Web Plan §7): partners who introduce customers and earn profit-share commission — SQL Server,
-- commercial database. Idempotent. No raw bank details (only a payee reference). Rates are fractions (0.20).

IF OBJECT_ID(N'dbo.Affiliates', N'U') IS NULL
CREATE TABLE dbo.Affiliates
(
    Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Affiliates PRIMARY KEY,
    Name             NVARCHAR(160)    NOT NULL,
    Company          NVARCHAR(200)    NULL,
    ContactEmail     NVARCHAR(256)    NOT NULL,
    Status           INT              NOT NULL CONSTRAINT DF_Affiliates_Status DEFAULT 0,
    AccountManager   NVARCHAR(160)    NULL,
    PayeeReference   NVARCHAR(140)    NULL,
    AffiliateRatePct DECIMAL(9, 4)    NOT NULL CONSTRAINT DF_Affiliates_Rate DEFAULT 0,
    ProfitMarginPct  DECIMAL(9, 4)    NOT NULL CONSTRAINT DF_Affiliates_Margin DEFAULT 0,
    CreatedUtc       DATETIMEOFFSET   NOT NULL,
    UpdatedUtc       DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Affiliates_UpdatedUtc')
CREATE INDEX IX_Affiliates_UpdatedUtc ON dbo.Affiliates (UpdatedUtc DESC);
