-- Company documents slice (SUB-4: insurances, accreditations, policies) — SQL Server.
-- Idempotent so the runner can re-apply safely.

IF OBJECT_ID(N'dbo.CompanyDocuments', N'U') IS NULL
CREATE TABLE dbo.CompanyDocuments
(
    Id         UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CompanyDocuments PRIMARY KEY,
    CompanyId  UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_CompanyDocuments_Companies REFERENCES dbo.Companies (Id),
    Name       NVARCHAR(256)    NOT NULL,
    Type       NVARCHAR(128)    NOT NULL,
    ExpiresOn  DATE             NULL,
    Reference  NVARCHAR(128)    NULL,
    CreatedUtc DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyDocuments_CompanyId')
CREATE INDEX IX_CompanyDocuments_CompanyId ON dbo.CompanyDocuments (CompanyId);
