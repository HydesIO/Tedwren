-- Forms Library: customer-built, per-tenant form templates (PRD-Phase 2 checklist/inspection engine) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.
-- Sections and fields are stored as JSON; templates are versioned and append-only (R4/R10/R16), grouped by familyid.

CREATE TABLE IF NOT EXISTS formtemplates
(
    id           uuid          NOT NULL PRIMARY KEY,
    companyid    uuid          NOT NULL,
    familyid     uuid          NOT NULL,
    name         varchar(256)  NOT NULL,
    description  varchar(1024) NULL,
    version      integer       NOT NULL,
    status       integer       NOT NULL,   -- 0 Draft, 1 Published, 2 Archived
    sectionsjson text          NOT NULL,
    createdutc   timestamptz   NOT NULL,
    updatedutc   timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_formtemplates_company ON formtemplates (companyid);
CREATE INDEX IF NOT EXISTS ix_formtemplates_family ON formtemplates (companyid, familyid);
