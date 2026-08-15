-- Lead activity log (Web Plan §7): append-only notes per lead, including automatic status-change entries —
-- PostgreSQL, commercial database. Idempotent. Lowercase identifiers for the shared repository SQL.

CREATE TABLE IF NOT EXISTS leadnotes
(
    id           uuid          NOT NULL PRIMARY KEY,
    leadid       uuid          NOT NULL,
    author       varchar(160)  NULL,
    body         text          NOT NULL,
    statuschange varchar(64)   NULL,
    createdutc   timestamptz   NOT NULL
);

-- Fetch a lead's log newest-first.
CREATE INDEX IF NOT EXISTS ix_leadnotes_lead ON leadnotes (leadid, createdutc DESC);
