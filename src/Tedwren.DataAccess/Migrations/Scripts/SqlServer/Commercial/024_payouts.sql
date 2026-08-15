-- BACS payouts (Phase D): GoCardless settlement into the Tedwren creditor account — SQL Server. Idempotent.
-- Not tenant-scoped (Tedwren's own settlement). Deduped by GoCardless payout id. Money in minor units (pence).

IF OBJECT_ID(N'dbo.Payouts', N'U') IS NULL
CREATE TABLE dbo.Payouts
(
    Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Payouts PRIMARY KEY,
    GoCardlessPayoutId NVARCHAR(64)     NOT NULL,
    AmountPence        INT              NOT NULL,
    Currency           NVARCHAR(3)      NOT NULL,
    Status             INT              NOT NULL,
    Reference          NVARCHAR(140)    NULL,
    ArrivalDate        DATE             NULL,
    CreatedUtc         DATETIMEOFFSET   NOT NULL,
    UpdatedUtc         DATETIMEOFFSET   NOT NULL
);

-- Dedupe: one stored row per GoCardless payout id.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Payouts_GcPayout')
CREATE UNIQUE INDEX UX_Payouts_GcPayout ON dbo.Payouts (GoCardlessPayoutId);
