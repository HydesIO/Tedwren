-- Self-service onboarding links (SF-4, SUB-2) + private image store (SF-5, R9) — SQL Server. Idempotent.

IF OBJECT_ID(N'dbo.OnboardingLinks', N'U') IS NULL
CREATE TABLE dbo.OnboardingLinks
(
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OnboardingLinks PRIMARY KEY,
    Token           NVARCHAR(128)    NOT NULL,
    PasscodeHash    NVARCHAR(256)    NULL,
    CompanyId       UNIQUEIDENTIFIER NOT NULL,
    Name            NVARCHAR(256)    NULL,
    Trade           NVARCHAR(128)    NULL,
    ExpiresUtc      DATETIMEOFFSET   NOT NULL,
    Status          INT              NOT NULL,
    PersonId        UNIQUEIDENTIFIER NULL,
    EngagementId    UNIQUEIDENTIFIER NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    CreatedUtc      DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_OnboardingLinks_Token')
CREATE UNIQUE INDEX UX_OnboardingLinks_Token ON dbo.OnboardingLinks (Token);

IF OBJECT_ID(N'dbo.StoredImages', N'U') IS NULL
CREATE TABLE dbo.StoredImages
(
    Id          UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_StoredImages PRIMARY KEY,
    ContentType NVARCHAR(128)    NOT NULL,
    Bytes       VARBINARY(MAX)   NOT NULL,
    CreatedUtc  DATETIMEOFFSET   NOT NULL
);
