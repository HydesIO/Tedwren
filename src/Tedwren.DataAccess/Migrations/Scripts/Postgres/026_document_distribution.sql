-- Document distribution & acknowledgement (PRD §8.2) — PostgreSQL. Idempotent.

CREATE TABLE IF NOT EXISTS DocumentDistributions
(
    Id            UUID          NOT NULL PRIMARY KEY,
    CompanyId     UUID          NOT NULL,
    Title         VARCHAR(256)  NOT NULL,
    Category      VARCHAR(128)  NULL,
    Audience      VARCHAR(256)  NULL,
    FileReference VARCHAR(256)  NULL,
    SentBy        VARCHAR(256)  NOT NULL,
    SentUtc       TIMESTAMPTZ   NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_DocumentDistributions_Company_Sent ON DocumentDistributions (CompanyId, SentUtc);

CREATE TABLE IF NOT EXISTS DocumentAcknowledgements
(
    Id              UUID          NOT NULL PRIMARY KEY,
    DistributionId  UUID          NOT NULL,
    CompanyId       UUID          NOT NULL,
    RecipientName   VARCHAR(256)  NOT NULL,
    PersonId        UUID          NULL,
    AcknowledgedUtc TIMESTAMPTZ   NULL
);

CREATE INDEX IF NOT EXISTS IX_DocumentAcknowledgements_Distribution ON DocumentAcknowledgements (DistributionId);
CREATE INDEX IF NOT EXISTS IX_DocumentAcknowledgements_Company ON DocumentAcknowledgements (CompanyId);
