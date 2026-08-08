-- Digital inductions: configurable templates + operative sessions (MC-1–MC-7, MC-20, R5) — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.InductionTemplates', N'U') IS NULL
CREATE TABLE dbo.InductionTemplates
(
    Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_InductionTemplates PRIMARY KEY,
    CompanyId     UNIQUEIDENTIFIER NOT NULL,
    Name          NVARCHAR(256)    NOT NULL,
    ValidityDays  INT              NOT NULL,
    PassMark      INT              NOT NULL,
    StepsJson     NVARCHAR(MAX)    NOT NULL,
    QuestionsJson NVARCHAR(MAX)    NOT NULL   -- includes server-side answers; never sent to the device (R5)
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InductionTemplates_Company')
CREATE INDEX IX_InductionTemplates_Company ON dbo.InductionTemplates (CompanyId);

IF OBJECT_ID(N'dbo.InductionSessions', N'U') IS NULL
CREATE TABLE dbo.InductionSessions
(
    Id                    UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_InductionSessions PRIMARY KEY,
    TemplateId            UNIQUEIDENTIFIER NOT NULL,
    CompanyId             UNIQUEIDENTIFIER NOT NULL,
    PersonId              UNIQUEIDENTIFIER NOT NULL,
    PersonName            NVARCHAR(256)    NOT NULL,
    Status                INT              NOT NULL,
    StartedUtc            DATETIMEOFFSET   NOT NULL,
    CompletedUtc          DATETIMEOFFSET   NULL,
    CompletionReference   NVARCHAR(64)     NULL,
    ExpiresUtc            DATETIMEOFFSET   NULL,
    AttemptCount          INT              NOT NULL,
    LastScore             INT              NULL,
    CompletedStepsJson    NVARCHAR(MAX)    NULL,
    SignatureName         NVARCHAR(256)    NULL,
    ConsentGiven          BIT              NOT NULL,
    ResetReason           NVARCHAR(1024)   NULL,
    SupersededBySessionId UNIQUEIDENTIFIER NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InductionSessions_Company')
CREATE INDEX IX_InductionSessions_Company ON dbo.InductionSessions (CompanyId, StartedUtc);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InductionSessions_Person')
CREATE INDEX IX_InductionSessions_Person ON dbo.InductionSessions (CompanyId, TemplateId, PersonId, Status);
