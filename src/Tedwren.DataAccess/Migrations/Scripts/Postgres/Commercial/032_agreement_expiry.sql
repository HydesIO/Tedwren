-- Affiliate-agreement signing-link expiry — PostgreSQL, commercial database. Idempotent ALTER.
-- Lowercase identifiers for the shared repository SQL. Already-signed agreements ignore expiry.

ALTER TABLE affiliateagreements ADD COLUMN IF NOT EXISTS expiresutc timestamptz NULL;
