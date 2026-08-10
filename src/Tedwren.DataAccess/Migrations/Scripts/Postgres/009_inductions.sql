-- Digital inductions: configurable templates + operative sessions (MC-1–MC-7, MC-20, R5) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

CREATE TABLE IF NOT EXISTS inductiontemplates
(
    id            uuid          NOT NULL PRIMARY KEY,
    companyid     uuid          NOT NULL,
    name          varchar(256)  NOT NULL,
    validitydays  integer       NOT NULL,
    passmark      integer       NOT NULL,
    stepsjson     text          NOT NULL,
    questionsjson text          NOT NULL   -- includes server-side answers; never sent to the device (R5)
);

CREATE INDEX IF NOT EXISTS ix_inductiontemplates_company ON inductiontemplates (companyid);

CREATE TABLE IF NOT EXISTS inductionsessions
(
    id                    uuid          NOT NULL PRIMARY KEY,
    templateid            uuid          NOT NULL,
    companyid             uuid          NOT NULL,
    personid              uuid          NOT NULL,
    personname            varchar(256)  NOT NULL,
    status                integer       NOT NULL,
    startedutc            timestamptz   NOT NULL,
    completedutc          timestamptz   NULL,
    completionreference   varchar(64)   NULL,
    expiresutc            timestamptz   NULL,
    attemptcount          integer       NOT NULL,
    lastscore             integer       NULL,
    completedstepsjson    text          NULL,
    signaturename         varchar(256)  NULL,
    consentgiven          boolean       NOT NULL,
    resetreason           varchar(1024) NULL,
    supersededbysessionid uuid          NULL
);

CREATE INDEX IF NOT EXISTS ix_inductionsessions_company ON inductionsessions (companyid, startedutc);
CREATE INDEX IF NOT EXISTS ix_inductionsessions_person ON inductionsessions (companyid, templateid, personid, status);
