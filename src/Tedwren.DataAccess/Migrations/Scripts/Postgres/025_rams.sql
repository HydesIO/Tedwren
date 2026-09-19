-- RAMS submission & approval (PRD §8.2) — PostgreSQL. Idempotent. Append-only (resubmission = new version).

CREATE TABLE IF NOT EXISTS RamsSubmissions
(
    Id             UUID          NOT NULL PRIMARY KEY,
    CompanyId      UUID          NOT NULL,
    FamilyId       UUID          NOT NULL,
    Version        INT           NOT NULL,
    Reference      VARCHAR(64)   NOT NULL,
    ContractorName VARCHAR(256)  NOT NULL,
    Title          VARCHAR(256)  NOT NULL,
    SiteId         UUID          NULL,
    SiteName       VARCHAR(256)  NULL,
    FileReference  VARCHAR(256)  NULL,
    Status         INT           NOT NULL,
    ReviewNote     TEXT          NULL,
    ReviewedBy     VARCHAR(256)  NULL,
    ReviewedUtc    TIMESTAMPTZ   NULL,
    SubmittedUtc   TIMESTAMPTZ   NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_RamsSubmissions_Company_Submitted ON RamsSubmissions (CompanyId, SubmittedUtc);
CREATE INDEX IF NOT EXISTS IX_RamsSubmissions_Company_Family ON RamsSubmissions (CompanyId, FamilyId);
