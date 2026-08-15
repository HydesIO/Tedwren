-- Sales leads (Web Plan §7): the pipeline of prospective customers — PostgreSQL, commercial database.
-- Idempotent. Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.
-- Account/affiliate are soft references (Guids into other databases) — no cross-database foreign keys.

CREATE TABLE IF NOT EXISTS leads
(
    id                 uuid           NOT NULL PRIMARY KEY,
    companyname        varchar(256)   NOT NULL,
    contactname        varchar(160)   NULL,
    contactemail       varchar(256)   NULL,
    location           varchar(160)   NULL,
    model              integer        NOT NULL DEFAULT 0,
    numberofsites      integer        NULL,
    estimatedrevenue   numeric(18, 2) NULL,
    status             integer        NOT NULL DEFAULT 0,
    accountmanager     varchar(160)   NULL,
    companynumber      varchar(32)    NULL,
    affiliateid        uuid           NULL,
    convertedaccountid uuid           NULL,
    source             varchar(64)    NULL,
    createdutc         timestamptz    NOT NULL,
    updatedutc         timestamptz    NOT NULL
);

-- Recently-active ordering and status filtering.
CREATE INDEX IF NOT EXISTS ix_leads_updatedutc ON leads (updatedutc DESC);
