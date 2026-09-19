-- Operative field evidence captures (M5): offline-captured photo + note + location — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names. Append-only (R4).

CREATE TABLE IF NOT EXISTS evidenceitems
(
    id             uuid             NOT NULL PRIMARY KEY,
    companyid      uuid             NOT NULL,
    personid       uuid             NOT NULL,
    note           varchar(2000)    NULL,
    latitude       double precision NULL,
    longitude      double precision NULL,
    photoreference varchar(256)     NULL,
    capturedutc    timestamptz      NOT NULL,
    createdutc     timestamptz      NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_evidenceitems_companyid ON evidenceitems (companyid);
CREATE INDEX IF NOT EXISTS ix_evidenceitems_personid ON evidenceitems (personid);
