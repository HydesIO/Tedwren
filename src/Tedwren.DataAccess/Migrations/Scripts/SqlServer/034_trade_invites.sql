-- Trade (subcontractor) onboarding invites (PRD-Phase 1) — SQL Server. Idempotent. Backports the table the Dapper
-- TradeInviteRepository reads/writes; it was only ever created by the EF migration AddTradeOnboarding, so a
-- MigrationRunner-only database lacked it and the trade-invite flow failed. Restores raw-script ↔ EF parity.

IF OBJECT_ID(N'dbo.TradeInvites', N'U') IS NULL
CREATE TABLE dbo.TradeInvites
(
    Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TradeInvites PRIMARY KEY,
    Token            NVARCHAR(128)    NOT NULL,
    PasscodeHash     NVARCHAR(256)    NULL,
    CompanyId        UNIQUEIDENTIFIER NOT NULL,
    InviterCompanyId UNIQUEIDENTIFIER NOT NULL,
    ContactName      NVARCHAR(256)    NULL,
    ContactEmail     NVARCHAR(256)    NULL,
    Status           INT              NOT NULL,
    ExpiresUtc       DATETIMEOFFSET   NOT NULL,
    SubmittedUtc     DATETIMEOFFSET   NULL,
    DecidedBy        NVARCHAR(256)    NULL,
    DecidedUtc       DATETIMEOFFSET   NULL,
    ReviewNote       NVARCHAR(1024)   NULL,
    CreatedByUserId  UNIQUEIDENTIFIER NULL,
    CreatedUtc       DATETIMEOFFSET   NOT NULL
);

-- The token is the opaque lookup key for the invite link; unique.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TradeInvites_Token')
CREATE UNIQUE INDEX IX_TradeInvites_Token ON dbo.TradeInvites (Token);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TradeInvites_InviterCompanyId')
CREATE INDEX IX_TradeInvites_InviterCompanyId ON dbo.TradeInvites (InviterCompanyId);
