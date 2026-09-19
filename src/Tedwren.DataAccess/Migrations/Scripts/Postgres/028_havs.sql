-- Hand-arm vibration (HAVs) exposure records (PRD §8.2) — PostgreSQL. Idempotent.
-- Tool usages are held as JSON; the daily A(8)/points/band are derived at read time, never stored.

CREATE TABLE IF NOT EXISTS HavsExposureRecords
(
    Id             UUID          NOT NULL PRIMARY KEY,
    CompanyId      UUID          NOT NULL,
    PersonName     VARCHAR(256)  NOT NULL,
    ExposureDate   DATE          NOT NULL,
    ToolUsagesJson TEXT          NOT NULL,
    RecordedBy     VARCHAR(256)  NOT NULL,
    RecordedUtc    TIMESTAMPTZ   NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_HavsExposureRecords_Company_Recorded ON HavsExposureRecords (CompanyId, RecordedUtc);
