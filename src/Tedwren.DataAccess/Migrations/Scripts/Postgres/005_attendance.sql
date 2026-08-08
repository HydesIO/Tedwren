-- Sign-in / sign-out & attendance (SF-13–SF-19, R4, R11) — PostgreSQL. Idempotent, re-runnable.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

-- SF-15: the per-site location policy (added to the Phase 11 sites table).
ALTER TABLE sites ADD COLUMN IF NOT EXISTS locationpolicy int NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS attendance
(
    id               uuid          NOT NULL PRIMARY KEY,
    personid         uuid          NOT NULL,
    siteid           uuid          NOT NULL,
    propertyid       uuid          NULL,
    type             int           NOT NULL,
    outcome          int           NOT NULL,
    method           int           NOT NULL,
    latitude         double precision NULL,
    longitude        double precision NULL,
    withinboundary   boolean       NULL,
    reason           varchar(512)  NULL,
    correctsrecordid uuid          NULL,
    occurredutc      timestamptz   NOT NULL,
    createdutc       timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_attendance_person ON attendance (personid, occurredutc);
CREATE INDEX IF NOT EXISTS ix_attendance_site ON attendance (siteid, occurredutc);
