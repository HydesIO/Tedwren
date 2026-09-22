-- Phase 6 registers + one notification engine (SF-9 / SUB-4 / SUB-5): the expiry-warning idempotency log becomes
-- source-neutral so qualification cards, company documents (SUB-4) and inductions (MC-7) share it without colliding.
-- Adds source + subjectid, migrates the original card-only shape (003) by copying cardid into subjectid and retiring
-- cardid + its index, then rebuilds the uniqueness over (source, subjectid, stage, channel, recipient).
-- PostgreSQL. Idempotent, re-runnable. Lowercase identifiers.

ALTER TABLE expirynotifications ADD COLUMN IF NOT EXISTS source integer NOT NULL DEFAULT 0;
ALTER TABLE expirynotifications ADD COLUMN IF NOT EXISTS subjectid uuid NULL;

-- One-time migration from the card-only shape (003): backfill subjectid from cardid (source defaults to 0 = Card),
-- drop the old unique index and retire cardid. Guarded so the block is a no-op once cardid is gone.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'expirynotifications' AND column_name = 'cardid') THEN
        UPDATE expirynotifications SET subjectid = cardid WHERE subjectid IS NULL;
        DROP INDEX IF EXISTS ux_expirynotifications_unique;
        ALTER TABLE expirynotifications DROP COLUMN cardid;
    END IF;
END $$;

-- SF-9 (now source-neutral): a given warning (source + subject + stage + channel + recipient) is sent at most once.
CREATE UNIQUE INDEX IF NOT EXISTS ux_expirynotifications_sourcesubject
    ON expirynotifications (source, subjectid, stage, channel, recipient);
