-- Sign-in / sign-out & attendance (SF-13–SF-19, R4, R11) — SQL Server. Idempotent, re-runnable.

-- SF-15: the per-site location policy (added to the Phase 11 Sites table).
IF COL_LENGTH(N'dbo.Sites', N'LocationPolicy') IS NULL
ALTER TABLE dbo.Sites ADD LocationPolicy INT NOT NULL CONSTRAINT DF_Sites_LocationPolicy DEFAULT 0;

IF OBJECT_ID(N'dbo.Attendance', N'U') IS NULL
CREATE TABLE dbo.Attendance
(
    Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Attendance PRIMARY KEY,
    PersonId         UNIQUEIDENTIFIER NOT NULL,
    SiteId           UNIQUEIDENTIFIER NOT NULL,
    PropertyId       UNIQUEIDENTIFIER NULL,
    Type             INT              NOT NULL,
    Outcome          INT              NOT NULL,
    Method           INT              NOT NULL,
    Latitude         FLOAT            NULL,
    Longitude        FLOAT            NULL,
    WithinBoundary   BIT              NULL,
    Reason           NVARCHAR(512)    NULL,
    CorrectsRecordId UNIQUEIDENTIFIER NULL,
    OccurredUtc      DATETIMEOFFSET   NOT NULL,
    CreatedUtc       DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Attendance_Person')
CREATE INDEX IX_Attendance_Person ON dbo.Attendance (PersonId, OccurredUtc);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Attendance_Site')
CREATE INDEX IX_Attendance_Site ON dbo.Attendance (SiteId, OccurredUtc);
