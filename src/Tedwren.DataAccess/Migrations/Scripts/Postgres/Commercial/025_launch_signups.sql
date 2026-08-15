-- Launch list (Web Content Spec §6.9): pre-launch interest emails captured on the marketing landing page —
-- PostgreSQL, commercial database. Idempotent. Lowercase identifiers so the shared (unquoted) repository SQL
-- folds to these names. Deduped on the lower-cased email.

CREATE TABLE IF NOT EXISTS launchsignups
(
    id          uuid          NOT NULL PRIMARY KEY,
    email       varchar(256)  NOT NULL,
    source      varchar(64)   NULL,
    utmsource   varchar(128)  NULL,
    utmmedium   varchar(128)  NULL,
    utmcampaign varchar(128)  NULL,
    createdutc  timestamptz   NOT NULL,
    notified    boolean       NOT NULL DEFAULT false,
    notifiedutc timestamptz   NULL
);

-- Dedupe: one row per email (case-insensitive).
CREATE UNIQUE INDEX IF NOT EXISTS ux_launchsignups_email ON launchsignups (LOWER(email));
