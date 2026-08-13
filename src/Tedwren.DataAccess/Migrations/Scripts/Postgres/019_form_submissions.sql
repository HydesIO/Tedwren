-- Forms Library: completed form submissions + captured files (PRD-Phase 2 checklist/inspection engine) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.
-- Answers are stored as JSON; submissions are append-only evidence (R4/R10). Files are stored as blobs.

CREATE TABLE IF NOT EXISTS formsubmissions
(
    id                  uuid          NOT NULL PRIMARY KEY,
    companyid           uuid          NOT NULL,
    formtemplateid      uuid          NOT NULL,
    formtemplateversion integer       NOT NULL,
    formname            varchar(256)  NOT NULL,
    scope               integer       NOT NULL,   -- 0 Organisation, 1 Site, 2 Operator, 3 Induction
    siteid              uuid          NULL,
    personid            uuid          NULL,
    answersjson         text          NOT NULL,
    status              integer       NOT NULL,   -- 0 Draft, 1 Submitted, 2 Approved, 3 Rejected
    submittedutc        timestamptz   NOT NULL,
    submittedby         varchar(256)  NOT NULL,
    reviewnote          text          NULL
);

CREATE INDEX IF NOT EXISTS ix_formsubmissions_company ON formsubmissions (companyid, submittedutc);
CREATE INDEX IF NOT EXISTS ix_formsubmissions_template ON formsubmissions (formtemplateid);

CREATE TABLE IF NOT EXISTS formsubmissionfiles
(
    id           uuid          NOT NULL PRIMARY KEY,
    companyid    uuid          NOT NULL,
    submissionid uuid          NOT NULL,
    fieldid      varchar(64)   NOT NULL,
    filename     varchar(512)  NOT NULL,
    contenttype  varchar(128)  NOT NULL,
    content      bytea         NOT NULL,
    uploadedutc  timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_formsubmissionfiles_submission ON formsubmissionfiles (submissionid);
