-- Organisation slice (SF-1/SF-2/SF-3) — PostgreSQL. Idempotent so the runner can re-apply safely.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names on PostgreSQL
-- and matches the PascalCase columns on SQL Server (which is case-insensitive).

CREATE TABLE IF NOT EXISTS companies
(
    id                 uuid          NOT NULL PRIMARY KEY,
    name               varchar(256)  NOT NULL,
    type               varchar(128)  NULL,
    trade              varchar(128)  NULL,
    registrationnumber varchar(64)   NULL,
    address            varchar(512)  NULL,
    contactname        varchar(256)  NULL,
    contactemail       varchar(256)  NULL,
    contactphone       varchar(64)   NULL,
    createdutc         timestamptz   NOT NULL
);

CREATE TABLE IF NOT EXISTS persons
(
    id          uuid        NOT NULL PRIMARY KEY,
    phonenumber varchar(32) NOT NULL,
    createdutc  timestamptz NOT NULL
);

-- SF-1: one person per mobile number.
CREATE UNIQUE INDEX IF NOT EXISTS ux_persons_phonenumber ON persons (phonenumber);

CREATE TABLE IF NOT EXISTS engagements
(
    id                uuid          NOT NULL PRIMARY KEY,
    companyid         uuid          NOT NULL REFERENCES companies (id),
    personid          uuid          NOT NULL REFERENCES persons (id),
    name              varchar(256)  NOT NULL,
    trade             varchar(128)  NULL,
    internalreference varchar(128)  NULL,
    status            int           NOT NULL,
    createdutc        timestamptz   NOT NULL,
    archivedutc       timestamptz   NULL
);

-- SF-2: one engagement per person per company.
CREATE UNIQUE INDEX IF NOT EXISTS ux_engagements_company_person ON engagements (companyid, personid);
