-- Qualification cards & competency (SF-5–SF-8, SF-10–SF-12) — SQL Server. Idempotent, re-runnable.

IF OBJECT_ID(N'dbo.QualificationTypes', N'U') IS NULL
CREATE TABLE dbo.QualificationTypes
(
    Id                    UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_QualificationTypes PRIMARY KEY,
    Name                  NVARCHAR(256)    NOT NULL,
    Category              NVARCHAR(128)    NULL,
    Issuer                NVARCHAR(128)    NULL,
    DefaultValidityMonths INT              NOT NULL,
    IsCscsVerifiable      BIT              NOT NULL,
    CreatedUtc            DATETIMEOFFSET   NOT NULL
);

IF OBJECT_ID(N'dbo.QualificationCards', N'U') IS NULL
CREATE TABLE dbo.QualificationCards
(
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_QualificationCards PRIMARY KEY,
    PersonId            UNIQUEIDENTIFIER NOT NULL,
    QualificationTypeId UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_QualificationCards_Types REFERENCES dbo.QualificationTypes (Id),
    CardNumber          NVARCHAR(128)    NULL,
    HolderName          NVARCHAR(256)    NULL,
    IssuedOn            DATE             NULL,
    ExpiresOn           DATE             NULL,
    CaptureSource       INT              NOT NULL,
    ImageReference      NVARCHAR(512)    NULL,
    VerificationState   INT              NOT NULL,
    NeedsReview         BIT              NOT NULL,
    ConfirmedBy         NVARCHAR(256)    NULL,
    ConfirmedUtc        DATETIMEOFFSET   NULL,
    SupersedesCardId    UNIQUEIDENTIFIER NULL,
    SupersededByCardId  UNIQUEIDENTIFIER NULL,
    CreatedUtc          DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QualificationCards_Person')
CREATE INDEX IX_QualificationCards_Person ON dbo.QualificationCards (PersonId);

IF OBJECT_ID(N'dbo.TradeQualificationRequirements', N'U') IS NULL
CREATE TABLE dbo.TradeQualificationRequirements
(
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TradeQualificationRequirements PRIMARY KEY,
    Trade               NVARCHAR(128)    NOT NULL,
    QualificationTypeId UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_TradeReq_Types REFERENCES dbo.QualificationTypes (Id)
);

-- SF-11: one requirement row per (trade, qualification type).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_TradeReq_Trade_Type')
CREATE UNIQUE INDEX UX_TradeReq_Trade_Type ON dbo.TradeQualificationRequirements (Trade, QualificationTypeId);
