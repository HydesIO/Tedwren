-- Direct-debit billing: mandates, payments & subscriptions (GoCardless; proposed PRD §9) — SQL Server. Idempotent.
-- Scoped by CompanyId (R15). Enum columns stored as INT. Money in minor units (pence).

IF OBJECT_ID(N'dbo.Mandates', N'U') IS NULL
CREATE TABLE dbo.Mandates
(
    Id                        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Mandates PRIMARY KEY,
    CompanyId                 UNIQUEIDENTIFIER NOT NULL,
    GoCardlessBillingRequestId NVARCHAR(64)    NULL,
    GoCardlessMandateId       NVARCHAR(64)     NULL,
    GoCardlessCustomerId      NVARCHAR(64)     NULL,
    Reference                 NVARCHAR(140)    NULL,
    BankName                  NVARCHAR(140)    NULL,
    AccountNumberEnding       NVARCHAR(8)      NULL,
    Status                    INT              NOT NULL,
    CreatedUtc                DATETIMEOFFSET   NOT NULL,
    UpdatedUtc                DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Mandates_Company')
CREATE INDEX IX_Mandates_Company ON dbo.Mandates (CompanyId, CreatedUtc);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Mandates_GcMandate')
CREATE INDEX IX_Mandates_GcMandate ON dbo.Mandates (GoCardlessMandateId);

IF OBJECT_ID(N'dbo.Payments', N'U') IS NULL
CREATE TABLE dbo.Payments
(
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
    CompanyId           UNIQUEIDENTIFIER NOT NULL,
    MandateId           UNIQUEIDENTIFIER NOT NULL,
    GoCardlessPaymentId NVARCHAR(64)     NULL,
    AmountPence         INT              NOT NULL,
    Currency            NVARCHAR(3)      NOT NULL,
    Description         NVARCHAR(500)    NULL,
    Status              INT              NOT NULL,
    ChargeDate          DATE             NULL,
    FailureReason       NVARCHAR(500)    NULL,
    CreatedUtc          DATETIMEOFFSET   NOT NULL,
    UpdatedUtc          DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Payments_Company')
CREATE INDEX IX_Payments_Company ON dbo.Payments (CompanyId, CreatedUtc);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Payments_GcPayment')
CREATE INDEX IX_Payments_GcPayment ON dbo.Payments (GoCardlessPaymentId);

IF OBJECT_ID(N'dbo.BillingSubscriptions', N'U') IS NULL
CREATE TABLE dbo.BillingSubscriptions
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_BillingSubscriptions PRIMARY KEY,
    CompanyId   UNIQUEIDENTIFIER NOT NULL,
    MandateId   UNIQUEIDENTIFIER NULL,
    MeterKey    NVARCHAR(64)     NOT NULL,
    BandKey     NVARCHAR(64)     NULL,
    Description  NVARCHAR(500)   NULL,
    Status      INT              NOT NULL,
    CreatedUtc  DATETIMEOFFSET   NOT NULL,
    UpdatedUtc  DATETIMEOFFSET   NOT NULL
);

-- One billing subscription per company.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BillingSubscriptions_Company')
CREATE UNIQUE INDEX UX_BillingSubscriptions_Company ON dbo.BillingSubscriptions (CompanyId);
