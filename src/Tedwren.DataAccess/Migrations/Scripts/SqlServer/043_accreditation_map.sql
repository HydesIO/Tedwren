-- Phase 5b accreditation / Gate 3: the SF-11 trade→accreditation map gains legal-mandatory / client-required
-- flags and org ownership; qualification types gain org ownership (Q21, customer-adjustable); and a card carries
-- the client id of the mobile capture that created it, so the operative app's at-least-once outbox upload is
-- idempotent. SQL Server. Idempotent, additive columns.

IF COL_LENGTH('dbo.TradeQualificationRequirements', 'LegalMandatory') IS NULL
ALTER TABLE dbo.TradeQualificationRequirements ADD LegalMandatory BIT NOT NULL CONSTRAINT DF_TradeQualReq_LegalMandatory DEFAULT 0;

IF COL_LENGTH('dbo.TradeQualificationRequirements', 'ClientRequired') IS NULL
ALTER TABLE dbo.TradeQualificationRequirements ADD ClientRequired BIT NOT NULL CONSTRAINT DF_TradeQualReq_ClientRequired DEFAULT 0;

IF COL_LENGTH('dbo.TradeQualificationRequirements', 'CompanyId') IS NULL
ALTER TABLE dbo.TradeQualificationRequirements ADD CompanyId UNIQUEIDENTIFIER NULL;

IF COL_LENGTH('dbo.QualificationTypes', 'CompanyId') IS NULL
ALTER TABLE dbo.QualificationTypes ADD CompanyId UNIQUEIDENTIFIER NULL;

IF COL_LENGTH('dbo.QualificationCards', 'CaptureClientId') IS NULL
ALTER TABLE dbo.QualificationCards ADD CaptureClientId UNIQUEIDENTIFIER NULL;
