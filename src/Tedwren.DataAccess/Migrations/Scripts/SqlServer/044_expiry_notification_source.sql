-- Phase 6 registers + one notification engine (SF-9 / SUB-4 / SUB-5): the expiry-warning idempotency log becomes
-- source-neutral so qualification cards, company documents (SUB-4) and inductions (MC-7) share it without colliding.
-- Adds Source + SubjectId, migrates the original card-only shape (003) by copying CardId into SubjectId and retiring
-- CardId + its index, then rebuilds the uniqueness over (Source, SubjectId, Stage, Channel, Recipient).
-- SQL Server. Idempotent, re-runnable.

IF COL_LENGTH('dbo.ExpiryNotifications', 'Source') IS NULL
ALTER TABLE dbo.ExpiryNotifications ADD Source INT NOT NULL CONSTRAINT DF_ExpiryNotifications_Source DEFAULT 0;

IF COL_LENGTH('dbo.ExpiryNotifications', 'SubjectId') IS NULL
ALTER TABLE dbo.ExpiryNotifications ADD SubjectId UNIQUEIDENTIFIER NULL;

-- One-time migration from the card-only shape (003): backfill SubjectId from CardId (Source defaults to 0 = Card),
-- drop the old unique index and retire CardId. Wrapped in dynamic SQL so the batch still compiles once CardId is gone.
IF COL_LENGTH('dbo.ExpiryNotifications', 'CardId') IS NOT NULL
BEGIN
    EXEC sp_executesql N'UPDATE dbo.ExpiryNotifications SET SubjectId = CardId WHERE SubjectId IS NULL;';

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ExpiryNotifications_Unique')
        EXEC sp_executesql N'DROP INDEX UX_ExpiryNotifications_Unique ON dbo.ExpiryNotifications;';

    EXEC sp_executesql N'ALTER TABLE dbo.ExpiryNotifications DROP COLUMN CardId;';
END

-- SF-9 (now source-neutral): a given warning (source + subject + stage + channel + recipient) is sent at most once.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ExpiryNotifications_SourceSubject')
CREATE UNIQUE INDEX UX_ExpiryNotifications_SourceSubject
    ON dbo.ExpiryNotifications (Source, SubjectId, Stage, Channel, Recipient);
