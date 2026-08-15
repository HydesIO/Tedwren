-- Affiliate-agreement signing-link expiry — SQL Server, commercial database. Idempotent ALTER.
-- A leaked link is only signable until it expires; already-signed agreements ignore expiry.

IF COL_LENGTH(N'dbo.AffiliateAgreements', N'ExpiresUtc') IS NULL
ALTER TABLE dbo.AffiliateAgreements ADD ExpiresUtc DATETIMEOFFSET NULL;
