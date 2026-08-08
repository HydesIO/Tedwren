-- Timesheets: first-class, stateful, approvable objects with append-only lines (SUB-7–SUB-12, R16) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

CREATE TABLE IF NOT EXISTS timesheets
(
    id            uuid          NOT NULL PRIMARY KEY,
    companyid     uuid          NOT NULL,
    personid      uuid          NOT NULL,
    operativename varchar(256)  NOT NULL,
    weekstart     date          NOT NULL,
    status        integer       NOT NULL,
    submittedutc  timestamptz   NULL,
    decidedby     varchar(256)  NULL,
    decidedutc    timestamptz   NULL,
    decisionscope integer       NULL,
    returnreason  varchar(1024) NULL,
    createdutc    timestamptz   NOT NULL
);

-- One timesheet per (company, operative, week).
CREATE UNIQUE INDEX IF NOT EXISTS ux_timesheets_company_person_week ON timesheets (companyid, personid, weekstart);
CREATE INDEX IF NOT EXISTS ix_timesheets_company_week ON timesheets (companyid, weekstart);

CREATE TABLE IF NOT EXISTS timesheetentries
(
    id              uuid          NOT NULL PRIMARY KEY,
    timesheetid     uuid          NOT NULL,
    workdate        date          NOT NULL,
    siteid          uuid          NOT NULL,
    sitename        varchar(256)  NOT NULL,
    hours           numeric(9, 2) NOT NULL,
    correctsentryid uuid          NULL,
    author          varchar(256)  NULL,
    reason          varchar(1024) NULL,
    createdutc      timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_timesheetentries_timesheet ON timesheetentries (timesheetid, createdutc);
