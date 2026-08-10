-- Console users (SF-20, SF-23, Q2) — SQL Server. Idempotent, re-runnable.

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
CREATE TABLE dbo.Users
(
    Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    CompanyId     UNIQUEIDENTIFIER NOT NULL,
    Name          NVARCHAR(256)    NOT NULL,
    Email         NVARCHAR(256)    NOT NULL,
    Role          INT              NOT NULL,
    Status        INT              NOT NULL,
    CreatedUtc    DATETIMEOFFSET   NOT NULL,
    LastActiveUtc DATETIMEOFFSET   NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Users_Company')
CREATE INDEX IX_Users_Company ON dbo.Users (CompanyId);

-- One account per email address.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Users_Email')
CREATE UNIQUE INDEX UX_Users_Email ON dbo.Users (Email);
