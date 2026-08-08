-- Qualification cards & competency (SF-5–SF-8, SF-10–SF-12) — PostgreSQL. Idempotent, re-runnable.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

CREATE TABLE IF NOT EXISTS qualificationtypes
(
    id                    uuid          NOT NULL PRIMARY KEY,
    name                  varchar(256)  NOT NULL,
    category              varchar(128)  NULL,
    issuer                varchar(128)  NULL,
    defaultvaliditymonths int           NOT NULL,
    iscscsverifiable      boolean       NOT NULL,
    createdutc            timestamptz   NOT NULL
);

CREATE TABLE IF NOT EXISTS qualificationcards
(
    id                  uuid          NOT NULL PRIMARY KEY,
    personid            uuid          NOT NULL,
    qualificationtypeid uuid          NOT NULL REFERENCES qualificationtypes (id),
    cardnumber          varchar(128)  NULL,
    holdername          varchar(256)  NULL,
    issuedon            date          NULL,
    expireson           date          NULL,
    capturesource       int           NOT NULL,
    imagereference      varchar(512)  NULL,
    verificationstate   int           NOT NULL,
    needsreview         boolean       NOT NULL,
    confirmedby         varchar(256)  NULL,
    confirmedutc        timestamptz   NULL,
    supersedescardid    uuid          NULL,
    supersededbycardid  uuid          NULL,
    createdutc          timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_qualificationcards_person ON qualificationcards (personid);

CREATE TABLE IF NOT EXISTS tradequalificationrequirements
(
    id                  uuid          NOT NULL PRIMARY KEY,
    trade               varchar(128)  NOT NULL,
    qualificationtypeid uuid          NOT NULL REFERENCES qualificationtypes (id)
);

-- SF-11: one requirement row per (trade, qualification type).
CREATE UNIQUE INDEX IF NOT EXISTS ux_tradereq_trade_type ON tradequalificationrequirements (trade, qualificationtypeid);
