-- Person emergency contact (MC-2 / UAT-010a) — PostgreSQL. Idempotent. Lowercase identifiers so the shared
-- repository SQL resolves. Previously only added by the EF migration AddPersonEmergencyContact, so a database
-- provisioned by the startup MigrationRunner alone lacked them and adding an operative failed. Nullable, no default.

ALTER TABLE persons ADD COLUMN IF NOT EXISTS emergencycontactname text NULL;
ALTER TABLE persons ADD COLUMN IF NOT EXISTS emergencycontactphone text NULL;
