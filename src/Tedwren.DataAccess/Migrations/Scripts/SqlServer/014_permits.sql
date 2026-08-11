-- Permits to work (Permits page) — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.Permits', N'U') IS NULL
CREATE TABLE dbo.Permits
(
    Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Permits PRIMARY KEY,
    CompanyId         UNIQUEIDENTIFIER NOT NULL,
    PermitType        NVARCHAR(128)    NOT NULL,
    SiteName          NVARCHAR(256)    NULL,
    ResponsiblePerson NVARCHAR(256)    NULL,
    ValidFrom         DATE             NULL,
    ValidTo           DATE             NULL,
    Description       NVARCHAR(MAX)    NULL,
    HighRisk          BIT              NOT NULL,
    RamsAttached      BIT              NOT NULL,
    Status            INT              NOT NULL,
    CreatedUtc        DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Permits_Company_Created')
CREATE INDEX IX_Permits_Company_Created ON dbo.Permits (CompanyId, CreatedUtc);
