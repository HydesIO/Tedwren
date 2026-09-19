-- Plant & equipment register (Assets page, PRD §8.2) — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.Assets', N'U') IS NULL
CREATE TABLE dbo.Assets
(
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Assets PRIMARY KEY,
    CompanyId           UNIQUEIDENTIFIER NOT NULL,
    Name                NVARCHAR(256)    NOT NULL,
    AssetType           NVARCHAR(128)    NULL,
    SerialNumber        NVARCHAR(128)    NULL,
    Location            NVARCHAR(256)    NULL,
    OwnerName           NVARCHAR(256)    NULL,
    CertificationExpiry DATE             NULL,
    NextInspectionDue   DATE             NULL,
    Notes               NVARCHAR(MAX)    NULL,
    Status              INT              NOT NULL,
    CreatedUtc          DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Assets_Company_Created')
CREATE INDEX IX_Assets_Company_Created ON dbo.Assets (CompanyId, CreatedUtc);
