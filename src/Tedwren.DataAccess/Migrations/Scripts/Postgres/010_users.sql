-- Console users (SF-20, SF-23, Q2) — PostgreSQL. Idempotent, re-runnable.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

CREATE TABLE IF NOT EXISTS users
(
    id            uuid          NOT NULL PRIMARY KEY,
    companyid     uuid          NOT NULL,
    name          varchar(256)  NOT NULL,
    email         varchar(256)  NOT NULL,
    role          int           NOT NULL,
    status        int           NOT NULL,
    createdutc    timestamptz   NOT NULL,
    lastactiveutc timestamptz   NULL
);

CREATE INDEX IF NOT EXISTS ix_users_company ON users (companyid);

-- One account per email address (case-insensitive).
CREATE UNIQUE INDEX IF NOT EXISTS ux_users_email ON users (LOWER(email));
