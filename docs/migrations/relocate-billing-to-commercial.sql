-- =====================================================================================================
-- ONE-OFF billing-data relocation: product database  ->  commercial database (SQL Server).
--
-- This is NOT an auto-run migration. It is a manual DBA step for an environment whose PRODUCT database was
-- populated with billing data BEFORE the commercial database was split out (Phase 1). Fresh environments do
-- not need it — the commercial scripts create these tables empty in the commercial database already.
--
-- Prerequisites:
--   * Both databases live on the same SQL Server instance (cross-database three-part names are used below).
--   * The commercial database already has the tables (startup migrations created them).
--   * Take a backup first. Run inside a transaction. Verify counts before dropping the product copies.
--
-- Usage: replace the two placeholders, then run.
--   :ProductDb    = the existing product/compliance catalogue (e.g. Tedwren)
--   :CommercialDb = the new commercial catalogue           (e.g. TedwrenCommercial)
--
-- Idempotent: each row is copied only when a row with the same primary key is not already present, so the
-- script can be re-run safely.
-- =====================================================================================================

-- Mandates
INSERT INTO [:CommercialDb].dbo.Mandates
SELECT src.* FROM [:ProductDb].dbo.Mandates AS src
WHERE NOT EXISTS (SELECT 1 FROM [:CommercialDb].dbo.Mandates AS dst WHERE dst.Id = src.Id);

-- Payments
INSERT INTO [:CommercialDb].dbo.Payments
SELECT src.* FROM [:ProductDb].dbo.Payments AS src
WHERE NOT EXISTS (SELECT 1 FROM [:CommercialDb].dbo.Payments AS dst WHERE dst.Id = src.Id);

-- Subscriptions
INSERT INTO [:CommercialDb].dbo.BillingSubscriptions
SELECT src.* FROM [:ProductDb].dbo.BillingSubscriptions AS src
WHERE NOT EXISTS (SELECT 1 FROM [:CommercialDb].dbo.BillingSubscriptions AS dst WHERE dst.Id = src.Id);

-- Webhook events
INSERT INTO [:CommercialDb].dbo.WebhookEvents
SELECT src.* FROM [:ProductDb].dbo.WebhookEvents AS src
WHERE NOT EXISTS (SELECT 1 FROM [:CommercialDb].dbo.WebhookEvents AS dst WHERE dst.Id = src.Id);

-- Payouts
INSERT INTO [:CommercialDb].dbo.Payouts
SELECT src.* FROM [:ProductDb].dbo.Payouts AS src
WHERE NOT EXISTS (SELECT 1 FROM [:CommercialDb].dbo.Payouts AS dst WHERE dst.Id = src.Id);

-- Verify (should match) BEFORE dropping the product-side copies:
--   SELECT (SELECT COUNT(*) FROM [:ProductDb].dbo.Payments)   AS product_payments,
--          (SELECT COUNT(*) FROM [:CommercialDb].dbo.Payments) AS commercial_payments;
--
-- Only once every table's counts reconcile, drop the orphaned product-side tables:
--   DROP TABLE [:ProductDb].dbo.Payouts, [:ProductDb].dbo.WebhookEvents,
--              [:ProductDb].dbo.Payments, [:ProductDb].dbo.BillingSubscriptions, [:ProductDb].dbo.Mandates;
