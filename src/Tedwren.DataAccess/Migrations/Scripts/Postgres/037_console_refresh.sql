-- Console user refresh tokens (M8): renew an expired console access token without re-login — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names.

CREATE TABLE IF NOT EXISTS userrefreshtokens
(
    id          uuid         NOT NULL PRIMARY KEY,
    userid      uuid         NOT NULL,
    companyid   uuid         NOT NULL,
    tokenhash   varchar(512) NOT NULL,
    expiresutc  timestamptz  NOT NULL,
    createdutc  timestamptz  NOT NULL,
    lastusedutc timestamptz  NOT NULL,
    revokedutc  timestamptz  NULL
);

CREATE INDEX IF NOT EXISTS ix_userrefreshtokens_userid ON userrefreshtokens (userid);
