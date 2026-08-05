-- Organisation slice (SF-1/SF-2/SF-3) — SQL Server. Idempotent so the runner can re-apply safely.

IF OBJECT_ID(N'dbo.Companies', N'U') IS NULL
CREATE TABLE dbo.Companies
(
    Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Companies PRIMARY KEY,
    Name               NVARCHAR(256)    NOT NULL,
    Type               NVARCHAR(128)    NULL,
    Trade              NVARCHAR(128)    NULL,
    RegistrationNumber NVARCHAR(64)     NULL,
    Address            NVARCHAR(512)    NULL,
    ContactName        NVARCHAR(256)    NULL,
    ContactEmail       NVARCHAR(256)    NULL,
    ContactPhone       NVARCHAR(64)     NULL,
    CreatedUtc         DATETIMEOFFSET   NOT NULL
);

IF OBJECT_ID(N'dbo.Persons', N'U') IS NULL
CREATE TABLE dbo.Persons
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Persons PRIMARY KEY,
    PhoneNumber NVARCHAR(32)     NOT NULL,
    CreatedUtc  DATETIMEOFFSET   NOT NULL
);

-- SF-1: one person per mobile number.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Persons_PhoneNumber')
CREATE UNIQUE INDEX UX_Persons_PhoneNumber ON dbo.Persons (PhoneNumber);

IF OBJECT_ID(N'dbo.Engagements', N'U') IS NULL
CREATE TABLE dbo.Engagements
(
    Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Engagements PRIMARY KEY,
    CompanyId         UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_Engagements_Companies REFERENCES dbo.Companies (Id),
    PersonId          UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_Engagements_Persons REFERENCES dbo.Persons (Id),
    Name              NVARCHAR(256)    NOT NULL,
    Trade             NVARCHAR(128)    NULL,
    InternalReference NVARCHAR(128)    NULL,
    Status            INT              NOT NULL,
    CreatedUtc        DATETIMEOFFSET   NOT NULL,
    ArchivedUtc       DATETIMEOFFSET   NULL
);

-- SF-2: one engagement per person per company.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Engagements_Company_Person')
CREATE UNIQUE INDEX UX_Engagements_Company_Person ON dbo.Engagements (CompanyId, PersonId);
