-- Console user ↔ site assignments (MC-21, UAT-011) — PostgreSQL. Idempotent. Lowercase identifiers so the shared
-- (unquoted) repository SQL folds to these names. Backport of the EF migration AddSiteAssignments.

CREATE TABLE IF NOT EXISTS siteassignments
(
    id         uuid        NOT NULL PRIMARY KEY,
    userid     uuid        NOT NULL,
    siteid     uuid        NOT NULL,
    createdutc timestamptz NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_siteassignments_userid ON siteassignments (userid);
CREATE UNIQUE INDEX IF NOT EXISTS ix_siteassignments_userid_siteid ON siteassignments (userid, siteid);
