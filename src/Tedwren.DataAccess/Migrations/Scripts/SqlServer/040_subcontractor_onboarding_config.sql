-- Subcontractor onboarding configuration (Subcontractor Onboarding spec Stage 1 / §4) — SQL Server. Idempotent.
-- One config per TradeInvite: the required-document headings (the Gate 1 set, stored as JSON), the access
-- period, SSSTS/SMSTS toggles, induction settings and the RAMS review cycle. Scoped to the inviting main
-- contractor (R15). Access period and RAMS review cycle are captured but not yet enforced (beyond PRD v6.4).

IF OBJECT_ID(N'dbo.SubcontractorOnboardingConfigs', N'U') IS NULL
CREATE TABLE dbo.SubcontractorOnboardingConfigs
(
    Id                     UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SubcontractorOnboardingConfigs PRIMARY KEY,
    InviterCompanyId       UNIQUEIDENTIFIER NOT NULL,
    SubcontractorCompanyId UNIQUEIDENTIFIER NOT NULL,
    TradeInviteId          UNIQUEIDENTIFIER NOT NULL,
    AccessPeriodMonths     INT              NOT NULL,
    RequiredDocumentsJson  NVARCHAR(MAX)    NOT NULL,
    SsstsRequired          BIT              NOT NULL,
    SmstsRequired          BIT              NOT NULL,
    InductionValidityDays  INT              NOT NULL,
    InductionPassMark      INT              NOT NULL,
    InductionAttemptLimit  INT              NOT NULL,
    RamsReviewCycleMonths  INT              NULL,
    CreatedUtc             DATETIMEOFFSET   NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SubcontractorOnboardingConfigs_SubcontractorCompanyId')
CREATE INDEX IX_SubcontractorOnboardingConfigs_SubcontractorCompanyId ON dbo.SubcontractorOnboardingConfigs (SubcontractorCompanyId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SubcontractorOnboardingConfigs_TradeInviteId')
CREATE INDEX IX_SubcontractorOnboardingConfigs_TradeInviteId ON dbo.SubcontractorOnboardingConfigs (TradeInviteId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SubcontractorOnboardingConfigs_InviterCompanyId')
CREATE INDEX IX_SubcontractorOnboardingConfigs_InviterCompanyId ON dbo.SubcontractorOnboardingConfigs (InviterCompanyId);
