-- Sites, boundaries & dispersed schemes (SF-6, SF-14, SF-25, SF-26) — SQL Server. Idempotent, re-runnable.

IF OBJECT_ID(N'dbo.Sites', N'U') IS NULL
CREATE TABLE dbo.Sites
(
    Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Sites PRIMARY KEY,
    CompanyId            UNIQUEIDENTIFIER NOT NULL,
    Name                 NVARCHAR(256)    NOT NULL,
    Client               NVARCHAR(256)    NULL,
    Region               NVARCHAR(128)    NULL,
    Address              NVARCHAR(512)    NULL,
    HasCompound          BIT              NOT NULL,
    IsDispersed          BIT              NOT NULL,
    BoundaryLatitude     FLOAT            NULL,
    BoundaryLongitude    FLOAT            NULL,
    BoundaryRadiusMetres FLOAT            NULL,
    CreatedUtc           DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sites_Company')
CREATE INDEX IX_Sites_Company ON dbo.Sites (CompanyId);

IF OBJECT_ID(N'dbo.SiteProperties', N'U') IS NULL
CREATE TABLE dbo.SiteProperties
(
    Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SiteProperties PRIMARY KEY,
    SiteId               UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_SiteProperties_Sites REFERENCES dbo.Sites (Id),
    Address              NVARCHAR(512)    NOT NULL,
    Units                INT              NOT NULL,
    BoundaryLatitude     FLOAT            NOT NULL,
    BoundaryLongitude    FLOAT            NOT NULL,
    BoundaryRadiusMetres FLOAT            NOT NULL,
    CreatedUtc           DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SiteProperties_Site')
CREATE INDEX IX_SiteProperties_Site ON dbo.SiteProperties (SiteId);
