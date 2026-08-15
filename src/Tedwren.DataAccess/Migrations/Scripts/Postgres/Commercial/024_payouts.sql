-- BACS payouts (Phase D): GoCardless settlement into the Tedwren creditor account — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names. Money in minor units (pence).

CREATE TABLE IF NOT EXISTS payouts
(
    id                 uuid          NOT NULL PRIMARY KEY,
    gocardlesspayoutid varchar(64)   NOT NULL,
    amountpence        integer       NOT NULL,
    currency           varchar(3)    NOT NULL,
    status             integer       NOT NULL,
    reference          varchar(140)  NULL,
    arrivaldate        date          NULL,
    createdutc         timestamptz   NOT NULL,
    updatedutc         timestamptz   NOT NULL
);

-- Dedupe: one stored row per GoCardless payout id.
CREATE UNIQUE INDEX IF NOT EXISTS ux_payouts_gcpayout ON payouts (gocardlesspayoutid);
