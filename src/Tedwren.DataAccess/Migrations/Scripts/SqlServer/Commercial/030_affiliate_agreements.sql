-- Affiliate agreements (Web Plan §7): the token-gated terms an affiliate signs, and the countersigned PDF
-- captured at signing — SQL Server, commercial database. Idempotent. The PDF is served only via the
-- token-gated endpoint, never a permanent public URL (R9).

IF OBJECT_ID(N'dbo.AffiliateAgreements', N'U') IS NULL
CREATE TABLE dbo.AffiliateAgreements
(
    Id                    UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AffiliateAgreements PRIMARY KEY,
    AffiliateId           UNIQUEIDENTIFIER NOT NULL,
    Token                 NVARCHAR(64)     NOT NULL,
    Status                INT              NOT NULL CONSTRAINT DF_AffiliateAgreements_Status DEFAULT 0,
    TermsHtml             NVARCHAR(MAX)    NOT NULL,
    CountersignatoryName  NVARCHAR(160)    NOT NULL,
    CountersignatoryTitle NVARCHAR(160)    NULL,
    SignatureDataUrl      NVARCHAR(MAX)    NULL,
    SignedByName          NVARCHAR(160)    NULL,
    SignedUtc             DATETIMEOFFSET   NULL,
    PdfBytes              VARBINARY(MAX)   NULL,
    CreatedUtc            DATETIMEOFFSET   NOT NULL
);

-- The public token is the lookup key and must be unique.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_AffiliateAgreements_Token')
CREATE UNIQUE INDEX UX_AffiliateAgreements_Token ON dbo.AffiliateAgreements (Token);
