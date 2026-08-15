-- Launch list (Web Content Spec §6.9): pre-launch interest emails captured on the marketing landing page —
-- SQL Server, commercial database. Idempotent. Not tenant-scoped (no account behind a signup yet). Deduped
-- on the lower-cased email.

IF OBJECT_ID(N'dbo.LaunchSignups', N'U') IS NULL
CREATE TABLE dbo.LaunchSignups
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_LaunchSignups PRIMARY KEY,
    Email       NVARCHAR(256)    NOT NULL,
    Source      NVARCHAR(64)     NULL,
    UtmSource   NVARCHAR(128)    NULL,
    UtmMedium   NVARCHAR(128)    NULL,
    UtmCampaign NVARCHAR(128)    NULL,
    CreatedUtc  DATETIMEOFFSET   NOT NULL,
    Notified    BIT              NOT NULL CONSTRAINT DF_LaunchSignups_Notified DEFAULT 0,
    NotifiedUtc DATETIMEOFFSET   NULL
);

-- Dedupe: one row per email (case-insensitive via a computed lower-cased key).
IF COL_LENGTH(N'dbo.LaunchSignups', N'EmailLower') IS NULL
ALTER TABLE dbo.LaunchSignups ADD EmailLower AS LOWER(Email) PERSISTED;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_LaunchSignups_Email')
CREATE UNIQUE INDEX UX_LaunchSignups_Email ON dbo.LaunchSignups (EmailLower);
