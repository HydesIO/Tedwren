-- Timesheets: first-class, stateful, approvable objects with append-only lines (SUB-7–SUB-12, R16) — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.Timesheets', N'U') IS NULL
CREATE TABLE dbo.Timesheets
(
    Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Timesheets PRIMARY KEY,
    CompanyId     UNIQUEIDENTIFIER NOT NULL,
    PersonId      UNIQUEIDENTIFIER NOT NULL,
    OperativeName NVARCHAR(256)    NOT NULL,
    WeekStart     DATE             NOT NULL,
    Status        INT              NOT NULL,
    SubmittedUtc  DATETIMEOFFSET   NULL,
    DecidedBy     NVARCHAR(256)    NULL,
    DecidedUtc    DATETIMEOFFSET   NULL,
    DecisionScope INT              NULL,
    ReturnReason  NVARCHAR(1024)   NULL,
    CreatedUtc    DATETIMEOFFSET   NOT NULL
);

-- One timesheet per (company, operative, week).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Timesheets_Company_Person_Week')
CREATE UNIQUE INDEX UX_Timesheets_Company_Person_Week ON dbo.Timesheets (CompanyId, PersonId, WeekStart);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Timesheets_Company_Week')
CREATE INDEX IX_Timesheets_Company_Week ON dbo.Timesheets (CompanyId, WeekStart);

IF OBJECT_ID(N'dbo.TimesheetEntries', N'U') IS NULL
CREATE TABLE dbo.TimesheetEntries
(
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TimesheetEntries PRIMARY KEY,
    TimesheetId     UNIQUEIDENTIFIER NOT NULL,
    WorkDate        DATE             NOT NULL,
    SiteId          UNIQUEIDENTIFIER NOT NULL,
    SiteName        NVARCHAR(256)    NOT NULL,
    Hours           DECIMAL(9, 2)    NOT NULL,
    CorrectsEntryId UNIQUEIDENTIFIER NULL,
    Author          NVARCHAR(256)    NULL,
    Reason          NVARCHAR(1024)   NULL,
    CreatedUtc      DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TimesheetEntries_Timesheet')
CREATE INDEX IX_TimesheetEntries_Timesheet ON dbo.TimesheetEntries (TimesheetId, CreatedUtc);
