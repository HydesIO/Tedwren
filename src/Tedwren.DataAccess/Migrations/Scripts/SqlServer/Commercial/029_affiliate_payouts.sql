-- Affiliate payouts (Web Plan §7): commission owed/paid to an affiliate — SQL Server, commercial database.
-- Idempotent. Amount + status only; no bank details. Optionally tied to the account/deal (a lead).

IF OBJECT_ID(N'dbo.AffiliatePayouts', N'U') IS NULL
CREATE TABLE dbo.AffiliatePayouts
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AffiliatePayouts PRIMARY KEY,
    AffiliateId UNIQUEIDENTIFIER NOT NULL,
    LeadId      UNIQUEIDENTIFIER NULL,
    Amount      DECIMAL(18, 2)   NOT NULL,
    Currency    NVARCHAR(3)      NOT NULL CONSTRAINT DF_AffiliatePayouts_Ccy DEFAULT 'GBP',
    Status      INT              NOT NULL CONSTRAINT DF_AffiliatePayouts_Status DEFAULT 0,
    Reference   NVARCHAR(200)    NULL,
    CreatedUtc  DATETIMEOFFSET   NOT NULL,
    PaidUtc     DATETIMEOFFSET   NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AffiliatePayouts_Affiliate')
CREATE INDEX IX_AffiliatePayouts_Affiliate ON dbo.AffiliatePayouts (AffiliateId, CreatedUtc DESC);
