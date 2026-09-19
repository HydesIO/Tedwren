-- Hand-arm vibration (HAVs) exposure records (PRD §8.2) — SQL Server. Idempotent.
-- Tool usages are held as JSON; the daily A(8)/points/band are derived at read time, never stored.

IF OBJECT_ID(N'dbo.HavsExposureRecords', N'U') IS NULL
CREATE TABLE dbo.HavsExposureRecords
(
    Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_HavsExposureRecords PRIMARY KEY,
    CompanyId     UNIQUEIDENTIFIER NOT NULL,
    PersonName    NVARCHAR(256)    NOT NULL,
    ExposureDate  DATE             NOT NULL,
    ToolUsagesJson NVARCHAR(MAX)   NOT NULL,
    RecordedBy    NVARCHAR(256)    NOT NULL,
    RecordedUtc   DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HavsExposureRecords_Company_Recorded')
CREATE INDEX IX_HavsExposureRecords_Company_Recorded ON dbo.HavsExposureRecords (CompanyId, RecordedUtc);
