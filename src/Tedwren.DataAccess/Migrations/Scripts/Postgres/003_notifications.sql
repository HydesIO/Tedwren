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
CREATE UNIQUE INDEX IF NOT EXISTS ux_expirynotifications_unique
    ON expirynotifications (cardid, stage, channel, recipient);

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
