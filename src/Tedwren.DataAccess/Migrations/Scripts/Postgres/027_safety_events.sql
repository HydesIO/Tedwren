-- Hazard / near-miss reports and accident / incident records (PRD §8.2) — PostgreSQL. Idempotent.

CREATE TABLE IF NOT EXISTS HazardReports
(
    Id             UUID           NOT NULL PRIMARY KEY,
    CompanyId      UUID           NOT NULL,
    Reference      VARCHAR(64)    NOT NULL,
    Kind           INT            NOT NULL,
    Description    VARCHAR(2000)  NOT NULL,
    Location       VARCHAR(256)   NULL,
    Latitude       DOUBLE PRECISION NULL,
    Longitude      DOUBLE PRECISION NULL,
    PhotoReference VARCHAR(256)   NULL,
    Severity       INT            NOT NULL,
    Category       VARCHAR(128)   NULL,
    Status         INT            NOT NULL,
    AssignedTo     VARCHAR(256)   NULL,
    ReportedBy     VARCHAR(256)   NOT NULL,
    ReportedUtc    TIMESTAMPTZ    NOT NULL,
    ClosedUtc      TIMESTAMPTZ    NULL,
    ClosureNote    VARCHAR(2000)  NULL
);

CREATE INDEX IF NOT EXISTS IX_HazardReports_Company_Reported ON HazardReports (CompanyId, ReportedUtc);

CREATE TABLE IF NOT EXISTS IncidentReports
(
    Id                UUID           NOT NULL PRIMARY KEY,
    CompanyId         UUID           NOT NULL,
    Reference         VARCHAR(64)    NOT NULL,
    Kind              INT            NOT NULL,
    Description       VARCHAR(2000)  NOT NULL,
    Location          VARCHAR(256)   NULL,
    OccurredUtc       TIMESTAMPTZ    NOT NULL,
    InjuredPersonName VARCHAR(256)   NULL,
    InjuryDetail      VARCHAR(512)   NULL,
    Severity          INT            NOT NULL,
    ImmediateCause    VARCHAR(2000)  NULL,
    RootCause         VARCHAR(2000)  NULL,
    CorrectiveActions VARCHAR(2000)  NULL,
    Status            INT            NOT NULL,
    RiddorReportable  BOOLEAN        NOT NULL,
    RiddorCategory    VARCHAR(128)   NULL,
    ReportedBy        VARCHAR(256)   NOT NULL,
    ReportedUtc       TIMESTAMPTZ    NOT NULL,
    InvestigatedBy    VARCHAR(256)   NULL,
    ClosedUtc         TIMESTAMPTZ    NULL
);

CREATE INDEX IF NOT EXISTS IX_IncidentReports_Company_Reported ON IncidentReports (CompanyId, ReportedUtc);
