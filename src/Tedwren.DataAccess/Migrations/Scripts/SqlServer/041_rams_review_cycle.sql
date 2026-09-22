-- Phase 4 RAMS review: a live-version pointer on RAMS submissions (the approved version operatives sign, spec
-- Stage 3) and the subcontractor's RAMS submission family on its onboarding config (links its uploaded RAMS to
-- the review history / review cycle) — SQL Server. Idempotent, additive columns.

IF COL_LENGTH('dbo.RamsSubmissions', 'IsLive') IS NULL
ALTER TABLE dbo.RamsSubmissions ADD IsLive BIT NOT NULL CONSTRAINT DF_RamsSubmissions_IsLive DEFAULT 0;

IF COL_LENGTH('dbo.SubcontractorOnboardingConfigs', 'RamsFamilyId') IS NULL
ALTER TABLE dbo.SubcontractorOnboardingConfigs ADD RamsFamilyId UNIQUEIDENTIFIER NULL;

-- Idempotency marker for the RAMS re-review reminder engine (beyond PRD; behind the subcontractor-onboarding flag).
IF COL_LENGTH('dbo.SubcontractorOnboardingConfigs', 'LastRamsReviewReminderUtc') IS NULL
ALTER TABLE dbo.SubcontractorOnboardingConfigs ADD LastRamsReviewReminderUtc DATETIMEOFFSET NULL;
