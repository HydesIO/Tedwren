-- Company documents slice (SUB-4: insurances, accreditations, policies) — PostgreSQL.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.
-- Idempotent so the runner can re-apply safely.

CREATE TABLE IF NOT EXISTS companydocuments
(
    id         uuid          NOT NULL PRIMARY KEY,
    companyid  uuid          NOT NULL REFERENCES companies (id),
    name       varchar(256)  NOT NULL,
    type       varchar(128)  NOT NULL,
    expireson  date          NULL,
    reference  varchar(128)  NULL,
    createdutc timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_companydocuments_companyid ON companydocuments (companyid);
