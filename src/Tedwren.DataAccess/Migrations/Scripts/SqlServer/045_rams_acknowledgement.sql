-- Operative RAMS acknowledgements (Subcontractor Onboarding spec Gate 5) — SQL Server. Idempotent.
-- One append-only row per signature: who signed which live RAMS version, when, and its re-sign deadline under the
-- MC's review cycle (ExpiresUtc, null = version-pinned). Scoped to the owning main contractor (R15). The site-entry
-- decision and the operative's own sign-in both read the latest row per (company, person, family) to clear Gate 5.

IF OBJECT_ID(N'dbo.RamsAcknowledgements', N'U') IS NULL
CREATE TABLE dbo.RamsAcknowledgements
(
    Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_RamsAcknowledgements PRIMARY KEY,
    CompanyId     UNIQUEIDENTIFIER NOT NULL,
    PersonId      UNIQUEIDENTIFIER NOT NULL,
    FamilyId      UNIQUEIDENTIFIER NOT NULL,
    Version       INT              NOT NULL,
    SignatureName NVARCHAR(256)    NOT NULL,
    SignedUtc     DATETIMEOFFSET   NOT NULL,
    ExpiresUtc    DATETIMEOFFSET   NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RamsAcknowledgements_CompanyId_PersonId_FamilyId')
CREATE INDEX IX_RamsAcknowledgements_CompanyId_PersonId_FamilyId ON dbo.RamsAcknowledgements (CompanyId, PersonId, FamilyId);
