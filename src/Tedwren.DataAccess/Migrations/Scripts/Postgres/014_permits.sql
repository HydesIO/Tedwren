-- Permits to work (Permits page) — PostgreSQL. Idempotent.

CREATE TABLE IF NOT EXISTS Permits
(
    Id                UUID          NOT NULL PRIMARY KEY,
    CompanyId         UUID          NOT NULL,
    PermitType        VARCHAR(128)  NOT NULL,
    SiteName          VARCHAR(256)  NULL,
    ResponsiblePerson VARCHAR(256)  NULL,
    ValidFrom         DATE          NULL,
    ValidTo           DATE          NULL,
    Description       TEXT          NULL,
    HighRisk          BOOLEAN       NOT NULL,
    RamsAttached      BOOLEAN       NOT NULL,
    Status            INT           NOT NULL,
    CreatedUtc        TIMESTAMPTZ   NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Permits_Company_Created ON Permits (CompanyId, CreatedUtc);
