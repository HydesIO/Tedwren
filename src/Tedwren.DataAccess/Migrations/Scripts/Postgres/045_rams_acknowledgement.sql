-- Operative RAMS acknowledgements (Subcontractor Onboarding spec Gate 5) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the repositories' unquoted SQL folds to these names. One append-only row per signature:
-- who signed which live RAMS version, when, and its re-sign deadline under the MC's review cycle (expiresutc, null =
-- version-pinned). Scoped to the owning main contractor (R15). The site-entry decision and the operative's own
-- sign-in both read the latest row per (company, person, family) to clear Gate 5.

CREATE TABLE IF NOT EXISTS ramsacknowledgements
(
    id            uuid          NOT NULL PRIMARY KEY,
    companyid     uuid          NOT NULL,
    personid      uuid          NOT NULL,
    familyid      uuid          NOT NULL,
    version       integer       NOT NULL,
    signaturename varchar(256)  NOT NULL,
    signedutc     timestamptz   NOT NULL,
    expiresutc    timestamptz   NULL
);

CREATE INDEX IF NOT EXISTS ix_ramsacknowledgements_companyid_personid_familyid ON ramsacknowledgements (companyid, personid, familyid);
