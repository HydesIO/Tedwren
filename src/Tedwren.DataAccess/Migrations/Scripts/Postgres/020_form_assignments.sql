-- Forms Library: form assignments to sites / operators / inductions (PRD-Phase 2, requirement 5) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

CREATE TABLE IF NOT EXISTS formassignments
(
    id                   uuid          NOT NULL PRIMARY KEY,
    companyid            uuid          NOT NULL,
    formtemplatefamilyid uuid          NOT NULL,
    formname             varchar(256)  NOT NULL,
    scope                integer       NOT NULL,   -- 0 Organisation, 1 Site, 2 Operator, 3 Induction
    siteid               uuid          NULL,
    personid             uuid          NULL,
    inductiontemplateid  uuid          NULL,
    schedule             integer       NOT NULL,   -- 0 AdHoc, 1 Daily, 2 Weekly, 3 Monthly
    failurealertemail    varchar(256)  NULL,
    createdutc           timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_formassignments_company ON formassignments (companyid);
CREATE INDEX IF NOT EXISTS ix_formassignments_family ON formassignments (companyid, formtemplatefamilyid);
