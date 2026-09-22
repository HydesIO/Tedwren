-- Phase 5a operative induction (Gate 4): link a subcontractor's onboarding config to the main contractor's
-- induction template the operatives must complete (spec Stage 4; the induction is always the MC's own, §6.1) —
-- SQL Server. Idempotent, additive column.

IF COL_LENGTH('dbo.SubcontractorOnboardingConfigs', 'InductionTemplateId') IS NULL
ALTER TABLE dbo.SubcontractorOnboardingConfigs ADD InductionTemplateId UNIQUEIDENTIFIER NULL;
