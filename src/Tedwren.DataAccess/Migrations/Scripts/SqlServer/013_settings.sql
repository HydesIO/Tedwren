-- Per-company general settings (System Configuration), stored as one JSON row per company — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.CompanySettings', N'U') IS NULL
CREATE TABLE dbo.CompanySettings
(
    CompanyId    UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanySettings PRIMARY KEY,
    SettingsJson NVARCHAR(MAX)    NOT NULL,
    UpdatedUtc   DATETIMEOFFSET   NOT NULL
);
