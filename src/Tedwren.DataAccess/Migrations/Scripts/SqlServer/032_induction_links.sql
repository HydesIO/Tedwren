-- Shareable / tokenised induction links (MC-1/MC-2, UAT-018) — SQL Server. Idempotent.
-- Backports the table the Dapper InductionLinkRepository reads/writes; it was only ever created by the EF
-- migration AddInductionLinks, so a database provisioned by the startup MigrationRunner alone lacked it and the
-- induction-link flow failed at runtime with "Invalid object name 'InductionLinks'". Restores raw-script ↔ EF parity.

IF OBJECT_ID(N'dbo.InductionLinks', N'U') IS NULL
CREATE TABLE dbo.InductionLinks
(
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_InductionLinks PRIMARY KEY,
    Token           NVARCHAR(128)    NOT NULL,
    PasscodeHash    NVARCHAR(256)    NULL,
    CompanyId       UNIQUEIDENTIFIER NOT NULL,
    TemplateId      UNIQUEIDENTIFIER NOT NULL,
    Name            NVARCHAR(256)    NULL,
    ExpiresUtc      DATETIMEOFFSET   NOT NULL,
    Status          INT              NOT NULL,
    SessionId       UNIQUEIDENTIFIER NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    CreatedUtc      DATETIMEOFFSET   NOT NULL
);

-- The token is the opaque lookup key for the shared link; unique.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InductionLinks_Token')
CREATE UNIQUE INDEX IX_InductionLinks_Token ON dbo.InductionLinks (Token);
