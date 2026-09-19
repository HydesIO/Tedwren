-- Hazard / near-miss reports and accident / incident records (PRD §8.2) — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.HazardReports', N'U') IS NULL
CREATE TABLE dbo.HazardReports
(
    Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_HazardReports PRIMARY KEY,
    CompanyId      UNIQUEIDENTIFIER NOT NULL,
    Reference      NVARCHAR(64)     NOT NULL,
    Kind           INT              NOT NULL,
    Description    NVARCHAR(2000)   NOT NULL,
    Location       NVARCHAR(256)    NULL,
    Latitude       FLOAT            NULL,
    Longitude      FLOAT            NULL,
    PhotoReference NVARCHAR(256)    NULL,
    Severity       INT              NOT NULL,
    Category       NVARCHAR(128)    NULL,
    Status         INT              NOT NULL,
    AssignedTo     NVARCHAR(256)    NULL,
    ReportedBy     NVARCHAR(256)    NOT NULL,
    ReportedUtc    DATETIMEOFFSET   NOT NULL,
    ClosedUtc      DATETIMEOFFSET   NULL,
    ClosureNote    NVARCHAR(2000)   NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HazardReports_Company_Reported')
CREATE INDEX IX_HazardReports_Company_Reported ON dbo.HazardReports (CompanyId, ReportedUtc);

IF OBJECT_ID(N'dbo.IncidentReports', N'U') IS NULL
CREATE TABLE dbo.IncidentReports
(
    Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_IncidentReports PRIMARY KEY,
    CompanyId         UNIQUEIDENTIFIER NOT NULL,
    Reference         NVARCHAR(64)     NOT NULL,
    Kind              INT              NOT NULL,
    Description       NVARCHAR(2000)   NOT NULL,
    Location          NVARCHAR(256)    NULL,
    OccurredUtc       DATETIMEOFFSET   NOT NULL,
    InjuredPersonName NVARCHAR(256)    NULL,
    InjuryDetail      NVARCHAR(512)    NULL,
    Severity          INT              NOT NULL,
    ImmediateCause    NVARCHAR(2000)   NULL,
    RootCause         NVARCHAR(2000)   NULL,
    CorrectiveActions NVARCHAR(2000)   NULL,
    Status            INT              NOT NULL,
    RiddorReportable  BIT              NOT NULL,
    RiddorCategory    NVARCHAR(128)    NULL,
    ReportedBy        NVARCHAR(256)    NOT NULL,
    ReportedUtc       DATETIMEOFFSET   NOT NULL,
    InvestigatedBy    NVARCHAR(256)    NULL,
    ClosedUtc         DATETIMEOFFSET   NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_IncidentReports_Company_Reported')
CREATE INDEX IX_IncidentReports_Company_Reported ON dbo.IncidentReports (CompanyId, ReportedUtc);
