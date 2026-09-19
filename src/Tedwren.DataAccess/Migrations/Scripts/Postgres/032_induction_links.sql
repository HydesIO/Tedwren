-- Shareable / tokenised induction links (MC-1/MC-2, UAT-018) — PostgreSQL. Idempotent. Lowercase identifiers so
-- the shared (unquoted) repository SQL folds to these names. Backport of the EF migration AddInductionLinks.

CREATE TABLE IF NOT EXISTS inductionlinks
(
    id              uuid          NOT NULL PRIMARY KEY,
    token           varchar(128)  NOT NULL,
    passcodehash    varchar(256)  NULL,
    companyid       uuid          NOT NULL,
    templateid      uuid          NOT NULL,
    name            varchar(256)  NULL,
    expiresutc      timestamptz   NOT NULL,
    status          integer       NOT NULL,
    sessionid       uuid          NULL,
    createdbyuserid uuid          NULL,
    createdutc      timestamptz   NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_inductionlinks_token ON inductionlinks (token);
