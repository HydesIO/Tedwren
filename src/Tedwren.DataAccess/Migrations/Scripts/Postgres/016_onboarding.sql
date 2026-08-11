-- Self-service onboarding links (SF-4, SUB-2) + private image store (SF-5, R9) — PostgreSQL. Idempotent.

CREATE TABLE IF NOT EXISTS OnboardingLinks
(
    Id              UUID          NOT NULL PRIMARY KEY,
    Token           VARCHAR(128)  NOT NULL,
    PasscodeHash    VARCHAR(256)  NULL,
    CompanyId       UUID          NOT NULL,
    Name            VARCHAR(256)  NULL,
    Trade           VARCHAR(128)  NULL,
    ExpiresUtc      TIMESTAMPTZ   NOT NULL,
    Status          INT           NOT NULL,
    PersonId        UUID          NULL,
    EngagementId    UUID          NULL,
    CreatedByUserId UUID          NULL,
    CreatedUtc      TIMESTAMPTZ   NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS UX_OnboardingLinks_Token ON OnboardingLinks (Token);

CREATE TABLE IF NOT EXISTS StoredImages
(
    Id          UUID         NOT NULL PRIMARY KEY,
    ContentType VARCHAR(128) NOT NULL,
    Bytes       BYTEA        NOT NULL,
    CreatedUtc  TIMESTAMPTZ  NOT NULL
);
