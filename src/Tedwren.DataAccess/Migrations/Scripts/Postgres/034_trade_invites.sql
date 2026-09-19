-- Trade (subcontractor) onboarding invites (PRD-Phase 1) — PostgreSQL. Idempotent. Lowercase identifiers so the
-- shared (unquoted) repository SQL folds to these names. Backport of the EF migration AddTradeOnboarding.

CREATE TABLE IF NOT EXISTS tradeinvites
(
    id               uuid          NOT NULL PRIMARY KEY,
    token            varchar(128)  NOT NULL,
    passcodehash     varchar(256)  NULL,
    companyid        uuid          NOT NULL,
    invitercompanyid uuid          NOT NULL,
    contactname      varchar(256)  NULL,
    contactemail     varchar(256)  NULL,
    status           integer       NOT NULL,
    expiresutc       timestamptz   NOT NULL,
    submittedutc     timestamptz   NULL,
    decidedby        varchar(256)  NULL,
    decidedutc       timestamptz   NULL,
    reviewnote       varchar(1024) NULL,
    createdbyuserid  uuid          NULL,
    createdutc       timestamptz   NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_tradeinvites_token ON tradeinvites (token);
CREATE INDEX IF NOT EXISTS ix_tradeinvites_invitercompanyid ON tradeinvites (invitercompanyid);
