-- Compliance packs: fixed-at-send snapshots shared with account-free recipients (SUB-13–SUB-26, R7–R9) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

CREATE TABLE IF NOT EXISTS compliancepacks
(
    id                 uuid          NOT NULL PRIMARY KEY,
    companyid          uuid          NOT NULL,
    title              varchar(256)  NOT NULL,
    siteid             uuid          NULL,
    sitename           varchar(256)  NULL,
    fromdate           date          NULL,
    todate             date          NULL,
    createdby          varchar(256)  NOT NULL,
    createdutc         timestamptz   NOT NULL,
    expiresutc         timestamptz   NOT NULL,
    token              varchar(128)  NOT NULL,
    passcodehash       varchar(256)  NOT NULL,
    status             integer       NOT NULL,
    supersededbypackid uuid          NULL,
    subjectsjson       text          NULL
);

-- R9: the token is the recipient's only handle and must be unique.
CREATE UNIQUE INDEX IF NOT EXISTS ux_compliancepacks_token ON compliancepacks (token);
CREATE INDEX IF NOT EXISTS ix_compliancepacks_company ON compliancepacks (companyid, createdutc);

CREATE TABLE IF NOT EXISTS packaccessevents
(
    id          uuid          NOT NULL PRIMARY KEY,
    packid      uuid          NOT NULL,
    kind        integer       NOT NULL,
    occurredutc timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_packaccessevents_pack ON packaccessevents (packid, kind);
