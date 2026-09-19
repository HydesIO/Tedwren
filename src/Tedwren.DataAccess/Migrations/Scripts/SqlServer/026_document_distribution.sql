-- Document distribution & acknowledgement (PRD §8.2) — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.DocumentDistributions', N'U') IS NULL
CREATE TABLE dbo.DocumentDistributions
(
    Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_DocumentDistributions PRIMARY KEY,
    CompanyId     UNIQUEIDENTIFIER NOT NULL,
    Title         NVARCHAR(256)    NOT NULL,
    Category      NVARCHAR(128)    NULL,
    Audience      NVARCHAR(256)    NULL,
    FileReference NVARCHAR(256)    NULL,
    SentBy        NVARCHAR(256)    NOT NULL,
    SentUtc       DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentDistributions_Company_Sent')
CREATE INDEX IX_DocumentDistributions_Company_Sent ON dbo.DocumentDistributions (CompanyId, SentUtc);

IF OBJECT_ID(N'dbo.DocumentAcknowledgements', N'U') IS NULL
CREATE TABLE dbo.DocumentAcknowledgements
(
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_DocumentAcknowledgements PRIMARY KEY,
    DistributionId  UNIQUEIDENTIFIER NOT NULL,
    CompanyId       UNIQUEIDENTIFIER NOT NULL,
    RecipientName   NVARCHAR(256)    NOT NULL,
    PersonId        UNIQUEIDENTIFIER NULL,
    AcknowledgedUtc DATETIMEOFFSET   NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentAcknowledgements_Distribution')
CREATE INDEX IX_DocumentAcknowledgements_Distribution ON dbo.DocumentAcknowledgements (DistributionId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DocumentAcknowledgements_Company')
CREATE INDEX IX_DocumentAcknowledgements_Company ON dbo.DocumentAcknowledgements (CompanyId);
