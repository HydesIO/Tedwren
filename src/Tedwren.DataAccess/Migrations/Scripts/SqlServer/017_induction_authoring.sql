-- Induction template authoring: attempt limit, mandatory flag, media URL, site scope (MC-4/MC-15) — SQL Server. Idempotent.

IF COL_LENGTH('dbo.InductionTemplates', 'AttemptLimit') IS NULL
    ALTER TABLE dbo.InductionTemplates ADD AttemptLimit INT NOT NULL CONSTRAINT DF_InductionTemplates_AttemptLimit DEFAULT (3);

IF COL_LENGTH('dbo.InductionTemplates', 'Mandatory') IS NULL
    ALTER TABLE dbo.InductionTemplates ADD Mandatory BIT NOT NULL CONSTRAINT DF_InductionTemplates_Mandatory DEFAULT (1);

IF COL_LENGTH('dbo.InductionTemplates', 'MediaUrl') IS NULL
    ALTER TABLE dbo.InductionTemplates ADD MediaUrl NVARCHAR(1024) NULL;

IF COL_LENGTH('dbo.InductionTemplates', 'SiteId') IS NULL
    ALTER TABLE dbo.InductionTemplates ADD SiteId UNIQUEIDENTIFIER NULL;
