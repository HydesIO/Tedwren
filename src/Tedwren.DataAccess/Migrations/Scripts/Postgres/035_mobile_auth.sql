-- Operative mobile auth (M2): device binding + one-time-code challenges — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

CREATE TABLE IF NOT EXISTS operativedevices
(
    id                     uuid         NOT NULL PRIMARY KEY,
    personid               uuid         NOT NULL,
    companyid              uuid         NOT NULL,
    deviceid               varchar(128) NOT NULL,
    devicename             varchar(256) NULL,
    status                 integer      NOT NULL,   -- 0 Active, 1 Revoked
    refreshtokenhash       varchar(512) NULL,
    refreshtokenexpiresutc timestamptz  NULL,
    enrolledutc            timestamptz  NOT NULL,
    lastseenutc            timestamptz  NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_operativedevices_deviceid ON operativedevices (deviceid);
CREATE INDEX IF NOT EXISTS ix_operativedevices_personid ON operativedevices (personid);

CREATE TABLE IF NOT EXISTS otpchallenges
(
    id          uuid         NOT NULL PRIMARY KEY,
    phonenumber varchar(32)  NOT NULL,
    codehash    varchar(512) NOT NULL,
    expiresutc  timestamptz  NOT NULL,
    attempts    integer      NOT NULL,
    createdutc  timestamptz  NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_otpchallenges_phonenumber ON otpchallenges (phonenumber);
