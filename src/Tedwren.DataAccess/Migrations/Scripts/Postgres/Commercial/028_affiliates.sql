-- Affiliates (Web Plan §7): partners who introduce customers and earn profit-share commission — PostgreSQL,
-- commercial database. Idempotent. Lowercase identifiers for the shared repository SQL. No raw bank details.

CREATE TABLE IF NOT EXISTS affiliates
(
    id               uuid          NOT NULL PRIMARY KEY,
    name             varchar(160)  NOT NULL,
    company          varchar(200)  NULL,
    contactemail     varchar(256)  NOT NULL,
    status           integer       NOT NULL DEFAULT 0,
    accountmanager   varchar(160)  NULL,
    payeereference   varchar(140)  NULL,
    affiliateratepct numeric(9, 4) NOT NULL DEFAULT 0,
    profitmarginpct  numeric(9, 4) NOT NULL DEFAULT 0,
    createdutc       timestamptz   NOT NULL,
    updatedutc       timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_affiliates_updatedutc ON affiliates (updatedutc DESC);
