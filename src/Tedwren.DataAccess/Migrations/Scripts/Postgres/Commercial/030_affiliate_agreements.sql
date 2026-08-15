-- Affiliate agreements (Web Plan §7): the token-gated terms an affiliate signs, and the countersigned PDF
-- captured at signing — PostgreSQL, commercial database. Idempotent. Lowercase identifiers for the shared
-- repository SQL. The PDF is served only via the token-gated endpoint, never a permanent public URL (R9).

CREATE TABLE IF NOT EXISTS affiliateagreements
(
    id                    uuid          NOT NULL PRIMARY KEY,
    affiliateid           uuid          NOT NULL,
    token                 varchar(64)   NOT NULL,
    status                integer       NOT NULL DEFAULT 0,
    termshtml             text          NOT NULL,
    countersignatoryname  varchar(160)  NOT NULL,
    countersignatorytitle varchar(160)  NULL,
    signaturedataurl      text          NULL,
    signedbyname          varchar(160)  NULL,
    signedutc             timestamptz   NULL,
    pdfbytes              bytea         NULL,
    createdutc            timestamptz   NOT NULL
);

-- The public token is the lookup key and must be unique.
CREATE UNIQUE INDEX IF NOT EXISTS ux_affiliateagreements_token ON affiliateagreements (token);
