-- Sites, boundaries & dispersed schemes (SF-6, SF-14, SF-25, SF-26) — PostgreSQL. Idempotent, re-runnable.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

CREATE TABLE IF NOT EXISTS sites
(
    id                   uuid              NOT NULL PRIMARY KEY,
    companyid            uuid              NOT NULL,
    name                 varchar(256)      NOT NULL,
    client               varchar(256)      NULL,
    region               varchar(128)      NULL,
    address              varchar(512)      NULL,
    hascompound          boolean           NOT NULL,
    isdispersed          boolean           NOT NULL,
    boundarylatitude     double precision  NULL,
    boundarylongitude    double precision  NULL,
    boundaryradiusmetres double precision  NULL,
    createdutc           timestamptz       NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_sites_company ON sites (companyid);

CREATE TABLE IF NOT EXISTS siteproperties
(
    id                   uuid              NOT NULL PRIMARY KEY,
    siteid               uuid              NOT NULL REFERENCES sites (id),
    address              varchar(512)      NOT NULL,
    units                int               NOT NULL,
    boundarylatitude     double precision  NOT NULL,
    boundarylongitude    double precision  NOT NULL,
    boundaryradiusmetres double precision  NOT NULL,
    createdutc           timestamptz       NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_siteproperties_site ON siteproperties (siteid);
