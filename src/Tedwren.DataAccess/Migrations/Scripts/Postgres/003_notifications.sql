-- Expiry warnings & job runs (SF-9, SF-21, SUB-5, R12) — PostgreSQL. Idempotent, re-runnable.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

CREATE TABLE IF NOT EXISTS expirynotifications
(
    id        uuid          NOT NULL PRIMARY KEY,
    cardid    uuid          NOT NULL,
    stage     int           NOT NULL,
    channel   int           NOT NULL,
    recipient varchar(256)  NOT NULL,
    sentutc   timestamptz   NOT NULL
);

-- SF-9: a given warning (card + stage + channel + recipient) is sent at most once.
-- Guarded by cardid's existence so a re-run after 044 has retired the card-only shape does not try to recreate
-- this legacy index on the dropped cardid column (which would abort the startup migration run on every boot).
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_name = 'expirynotifications' AND column_name = 'cardid')
       AND NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'ux_expirynotifications_unique') THEN
        CREATE UNIQUE INDEX ux_expirynotifications_unique
            ON expirynotifications (cardid, stage, channel, recipient);
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS jobruns
(
    id                uuid          NOT NULL PRIMARY KEY,
    jobname           varchar(128)  NOT NULL,
    startedutc        timestamptz   NOT NULL,
    finishedutc       timestamptz   NULL,
    status            int           NOT NULL,
    itemsprocessed    int           NOT NULL,
    notificationssent int           NOT NULL,
    error             varchar(2048) NULL
);

CREATE INDEX IF NOT EXISTS ix_jobruns_name_started ON jobruns (jobname, startedutc DESC);
