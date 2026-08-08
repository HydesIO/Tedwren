-- Expiry warnings & job runs (SF-9, SF-21, SUB-5, R12) — SQL Server. Idempotent, re-runnable.

IF OBJECT_ID(N'dbo.ExpiryNotifications', N'U') IS NULL
CREATE TABLE dbo.ExpiryNotifications
(
    Id        UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ExpiryNotifications PRIMARY KEY,
    CardId    UNIQUEIDENTIFIER NOT NULL,
    Stage     INT              NOT NULL,
    Channel   INT              NOT NULL,
    Recipient NVARCHAR(256)    NOT NULL,
    SentUtc   DATETIMEOFFSET   NOT NULL
);

-- SF-9: a given warning (card + stage + channel + recipient) is sent at most once.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ExpiryNotifications_Unique')
CREATE UNIQUE INDEX UX_ExpiryNotifications_Unique
    ON dbo.ExpiryNotifications (CardId, Stage, Channel, Recipient);

IF OBJECT_ID(N'dbo.JobRuns', N'U') IS NULL
CREATE TABLE dbo.JobRuns
(
    Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_JobRuns PRIMARY KEY,
    JobName           NVARCHAR(128)    NOT NULL,
    StartedUtc        DATETIMEOFFSET   NOT NULL,
    FinishedUtc       DATETIMEOFFSET   NULL,
    Status            INT              NOT NULL,
    ItemsProcessed    INT              NOT NULL,
    NotificationsSent INT              NOT NULL,
    Error             NVARCHAR(2048)   NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_JobRuns_Name_Started')
CREATE INDEX IX_JobRuns_Name_Started ON dbo.JobRuns (JobName, StartedUtc DESC);
