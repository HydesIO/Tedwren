-- Affiliate payouts (Web Plan §7): commission owed/paid to an affiliate — PostgreSQL, commercial database.
-- Idempotent. Lowercase identifiers for the shared repository SQL. Amount + status only; no bank details.

CREATE TABLE IF NOT EXISTS affiliatepayouts
(
    id          uuid           NOT NULL PRIMARY KEY,
    affiliateid uuid           NOT NULL,
    leadid      uuid           NULL,
    amount      numeric(18, 2) NOT NULL,
    currency    varchar(3)     NOT NULL DEFAULT 'GBP',
    status      integer        NOT NULL DEFAULT 0,
    reference   varchar(200)   NULL,
    createdutc  timestamptz    NOT NULL,
    paidutc     timestamptz    NULL
);

CREATE INDEX IF NOT EXISTS ix_affiliatepayouts_affiliate ON affiliatepayouts (affiliateid, createdutc DESC);
