-- Person emergency contact (MC-2 / UAT-010a) — SQL Server. Idempotent ALTER of the existing Persons table.
-- The Dapper PersonRepository reads/writes these columns, but they were only ever added by the EF migration
-- AddPersonEmergencyContact — never by the startup MigrationRunner scripts. A database provisioned by the
-- MigrationRunner alone (the API's default startup path) therefore lacked them and adding an operative failed
-- with "Invalid column name 'EmergencyContactName'". This restores raw-script ↔ EF parity. Nullable, no default.

IF COL_LENGTH(N'dbo.Persons', N'EmergencyContactName') IS NULL
ALTER TABLE dbo.Persons ADD EmergencyContactName NVARCHAR(MAX) NULL;

IF COL_LENGTH(N'dbo.Persons', N'EmergencyContactPhone') IS NULL
ALTER TABLE dbo.Persons ADD EmergencyContactPhone NVARCHAR(MAX) NULL;
