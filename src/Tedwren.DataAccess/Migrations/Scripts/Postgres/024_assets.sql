-- Plant & equipment register (Assets page, PRD §8.2) — PostgreSQL. Idempotent.

CREATE TABLE IF NOT EXISTS Assets
(
    Id                  UUID          NOT NULL PRIMARY KEY,
    CompanyId           UUID          NOT NULL,
    Name                VARCHAR(256)  NOT NULL,
    AssetType           VARCHAR(128)  NULL,
    SerialNumber        VARCHAR(128)  NULL,
    Location            VARCHAR(256)  NULL,
    OwnerName           VARCHAR(256)  NULL,
    CertificationExpiry DATE          NULL,
    NextInspectionDue   DATE          NULL,
    Notes               TEXT          NULL,
    Status              INT           NOT NULL,
    CreatedUtc          TIMESTAMPTZ   NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Assets_Company_Created ON Assets (CompanyId, CreatedUtc);
