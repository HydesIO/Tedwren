-- Per-company general settings (System Configuration), stored as one JSON row per company — PostgreSQL. Idempotent.

CREATE TABLE IF NOT EXISTS CompanySettings
(
    CompanyId    UUID        NOT NULL PRIMARY KEY,
    SettingsJson TEXT        NOT NULL,
    UpdatedUtc   TIMESTAMPTZ NOT NULL
);
