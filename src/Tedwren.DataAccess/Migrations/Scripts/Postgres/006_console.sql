-- Roles, audit, entitlements & decision store (SF-20, SF-22, SF-23, R10, Q2) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

CREATE TABLE IF NOT EXISTS moduleentitlements
(
    id         uuid          NOT NULL PRIMARY KEY,
    companyid  uuid          NOT NULL,
    modulekey  varchar(64)   NOT NULL,
    enabled    boolean       NOT NULL,
    updatedutc timestamptz   NOT NULL
);

-- Q2: one entitlement per (company, module).
CREATE UNIQUE INDEX IF NOT EXISTS ux_moduleentitlements_company_module ON moduleentitlements (companyid, modulekey);

CREATE TABLE IF NOT EXISTS auditentries
(
    id          uuid          NOT NULL PRIMARY KEY,
    companyid   uuid          NOT NULL,
    occurredutc timestamptz   NOT NULL,
    actor       varchar(256)  NOT NULL,
    action      varchar(512)  NOT NULL,
    entity      varchar(256)  NOT NULL,
    reference   varchar(128)  NULL,
    category    varchar(128)  NULL
);

CREATE INDEX IF NOT EXISTS ix_auditentries_company_occurred ON auditentries (companyid, occurredutc);

CREATE TABLE IF NOT EXISTS decisions
(
    id          uuid          NOT NULL PRIMARY KEY,
    personid    uuid          NOT NULL,
    siteid      uuid          NOT NULL,
    admitted    boolean       NOT NULL,
    occurredutc timestamptz   NOT NULL,
    checksjson  text          NULL
);

CREATE INDEX IF NOT EXISTS ix_decisions_person ON decisions (personid, occurredutc);
CREATE INDEX IF NOT EXISTS ix_decisions_site ON decisions (siteid, occurredutc);
