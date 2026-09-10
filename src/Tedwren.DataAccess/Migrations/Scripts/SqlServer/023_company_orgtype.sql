-- Company product discriminator (PRD §2; SF-22/SUB-24/MC-23/R18) — SQL Server. Idempotent.
-- OrgType is the typed product a company is on (0 = Subcontractor, 1 = MainContractor), stored beside the
-- deliberately-open free-text Type. Nullable so pre-existing companies simply have no product until backfilled.
-- The backfill runs via EXEC so it is compiled after the column exists (single-batch runner, no GO support).

IF COL_LENGTH(N'dbo.Companies', N'OrgType') IS NULL
    ALTER TABLE dbo.Companies ADD OrgType INT NULL;

EXEC(N'UPDATE dbo.Companies SET OrgType = 1
       WHERE OrgType IS NULL AND Type IS NOT NULL AND LOWER(REPLACE(Type, '' '', '''')) = N''maincontractor'';');

EXEC(N'UPDATE dbo.Companies SET OrgType = 0
       WHERE OrgType IS NULL AND Type IS NOT NULL AND LOWER(REPLACE(Type, '' '', '''')) = N''subcontractor'';');
