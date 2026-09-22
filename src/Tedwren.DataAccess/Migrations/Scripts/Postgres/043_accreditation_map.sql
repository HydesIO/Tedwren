-- Phase 5b accreditation / Gate 3: the SF-11 trade→accreditation map gains legal-mandatory / client-required
-- flags and org ownership; qualification types gain org ownership (Q21, customer-adjustable); and a card carries
-- the client id of the mobile capture that created it, so the operative app's at-least-once outbox upload is
-- idempotent. PostgreSQL. Idempotent, additive columns. Lowercase identifiers.

ALTER TABLE tradequalificationrequirements ADD COLUMN IF NOT EXISTS legalmandatory boolean NOT NULL DEFAULT false;
ALTER TABLE tradequalificationrequirements ADD COLUMN IF NOT EXISTS clientrequired boolean NOT NULL DEFAULT false;
ALTER TABLE tradequalificationrequirements ADD COLUMN IF NOT EXISTS companyid uuid NULL;

ALTER TABLE qualificationtypes ADD COLUMN IF NOT EXISTS companyid uuid NULL;

ALTER TABLE qualificationcards ADD COLUMN IF NOT EXISTS captureclientid uuid NULL;
