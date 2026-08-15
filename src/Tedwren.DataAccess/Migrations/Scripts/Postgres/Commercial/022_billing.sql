-- Direct-debit billing: mandates, payments & subscriptions (GoCardless; proposed PRD §9) — PostgreSQL. Idempotent.
-- Lowercase identifiers so the shared (unquoted) repository SQL folds to these names. Money in minor units (pence).

CREATE TABLE IF NOT EXISTS mandates
(
    id                         uuid          NOT NULL PRIMARY KEY,
    companyid                  uuid          NOT NULL,
    gocardlessbillingrequestid varchar(64)   NULL,
    gocardlessmandateid        varchar(64)   NULL,
    gocardlesscustomerid       varchar(64)   NULL,
    reference                  varchar(140)  NULL,
    bankname                   varchar(140)  NULL,
    accountnumberending        varchar(8)    NULL,
    status                     integer       NOT NULL,
    createdutc                 timestamptz   NOT NULL,
    updatedutc                 timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_mandates_company ON mandates (companyid, createdutc);
CREATE INDEX IF NOT EXISTS ix_mandates_gcmandate ON mandates (gocardlessmandateid);

CREATE TABLE IF NOT EXISTS payments
(
    id                  uuid          NOT NULL PRIMARY KEY,
    companyid           uuid          NOT NULL,
    mandateid           uuid          NOT NULL,
    gocardlesspaymentid varchar(64)   NULL,
    amountpence         integer       NOT NULL,
    currency            varchar(3)    NOT NULL,
    description         varchar(500)  NULL,
    status              integer       NOT NULL,
    chargedate          date          NULL,
    failurereason       varchar(500)  NULL,
    createdutc          timestamptz   NOT NULL,
    updatedutc          timestamptz   NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_payments_company ON payments (companyid, createdutc);
CREATE INDEX IF NOT EXISTS ix_payments_gcpayment ON payments (gocardlesspaymentid);

CREATE TABLE IF NOT EXISTS billingsubscriptions
(
    id          uuid          NOT NULL PRIMARY KEY,
    companyid   uuid          NOT NULL,
    mandateid   uuid          NULL,
    meterkey    varchar(64)   NOT NULL,
    bandkey     varchar(64)   NULL,
    description varchar(500)  NULL,
    status      integer       NOT NULL,
    createdutc  timestamptz   NOT NULL,
    updatedutc  timestamptz   NOT NULL
);

-- One billing subscription per company.
CREATE UNIQUE INDEX IF NOT EXISTS ux_billingsubscriptions_company ON billingsubscriptions (companyid);
