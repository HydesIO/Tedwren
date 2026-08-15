-- GoCardless webhook events (Phase C): stored inbound events, deduped by GoCardless event id — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.WebhookEvents', N'U') IS NULL
CREATE TABLE dbo.WebhookEvents
(
    Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_WebhookEvents PRIMARY KEY,
    GoCardlessEventId NVARCHAR(64)     NOT NULL,
    ResourceType      NVARCHAR(64)     NOT NULL,
    Action            NVARCHAR(64)     NOT NULL,
    ResourceId        NVARCHAR(64)     NULL,
    Outcome           INT              NOT NULL,
    Detail            NVARCHAR(500)    NULL,
    RawJson           NVARCHAR(MAX)    NOT NULL,
    ReceivedUtc       DATETIMEOFFSET   NOT NULL,
    ProcessedUtc      DATETIMEOFFSET   NULL
);

-- Dedupe: one stored row per GoCardless event id.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_WebhookEvents_EventId')
CREATE UNIQUE INDEX UX_WebhookEvents_EventId ON dbo.WebhookEvents (GoCardlessEventId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WebhookEvents_Received')
CREATE INDEX IX_WebhookEvents_Received ON dbo.WebhookEvents (ReceivedUtc);
